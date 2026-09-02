using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del boss activo. Se engancha al evento <see cref="BossHealth.ActiveBossChanged"/>
/// en lugar de comprobar el boss activo en cada fotograma, y solo escribe en la imagen cuando
/// el relleno cambia para no forzar reconstrucciones del canvas.
///
/// El relleno se configura por codigo (Filled / Horizontal / origen izquierda) para que la barra
/// vacie siempre desde la derecha y coincida exactamente con la vida restante: dejarlo al ajuste
/// del inspector era lo que hacia que el dibujo no correspondiera con el dano aplicado.
/// </summary>
public class BossHealthHUD : MonoBehaviour
{
    private const float FillSnapThreshold = 0.001f;

    [Header("UI")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private Image fillImage;

    [Header("Opciones")]
    [SerializeField] private bool hideWhenNoBoss = false;

    [Tooltip("Recoloca la barra en la parte inferior de la pantalla al arrancar. Desactivado = mantiene la posicion de la escena")]
    [SerializeField] private bool anchorToBottom = false;

    [SerializeField] private Vector2 bottomOffset = new Vector2(0f, 20f);

    [Header("Animacion")]
    [Tooltip("Velocidad con la que la barra persigue la vida real, en fracciones de barra por segundo. 0 = salto instantaneo")]
    [SerializeField] private float fillDrainSpeed = 8f;

    private BossHealth trackedBoss;
    private float targetFill;
    private float displayedFill = -1f;

    private void Awake()
    {
        if (hudRoot == null) hudRoot = gameObject;

        ConfigureFillImage();

        if (anchorToBottom) ApplyBottomAnchor();
    }

    private void OnEnable()
    {
        BossHealth.ActiveBossChanged += TrackBoss;
        TrackBoss(BossHealth.ActiveBoss);
    }

    private void OnDisable()
    {
        BossHealth.ActiveBossChanged -= TrackBoss;
        TrackBoss(null);
    }

    private void Update()
    {
        if (fillImage == null) return;
        if (Mathf.Abs(displayedFill - targetFill) <= FillSnapThreshold) return;

        // Tiempo real: la barra debe seguir vaciandose durante la congelacion del impacto,
        // que es justo el instante en el que el jugador esta mirando el golpe.
        float step = fillDrainSpeed <= 0f ? 1f : fillDrainSpeed * Time.unscaledDeltaTime;

        ApplyFill(Mathf.MoveTowards(displayedFill, targetFill, step));
    }

    private void TrackBoss(BossHealth boss)
    {
        if (trackedBoss == boss) return;

        if (trackedBoss != null) trackedBoss.HealthChanged -= OnBossHealthChanged;

        trackedBoss = boss;

        if (trackedBoss != null)
        {
            trackedBoss.HealthChanged += OnBossHealthChanged;
            SetFillImmediate(ResolveFill(trackedBoss.CurrentHP, trackedBoss.MaxHP));
        }
        else
        {
            SetFillImmediate(0f);
        }

        if (hideWhenNoBoss && hudRoot != null && hudRoot.activeSelf != (trackedBoss != null))
        {
            hudRoot.SetActive(trackedBoss != null);
        }
    }

    private void OnBossHealthChanged(int currentHP, int maxHP)
    {
        float fill = ResolveFill(currentHP, maxHP);

        // Solo se anima el vaciado. Rellenar (boss nuevo o vida reiniciada) es instantaneo,
        // porque una barra subiendo lentamente se lee como si el boss se estuviera curando.
        if (fill > displayedFill) SetFillImmediate(fill);
        else targetFill = fill;
    }

    private void SetFillImmediate(float fill)
    {
        targetFill = fill;
        ApplyFill(fill);
    }

    private void ApplyFill(float fill)
    {
        displayedFill = fill;

        if (fillImage != null) fillImage.fillAmount = fill;
    }

    private static float ResolveFill(int currentHP, int maxHP)
    {
        return maxHP <= 0 ? 0f : Mathf.Clamp01((float)currentHP / maxHP);
    }

    /// <summary>Fuerza el modo de relleno que hace que la barra represente la vida restante.</summary>
    private void ConfigureFillImage()
    {
        if (fillImage == null) return;

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillCenter = true;
        fillImage.preserveAspect = false;
    }

    private void ApplyBottomAnchor()
    {
        RectTransform rectTransform = hudRoot != null ? hudRoot.GetComponent<RectTransform>() : null;
        if (rectTransform == null) return;

        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = bottomOffset;
    }
}
