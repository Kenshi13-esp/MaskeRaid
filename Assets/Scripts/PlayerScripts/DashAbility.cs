using UnityEngine;

[CreateAssetMenu(fileName = "New Dash Ability", menuName = "Boss Rush/Dash Ability")]
public class DashAbility : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] private string abilityName = "Basic Dash";
    [SerializeField] private string description = "Standard dash ability";
    
    [Header("Charge Settings")]
    [Tooltip("Tiempo máximo de carga del dash (segundos)")]
    [SerializeField] private float maxChargeTime = 0.8f;
    
    [Header("Dash Settings")]
    [Tooltip("Tiempo que dura el dash (segundos). Más bajo = más rápido.")]
    [SerializeField] private float dashDuration = 0.10f;
    
    [Tooltip("Distancia mínima del dash")]
    [SerializeField] private float minDashDistance = 3.5f;
    
    [Tooltip("Distancia máxima del dash")]
    [SerializeField] private float maxDashDistance = 9f;
    
    [Header("Combo Settings")]
    [Tooltip("Número de dashes seguidos permitidos")]
    [SerializeField] private int comboDashes = 2;
    
    [Tooltip("Tiempo de cooldown después de usar todos los dashes del combo")]
    [SerializeField] private float dashCooldownAfterCombo = 1f;
    
    [Header("Damage Settings")]
    [Tooltip("Multiplicador de daño del dash (1.0 = daño normal, 2.0 = daño doble)")]
    [SerializeField] private float damageMultiplier = 1f;
    
    [Header("Bounce Settings (Jelly Power)")]
    [Tooltip("¿Este dash rebota en las paredes como el BossJelly?")]
    [SerializeField] private bool enableWallBounce = false;
    
    [Tooltip("Velocidad del dash cuando rebota")]
    [SerializeField] private float bounceSpeed = 16f;
    
    [Tooltip("Máximo de rebotes antes de detenerse")]
    [SerializeField] private int maxBounces = 6;
    
    [Tooltip("Velocidad mínima después de cada rebote")]
    [SerializeField] private float minSpeedAfterBounce = 12f;
    
    [Tooltip("Velocidad máxima (clamp)")]
    [SerializeField] private float maxSpeedClamp = 18f;
    
    [Header("Visual")]
    [Tooltip("Prefab del efecto visual del dash")]
    [SerializeField] private GameObject dashVfxPrefab;
    
    public string AbilityName => abilityName;
    public string Description => description;
    public float MaxChargeTime => maxChargeTime;
    public float DashDuration => dashDuration;
    public float MinDashDistance => minDashDistance;
    public float MaxDashDistance => maxDashDistance;
    public int ComboDashes => comboDashes;
    public float DashCooldownAfterCombo => dashCooldownAfterCombo;
    public float DamageMultiplier => damageMultiplier;
    public GameObject DashVfxPrefab => dashVfxPrefab;
    
    public bool EnableWallBounce => enableWallBounce;
    public float BounceSpeed => bounceSpeed;
    public int MaxBounces => maxBounces;
    public float MinSpeedAfterBounce => minSpeedAfterBounce;
    public float MaxSpeedClamp => maxSpeedClamp;
}
