using UnityEngine;

/// <summary>
/// Animacion de aparicion de un pop up: crece desde el centro y rebota ligeramente hasta
/// quedarse quieto. Se dispara sola cada vez que el objeto se activa, asi que basta con
/// colgarla del panel y el codigo que lo muestra no cambia.
///
/// Corre en tiempo real (<see cref="Time.unscaledDeltaTime"/>) a proposito: el menu de pausa y
/// las laminas de instrucciones aparecen con el juego congelado o en camara lenta, y con tiempo
/// escalado la animacion se quedaria parada o iria a medio gas.
///
/// Se anima la escala y no la posicion: los pop ups ya estan centrados en pantalla, asi que
/// crecer desde escala cero en su sitio ya se lee como que salen del centro. Ademas no toca el
/// RectTransform, que es lo que fija el layout.
/// </summary>
[DisallowMultipleComponent]
public class PopUpAppearAnimation : MonoBehaviour
{
    private const float MinDuration = 0.01f;
    private const float TwoPi = Mathf.PI * 2f;
    private const float QuarterTurn = Mathf.PI * 0.5f;

    [Header("Tiempo")]
    [Tooltip("Duracion total de la aparicion en segundos reales")]
    [SerializeField] private float duration = 0.45f;

    [Tooltip("Espera antes de empezar a crecer, en segundos reales")]
    [SerializeField] private float startDelay = 0f;

    [Header("Rebote")]
    [Tooltip("Numero de oscilaciones. Valores altos = mas rebotes y mas nerviosos")]
    [Range(0.5f, 3f)]
    [SerializeField] private float oscillations = 1.2f;

    [Tooltip("Rapidez con la que se apaga el rebote. Valores altos = rebote mas corto y sutil")]
    [Range(1f, 14f)]
    [SerializeField] private float bounceDamping = 5.5f;

    private Vector3 restScale = Vector3.one;
    private bool restScaleCaptured;
    private float elapsed;
    private bool isAnimating;

    private void Awake()
    {
        CaptureRestScale();
    }

    private void OnEnable()
    {
        CaptureRestScale();

        elapsed = 0f;
        isAnimating = true;

        ApplyScale(0f);
    }

    private void OnDisable()
    {
        // Se deja el objeto a su escala final: cualquier codigo que lo lea mientras esta oculto
        // debe ver el tamano real, no un fotograma congelado de la animacion.
        isAnimating = false;

        if (restScaleCaptured) transform.localScale = restScale;
    }

    private void Update()
    {
        if (!isAnimating) return;

        elapsed += Time.unscaledDeltaTime;

        float animationTime = elapsed - startDelay;

        if (animationTime < 0f)
        {
            ApplyScale(0f);
            return;
        }

        float totalDuration = Mathf.Max(MinDuration, duration);
        float progress = animationTime / totalDuration;

        if (progress >= 1f)
        {
            ApplyScale(1f);
            isAnimating = false;
            return;
        }

        ApplyScale(EvaluateBounce(progress));
    }

    /// <summary>
    /// Curva de aparicion con rebote amortiguado. Vale 0 al empezar y 1 al terminar, y en medio
    /// se pasa del destino y vuelve, con la amplitud apagandose de forma exponencial.
    /// </summary>
    private float EvaluateBounce(float progress)
    {
        float decay = Mathf.Pow(2f, -bounceDamping * progress);

        // El desfase de un cuarto de vuelta es lo que hace que la curva arranque exactamente en
        // 0 en lugar de en el valor de reposo.
        float wave = Mathf.Sin(progress * oscillations * TwoPi - QuarterTurn);

        return 1f + decay * wave;
    }

    private void ApplyScale(float factor)
    {
        transform.localScale = restScale * factor;
    }

    /// <summary>
    /// Guarda la escala de reposo la primera vez, antes de que la animacion la sobrescriba.
    /// </summary>
    private void CaptureRestScale()
    {
        if (restScaleCaptured) return;

        Vector3 currentScale = transform.localScale;

        // Una escala cero guardada dejaria el pop up invisible para siempre.
        restScale = currentScale == Vector3.zero ? Vector3.one : currentScale;
        restScaleCaptured = true;
    }
}
