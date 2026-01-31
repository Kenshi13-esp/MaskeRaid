using UnityEngine;

public class PowerDebugger : MonoBehaviour
{
    [SerializeField] private PlayerDashController2D playerDashController;
    
    private DashAbility lastAbility;
    private float checkInterval = 0.5f;
    private float nextCheckTime;

    private void Start()
    {
        if (playerDashController == null)
        {
            playerDashController = GetComponent<PlayerDashController2D>();
        }
        
        nextCheckTime = Time.time + checkInterval;
    }

    private void Update()
    {
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            CheckForPowerChange();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            ShowCurrentPower();
        }
    }

    private void CheckForPowerChange()
    {
        if (playerDashController == null) return;
        
        DashAbility currentAbility = playerDashController.GetCurrentDashAbility();
        
        if (currentAbility != null && currentAbility != lastAbility)
        {
            ShowPowerChange(currentAbility);
            lastAbility = currentAbility;
        }
    }

    private void ShowPowerChange(DashAbility ability)
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log($"🎉 ¡NUEVO PODER OBTENIDO! 🎉");
        Debug.Log($"📛 {ability.AbilityName}");
        Debug.Log($"📝 {ability.Description}");
        Debug.Log("───────────────────────────────────────────");
        Debug.Log($"⚡ Dashes en combo: {ability.ComboDashes}");
        Debug.Log($"⏱️ Tiempo de carga: {ability.MaxChargeTime}s");
        Debug.Log($"⏰ Cooldown: {ability.DashCooldownAfterCombo}s");
        Debug.Log($"📏 Distancia: {ability.MinDashDistance}-{ability.MaxDashDistance}");
        Debug.Log($"💥 Multiplicador daño: x{ability.DamageMultiplier}");
        Debug.Log("═══════════════════════════════════════════");
    }

    private void ShowCurrentPower()
    {
        if (playerDashController == null) return;
        
        DashAbility currentAbility = playerDashController.GetCurrentDashAbility();
        
        if (currentAbility != null)
        {
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log($"📊 PODER ACTUAL:");
            Debug.Log($"📛 {currentAbility.AbilityName}");
            Debug.Log($"⚡ Dashes: {currentAbility.ComboDashes} | Carga: {currentAbility.MaxChargeTime}s | CD: {currentAbility.DashCooldownAfterCombo}s");
            Debug.Log("═══════════════════════════════════════════");
        }
        else
        {
            Debug.LogWarning("No hay poder activo");
        }
    }
}
