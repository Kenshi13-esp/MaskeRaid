using UnityEngine;

public class PowerTransferManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El script BossHealth del boss que otorga el poder")]
    [SerializeField] private BossHealth bossHealth;
    
    [Tooltip("El PlayerDashController2D del jugador")]
    [SerializeField] private PlayerDashController2D playerDashController;

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnBossDeath.AddListener(OnBossDefeated);
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnBossDeath.RemoveListener(OnBossDefeated);
        }
    }

    private void OnBossDefeated(DashAbility newPower)
    {
        if (playerDashController != null && newPower != null)
        {
            playerDashController.SetDashAbility(newPower);
            Debug.Log($"[PowerTransfer] ¡El jugador ha obtenido el poder: {newPower.AbilityName}!");
        }
        else
        {
            Debug.LogWarning("[PowerTransfer] No se pudo transferir el poder. Verifica las referencias.");
        }
    }
}
