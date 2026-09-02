using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del jugador dentro del HUD. Es un elemento fijo del canvas (no sigue al
/// jugador) y se actualiza por evento, sin consultar la vida cada frame.
///
/// El vaciado se delega en <see cref="HealthBarFill"/>, que respeta el estilo visual del Image:
/// escribir fillAmount no servia porque la imagen es Sliced para conservar el marco del HUD.
/// </summary>
public class PlayerHealthHUD : MonoBehaviour
{
    private const float FillSnapThreshold = 0.001f;

    [Header("Referencias")]
    [Tooltip("Vacio = se busca el jugador por tag")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Imagen que representa la vida restante")]
    [SerializeField] private Image fillImage;

    [Header("Opciones")]
    [Tooltip("Oculta la barra cuando la vida esta al maximo")]
    [SerializeField] private bool hideWhenFull = false;

    [Tooltip("Objeto que se oculta. Vacio = este GameObject")]
    [SerializeField] private GameObject visualRoot;

    [Tooltip("Velocidad de interpolacion de la barra, en fracciones de barra por segundo. 0 = instantaneo")]
    [SerializeField] private float smoothSpeed = 10f;

    private readonly HealthBarFill barFill = new HealthBarFill();

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

        barFill.Initialize(fillImage);
    }

    private void OnEnable()
    {
        if (playerHealth == null) return;

        playerHealth.HealthChanged += OnHealthChanged;
        OnHealthChanged(playerHealth.CurrentHP, playerHealth.MaxHP);

        barFill.Apply(targetFill);
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= OnHealthChanged;
    }

    private void Update()
    {
        if (!barFill.IsValid) return;
        if (Mathf.Abs(barFill.Current - targetFill) <= FillSnapThreshold) return;

        // Tiempo real: la barra debe seguir moviendose durante la congelacion del impacto,
        // que es justo el instante en el que el jugador esta mirando el golpe.
        float step = smoothSpeed <= 0f ? 1f : smoothSpeed * Time.unscaledDeltaTime;

        barFill.Apply(Mathf.MoveTowards(barFill.Current, targetFill, step));
    }

    private void OnHealthChanged(int currentHP, int maxHP)
    {
        targetFill = HealthBarFill.ResolveFill(currentHP, maxHP);

        if (hideWhenFull && visualRoot != null) visualRoot.SetActive(targetFill < 1f);
    }
}
