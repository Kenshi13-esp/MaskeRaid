using UnityEngine;

/// <summary>
/// Conecta un boss concreto colocado en la escena con el jugador para entregarle su mascara
/// al morir. El boss rush hace lo mismo con los bosses que instancia; este componente sirve
/// para encuentros puestos a mano.
/// </summary>
public class PowerTransferManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("BossHealth del boss que otorga la mascara")]
    [SerializeField] private BossHealth bossHealth;

    [Tooltip("PlayerMaskController del jugador. Vacio = se busca por tag")]
    [SerializeField] private PlayerMaskController playerMaskController;

    private void Awake()
    {
        if (playerMaskController != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerMaskController = player.GetComponent<PlayerMaskController>();
    }

    private void OnEnable()
    {
        if (bossHealth != null) bossHealth.OnBossDeath.AddListener(OnBossDefeated);
    }

    private void OnDisable()
    {
        if (bossHealth != null) bossHealth.OnBossDeath.RemoveListener(OnBossDefeated);
    }

    private void OnBossDefeated(MaskDefinition mask)
    {
        if (mask == null || playerMaskController == null)
        {
            Debug.LogWarning("[PowerTransfer] No se pudo entregar la mascara. Revisa las referencias.", this);
            return;
        }

        playerMaskController.EquipMask(mask);
    }
}
