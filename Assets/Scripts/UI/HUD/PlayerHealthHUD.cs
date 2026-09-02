using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del jugador dentro del HUD. Es un elemento fijo del canvas (no sigue al
/// jugador) y se actualiza por evento, sin consultar la vida cada frame.
/// </summary>
public class PlayerHealthHUD : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Vacio = se busca el jugador por tag")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Imagen de tipo Filled que representa la vida restante")]
    [SerializeField] private Image fillImage;

    [Header("Opciones")]
    [Tooltip("Oculta la barra cuando la vida esta al maximo")]
    [SerializeField] private bool hideWhenFull = false;

    [Tooltip("Objeto que se oculta. Vacio = este GameObject")]
    [SerializeField] private GameObject visualRoot;

    [Tooltip("Velocidad de interpolacion de la barra. 0 = instantaneo")]
    [SerializeField] private float smoothSpeed = 10f;

    private float targetFill = 1f;

    private void Awake()
    {
        if (visualRoot == null) visualRoot = gameObject;

        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null) Debug.LogError("[PlayerHealthHUD] Falta la referencia a PlayerHealth.", this);
        if (fillImage == null) Debug.LogError("[PlayerHealthHUD] Falta la imagen de relleno.", this);
    }

    private void OnEnable()
    {
        if (playerHealth == null) return;

        playerHealth.HealthChanged += OnHealthChanged;
        OnHealthChanged(playerHealth.CurrentHP, playerHealth.MaxHP);

        if (fillImage != null) fillImage.fillAmount = targetFill;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= OnHealthChanged;
    }

    private void Update()
    {
        if (fillImage == null) return;
        if (Mathf.Approximately(fillImage.fillAmount, targetFill)) return;

        if (smoothSpeed <= 0f)
        {
            fillImage.fillAmount = targetFill;
            return;
        }

        fillImage.fillAmount = Mathf.MoveTowards(fillImage.fillAmount, targetFill, smoothSpeed * Time.unscaledDeltaTime);
    }

    private void OnHealthChanged(int currentHP, int maxHP)
    {
        targetFill = maxHP <= 0 ? 0f : Mathf.Clamp01((float)currentHP / maxHP);

        if (hideWhenFull && visualRoot != null) visualRoot.SetActive(targetFill < 1f);
    }
}
