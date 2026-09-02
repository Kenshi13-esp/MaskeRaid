using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador de carga y de dashes disponibles del combo. Lee el estado publico del
/// <see cref="PlayerDashController2D"/> y sigue al jugador en el mundo.
///
/// Solo escribe en la UI cuando el valor cambia: asignar fillAmount o enabled marca el canvas
/// como sucio y fuerza a reconstruir su malla, asi que hacerlo cada frame costaba un rebuild
/// por fotograma sin que cambiara nada en pantalla.
/// </summary>
public class DashComboHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerDashController2D dash;
    [SerializeField] private Image radialFill;
    [SerializeField] private Image[] dashPips = new Image[0];

    [Header("Follow (World Space UI)")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.45f, 0f);
    [SerializeField] private bool lockRotation = true;

    [Header("Comportamiento")]
    [SerializeField] private bool hideWhenCooldown = false;
    [SerializeField] private GameObject visualRoot;

    private int lastPipsShown = -1;
    private float lastFillAmount = -1f;

    private void Awake()
    {
        if (followTarget == null && dash != null) followTarget = dash.transform;
        if (visualRoot == null) visualRoot = gameObject;
    }

    private void LateUpdate()
    {
        FollowTarget();

        if (dash == null) return;

        UpdatePips();
        UpdateRadialFill();

        if (hideWhenCooldown) SetVisible(!dash.IsInCooldown);
    }

    private void FollowTarget()
    {
        if (followTarget == null) return;

        transform.position = followTarget.position + worldOffset;
        if (lockRotation) transform.rotation = Quaternion.identity;
    }

    private void UpdatePips()
    {
        int remaining = dash.DashesRemaining;
        if (remaining == lastPipsShown) return;

        lastPipsShown = remaining;

        for (int i = 0; i < dashPips.Length; i++)
        {
            if (dashPips[i] == null) continue;
            dashPips[i].enabled = remaining > i;
        }
    }

    private void UpdateRadialFill()
    {
        if (radialFill == null) return;

        float fill = dash.IsCharging ? dash.ChargeProgress : 0f;
        if (Mathf.Approximately(fill, lastFillAmount)) return;

        lastFillAmount = fill;
        radialFill.fillAmount = fill;
    }

    private void SetVisible(bool visible)
    {
        if (visualRoot == null || visualRoot.activeSelf == visible) return;

        visualRoot.SetActive(visible);
    }
}
