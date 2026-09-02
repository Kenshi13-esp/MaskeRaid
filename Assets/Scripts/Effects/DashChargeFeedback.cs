using UnityEngine;

/// <summary>
/// Feedback local de la carga y la salida del dash: comprime al jugador mientras acumula
/// carga y lo estira en la direccion del dash mientras dura.
///
/// Sustituye a la camara lenta que se aplicaba al cargar. La potencia acumulada se comunica
/// sobre el propio personaje, sin frenar el reloj del juego ni la lectura de los ataques del
/// boss. Solo lee el estado publico del <see cref="PlayerDashController2D"/>, igual que hace
/// el HUD radial de carga, asi que no anade acoplamiento al controlador.
/// </summary>
public class DashChargeFeedback : MonoBehaviour
{
    private const float ChargeSquashWidthRatio = 0.6f;
    private const float DashStretchThinRatio = 0.5f;

    [Header("Referencias")]
    [Tooltip("Vacio = se busca en este GameObject")]
    [SerializeField] private PlayerDashController2D dash;

    [Tooltip("Transform que se deforma. Vacio = el de este GameObject")]
    [SerializeField] private Transform deformTarget;

    [Header("Deformacion")]
    [Tooltip("Achatamiento maximo con la carga completa (0 = sin deformacion)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float chargeSquash = 0.16f;

    [Tooltip("Estirado en la direccion del dash mientras esta en curso")]
    [Range(0f, 0.5f)]
    [SerializeField] private float dashStretch = 0.2f;

    [Tooltip("Rapidez con la que la deformacion alcanza su objetivo")]
    [SerializeField] private float deformSpeed = 22f;

    private PlayerMaskController maskController;
    private Vector3 baseScale = Vector3.one;
    private Vector2 currentDeform = Vector2.one;
    private bool wasDashing;

    private void Awake()
    {
        if (dash == null) dash = GetComponent<PlayerDashController2D>();
        if (deformTarget == null) deformTarget = transform;

        maskController = GetComponent<PlayerMaskController>();
        baseScale = deformTarget.localScale;

        if (dash == null) Debug.LogError("[DashChargeFeedback] Falta la referencia al PlayerDashController2D.", this);
    }

    private void OnDisable()
    {
        currentDeform = Vector2.one;

        ApplyDeform();
    }

    private void Update()
    {
        if (dash == null) return;

        bool blocked = GamePause.IsGameplayBlocked;
        bool isDashing = !blocked && dash.IsDashing;
        float charge = !blocked && dash.IsCharging ? dash.ChargeProgress : 0f;

        wasDashing = isDashing;

        UpdateDeform(charge, isDashing);
    }

    private void UpdateDeform(float charge, bool isDashing)
    {
        Vector2 target = ResolveDeformTarget(charge, isDashing);

        // Interpolacion exponencial con el tiempo del juego: durante el hit stop la
        // deformacion se queda quieta como el resto de la escena.
        float blend = 1f - Mathf.Exp(-deformSpeed * Time.deltaTime);
        currentDeform = Vector2.Lerp(currentDeform, target, blend);

        ApplyDeform();
    }

    private Vector2 ResolveDeformTarget(float charge, bool isDashing)
    {
        if (isDashing)
        {
            Vector2 direction = dash.DashDirection;
            bool isVertical = Mathf.Abs(direction.y) > Mathf.Abs(direction.x);

            return isVertical
                ? new Vector2(1f - dashStretch * DashStretchThinRatio, 1f + dashStretch)
                : new Vector2(1f + dashStretch, 1f - dashStretch * DashStretchThinRatio);
        }

        if (charge <= 0f) return Vector2.one;

        // Anticipacion: el personaje se agacha y se ensancha a medida que acumula carga.
        return new Vector2(1f + chargeSquash * ChargeSquashWidthRatio * charge, 1f - chargeSquash * charge);
    }

    private void ApplyDeform()
    {
        if (deformTarget == null) return;

        // El pulso de escala al conseguir una mascara nueva escribe en el mismo transform:
        // mientras dure, manda ese feedback y aqui no se toca la escala.
        if (maskController != null && maskController.IsPlayingPowerUpFeedback) return;

        float facingSign = deformTarget.localScale.x < 0f ? -1f : 1f;

        deformTarget.localScale = new Vector3(
            Mathf.Abs(baseScale.x) * currentDeform.x * facingSign,
            baseScale.y * currentDeform.y,
            baseScale.z);
    }
}
