using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Utilidad de depuracion: imprime la mascara equipada al pulsar una tecla y avisa cuando el
/// jugador consigue una nueva.
/// </summary>
public class PowerDebugger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerMaskController playerMaskController;

    [Header("Debug")]
    [Tooltip("Tecla que imprime la mascara equipada por consola")]
    [SerializeField] private Key inspectKey = Key.P;

    private void Awake()
    {
        if (playerMaskController == null) playerMaskController = GetComponent<PlayerMaskController>();
    }

    private void OnEnable()
    {
        if (playerMaskController != null) playerMaskController.MaskEquipped += LogMask;
    }

    private void OnDisable()
    {
        if (playerMaskController != null) playerMaskController.MaskEquipped -= LogMask;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || playerMaskController == null) return;

        if (keyboard[inspectKey].wasPressedThisFrame) LogMask(playerMaskController.CurrentMask);
    }

    private void LogMask(MaskDefinition mask)
    {
        if (mask == null)
        {
            Debug.LogWarning("[PowerDebugger] No hay mascara equipada.");
            return;
        }

        DashProfile profile = mask.DashProfile;

        if (profile == null)
        {
            Debug.LogWarning($"[PowerDebugger] La mascara '{mask.MaskName}' no tiene DashProfile.");
            return;
        }

        Debug.Log($"[PowerDebugger] {mask.MaskName} | dash: {mask.DashMoveKind} | combo: {profile.ComboDashes} | " +
                  $"carga: {profile.MaxChargeTime}s | cooldown: {profile.DashCooldownAfterCombo}s | " +
                  $"distancia: {profile.MinDashDistance}-{profile.MaxDashDistance} | dano x{profile.DamageMultiplier}");
    }
}
