using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la mascara que lleva puesta el jugador. Al equipar una mascara anade (o
/// reactiva) el componente de dash de ese boss, le pasa su perfil de ajustes y aplica el
/// animator, el sprite y las instrucciones asociadas.
///
/// Es el punto de union entre el sistema de bosses y el jugador: el mismo
/// <see cref="DashMoveBase"/> que usa el boss acaba ejecutandose aqui.
///
/// Hay dos tipos de poder robado y no se comportan igual. El dash es exclusivo: solo se
/// ejecuta el de la mascara puesta. Las mejoras permanentes (por ahora las cargas extra del
/// especial) se acumulan al conseguir la mascara y se conservan el resto de la partida aunque
/// luego se equipe otra, porque el jugador ya se ha quedado con ese poder.
/// </summary>
[RequireComponent(typeof(PlayerDashActor))]
public class PlayerMaskController : MonoBehaviour
{
    private const float FeedbackDuration = 1.2f;
    private const float FeedbackBlinkSpeed = 6f;
    private const float FeedbackScalePulse = 0.1f;
    private const float FeedbackScaleSpeed = 10f;

    [Header("Mascaras")]
    [Tooltip("Mascara con la que empieza el jugador")]
    [SerializeField] private MaskDefinition defaultMask;

    [Header("Visuales")]
    [Tooltip("Vacio = se busca en este GameObject y sus hijos")]
    [SerializeField] private Animator animator;

    [Tooltip("Vacio = se busca en este GameObject y sus hijos")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Renderer opcional donde mostrar el sprite de la mascara equipada")]
    [SerializeField] private SpriteRenderer maskRenderer;

    [Header("Feedback")]
    [Tooltip("Color del destello al conseguir una mascara nueva")]
    [SerializeField] private Color powerUpColor = new Color(0.3f, 1f, 0.3f, 1f);

    private readonly HashSet<MaskDefinition> collectedMasks = new HashSet<MaskDefinition>();

    private InstructionsHandler instructionsHandler;
    private RuntimeAnimatorController defaultAnimatorController;
    private Vector3 defaultScale;
    private Coroutine feedbackRoutine;

    /// <summary>Se dispara cada vez que el jugador equipa una mascara distinta.</summary>
    public event Action<MaskDefinition> MaskEquipped;

    /// <summary>
    /// Se dispara cuando cambia el total de cargas extra del especial, al conseguir una mascara
    /// que las otorga.
    /// </summary>
    public event Action<int> ExtraSpecialChargesChanged;

    /// <summary>
    /// Cargas extra del especial acumuladas por todas las mascaras conseguidas. Es permanente:
    /// no se pierde al cambiar de mascara.
    /// </summary>
    public int ExtraSpecialCharges { get; private set; }

    /// <summary>Mascara equipada actualmente.</summary>
    public MaskDefinition CurrentMask { get; private set; }

    /// <summary>Componente de dash activo, aportado por la mascara equipada.</summary>
    public DashMoveBase CurrentMove { get; private set; }

    /// <summary>Ajustes del dash activo.</summary>
    public DashProfile CurrentProfile => CurrentMove != null ? CurrentMove.Profile : null;

    /// <summary>
    /// True mientras dura el pulso visual de mascara nueva. Ese feedback escribe en la escala
    /// del jugador, asi que quien tambien la deforme debe cederle el turno.
    /// </summary>
    public bool IsPlayingPowerUpFeedback => feedbackRoutine != null;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator != null) defaultAnimatorController = animator.runtimeAnimatorController;

        defaultScale = transform.localScale;

        instructionsHandler = GetComponent<InstructionsHandler>();
        if (instructionsHandler == null) instructionsHandler = gameObject.AddComponent<InstructionsHandler>();
    }

    private void Start()
    {
        if (CurrentMask == null && defaultMask != null) EquipMask(defaultMask, false);
    }

    /// <summary>
    /// Equipa una mascara: activa su dash, cambia el animator y, si se pide, muestra sus
    /// instrucciones y el feedback visual de poder nuevo.
    /// </summary>
    public void EquipMask(MaskDefinition mask, bool announce = true)
    {
        if (mask == null)
        {
            Debug.LogWarning("[PlayerMask] Se intento equipar una mascara nula. Ignorado.");
            return;
        }

        // Las mejoras permanentes se registran aunque la mascara ya estuviera puesta: primero se
        // acumulan y solo despues se descarta el trabajo de equipar.
        CollectPermanentUpgrades(mask);

        if (CurrentMask == mask) return;

        CurrentMask = mask;

        ApplyDashMove(mask);
        ApplyAnimator(mask);
        ApplyMaskSprite(mask);

        if (announce)
        {
            ShowInstructions(mask);
            PlayPowerUpFeedback();
        }

        MaskEquipped?.Invoke(mask);

        Debug.Log($"[PlayerMask] Mascara equipada: {mask.MaskName} ({mask.DashMoveKind}).");
    }

    /// <summary>
    /// Suma las mejoras permanentes de una mascara la primera vez que se consigue. Las siguientes
    /// veces no vuelve a sumarlas, para que reequipar una mascara ya obtenida no acumule cargas.
    /// </summary>
    private void CollectPermanentUpgrades(MaskDefinition mask)
    {
        if (!collectedMasks.Add(mask)) return;
        if (mask.ExtraSpecialCharges <= 0) return;

        ExtraSpecialCharges += mask.ExtraSpecialCharges;
        ExtraSpecialChargesChanged?.Invoke(ExtraSpecialCharges);

        Debug.Log($"[PlayerMask] {mask.MaskName} otorga +{mask.ExtraSpecialCharges} carga(s) " +
                  $"de especial. Total permanente: +{ExtraSpecialCharges}.");
    }

    private void ApplyDashMove(MaskDefinition mask)
    {
        if (CurrentMove != null)
        {
            CurrentMove.CancelDash();
            CurrentMove.enabled = false;
        }

        CurrentMove = DashMoveCatalog.AttachMove(gameObject, mask.DashMoveKind);

        if (CurrentMove == null) return;

        CurrentMove.Profile = mask.DashProfile;
        CurrentMove.enabled = true;
    }

    private void ApplyAnimator(MaskDefinition mask)
    {
        if (animator == null) return;

        RuntimeAnimatorController controller = mask.AnimatorController != null
            ? mask.AnimatorController
            : defaultAnimatorController;

        if (controller != null) animator.runtimeAnimatorController = controller;
    }

    private void ApplyMaskSprite(MaskDefinition mask)
    {
        if (maskRenderer == null) return;

        maskRenderer.sprite = mask.MaskSprite;
        maskRenderer.enabled = mask.MaskSprite != null;
    }

    private void ShowInstructions(MaskDefinition mask)
    {
        if (instructionsHandler == null || mask.InstructionsSprite == null) return;

        instructionsHandler.SetInstructionsSprite(mask.InstructionsSprite);
        instructionsHandler.ShowInstructions();
    }

    private void PlayPowerUpFeedback()
    {
        if (spriteRenderer == null) return;

        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(PowerUpFeedbackRoutine());
    }

    private IEnumerator PowerUpFeedbackRoutine()
    {
        Color originalColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < FeedbackDuration)
        {
            elapsed += Time.deltaTime;

            float blend = Mathf.PingPong(elapsed * FeedbackBlinkSpeed, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, powerUpColor, blend);

            float scaleMultiplier = 1f + Mathf.Sin(elapsed * FeedbackScaleSpeed) * FeedbackScalePulse;
            transform.localScale = defaultScale * scaleMultiplier;

            yield return null;
        }

        spriteRenderer.color = originalColor;
        transform.localScale = defaultScale;
        feedbackRoutine = null;
    }
}
