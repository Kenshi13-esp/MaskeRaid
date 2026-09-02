using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Circulo radial que muestra la carga del dash sobre el jugador. Lee el estado publico del
/// <see cref="PlayerDashController2D"/> y solo escribe en la imagen cuando el relleno cambia,
/// para no forzar una reconstruccion del canvas en cada fotograma.
/// </summary>
public class DashChargeRadialHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerDashController2D dash;
    [SerializeField] private Image radialImage;
    [SerializeField] private Transform followTarget;

    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.45f, 0f);
    [SerializeField] private bool lockRotation = true;

    [Header("Comportamiento")]
    [SerializeField] private bool hideWhenNotCharging = true;
    [SerializeField] private GameObject visualRoot;

    private float lastFillAmount = -1f;

    private void Awake()
    {
        if (followTarget == null && dash != null) followTarget = dash.transform;
        if (visualRoot == null) visualRoot = gameObject;

        if (dash == null) Debug.LogError("[DashChargeRadialHUD] Falta la referencia al PlayerDashController2D.", this);
        if (radialImage == null) Debug.LogError("[DashChargeRadialHUD] Falta la imagen radial.", this);
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
            if (lockRotation) transform.rotation = Quaternion.identity;
        }

        if (dash == null || radialImage == null) return;

        bool isCharging = dash.IsCharging;

        if (hideWhenNotCharging) SetVisible(isCharging && !dash.IsInCooldown);

        float fill = isCharging ? dash.ChargeProgress : 0f;
        if (Mathf.Approximately(fill, lastFillAmount)) return;

        lastFillAmount = fill;
        radialImage.fillAmount = fill;
    }

    private void SetVisible(bool visible)
    {
        if (visualRoot == null || visualRoot.activeSelf == visible) return;

        visualRoot.SetActive(visible);
    }
}
