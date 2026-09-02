using UnityEngine;

/// <summary>
/// Ajustes de un dash. El mismo perfil describe el ataque de un boss y el dash del jugador
/// cuando lleva la mascara de ese boss, asi que todo el balanceo se hace desde datos sin
/// tocar codigo. Los <see cref="DashMoveBase"/> leen sus valores desde aqui.
/// </summary>
[CreateAssetMenu(fileName = "New Dash Profile", menuName = "Boss Rush/Dash Profile")]
public class DashProfile : ScriptableObject
{
    [Header("Carga")]
    [Tooltip("Tiempo maximo de carga del dash (segundos)")]
    [SerializeField] private float maxChargeTime = 0.8f;

    [Header("Movimiento")]
    [Tooltip("Duracion fija del dash (segundos). Se ignora si useFixedSpeed esta activo")]
    [SerializeField] private float dashDuration = 0.10f;

    [Tooltip("Si esta activo, la duracion se calcula como distancia / dashSpeed")]
    [SerializeField] private bool useFixedSpeed = false;

    [Tooltip("Velocidad del dash en unidades/segundo (usada con useFixedSpeed y con los rebotes)")]
    [SerializeField] private float dashSpeed = 16f;

    [Tooltip("Distancia recorrida sin carga")]
    [SerializeField] private float minDashDistance = 3.5f;

    [Tooltip("Distancia recorrida con la carga completa")]
    [SerializeField] private float maxDashDistance = 9f;

    [Tooltip("Reparto de la distancia a lo largo del dash. Por defecto sale disparado y frena al final")]
    [SerializeField] private AnimationCurve dashCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.2f),
        new Keyframe(1f, 1f, 0.25f, 0f));

    [Header("Objetivo")]
    [Tooltip("Distancia a la que el dash se detiene al perseguir un objetivo")]
    [SerializeField] private float stopDistanceFromTarget = 0.5f;

    [Header("Paredes")]
    [Tooltip("Si esta activo, el dash atraviesa las paredes en lugar de detenerse")]
    [SerializeField] private bool pierceWalls = false;

    [Tooltip("Holgura que se deja al detenerse frente a una pared")]
    [SerializeField] private float wallStopSkin = 0.02f;

    [Header("Combo")]
    [Tooltip("Numero de dashes seguidos permitidos antes del cooldown")]
    [SerializeField] private int comboDashes = 2;

    [Tooltip("Cooldown tras agotar el combo (segundos)")]
    [SerializeField] private float dashCooldownAfterCombo = 1f;

    [Header("Dano")]
    [Tooltip("Multiplicador de dano del dash (1 = normal, 2 = doble)")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Rebotes (Glorbo)")]
    [Tooltip("Maximo de rebotes antes de detenerse")]
    [SerializeField] private int maxBounces = 6;

    [Tooltip("Velocidad minima garantizada tras cada rebote")]
    [SerializeField] private float minSpeedAfterBounce = 12f;

    [Tooltip("Velocidad maxima permitida durante el dash con rebotes")]
    [SerializeField] private float maxSpeedClamp = 18f;

    [Tooltip("Material de fisica aplicado mientras se rebota. Vacio = mantiene el del actor")]
    [SerializeField] private PhysicsMaterial2D bounceMaterial;

    [Header("Arranque")]
    [Tooltip("Duracion del temblor de camara al salir disparado (0 = sin temblor)")]
    [SerializeField] private float startShakeDuration = 0f;

    [Tooltip("Intensidad del temblor de camara al salir disparado")]
    [SerializeField] private float startShakeMagnitude = 0.06f;

    [Header("Impacto")]
    [Tooltip("Congelacion del juego al conectar el golpe, en segundos reales (0 = sin congelacion)")]
    [SerializeField] private float hitStopDuration = 0.06f;

    [Tooltip("Escala de tiempo durante la congelacion del impacto. 0 = el mundo se para del todo")]
    [Range(0f, 1f)]
    [SerializeField] private float hitStopTimeScale = 0f;

    [Tooltip("Duracion del temblor de camara al impactar (0 = sin temblor)")]
    [SerializeField] private float cameraShakeDuration = 0f;

    [Tooltip("Intensidad del temblor de camara al impactar")]
    [SerializeField] private float cameraShakeMagnitude = 0.15f;

    [Header("Animacion y sonido")]
    [Tooltip("Trigger del Animator al empezar el dash. Vacio = no dispara ninguno")]
    [SerializeField] private string dashAnimatorTrigger = "Dash";

    [Tooltip("Trigger del Animator al terminar el dash. Vacio = no dispara ninguno")]
    [SerializeField] private string endAnimatorTrigger = "";

    [Tooltip("Sonido puntual del dash (solo lo usa el jugador; los bosses usan su loop)")]
    [SerializeField] private SoundType dashSoundType = SoundType.PLAYER_ATTACK;

    [Header("Visual")]
    [Tooltip("Prefab del efecto visual del dash")]
    [SerializeField] private GameObject dashVfxPrefab;

    [Tooltip("Segundos hasta destruir el VFX instanciado (0 = no se destruye)")]
    [SerializeField] private float dashVfxLifetime = 1f;

    public float MaxChargeTime => maxChargeTime;
    public float DashDuration => dashDuration;
    public bool UseFixedSpeed => useFixedSpeed;
    public float DashSpeed => dashSpeed;
    public float MinDashDistance => minDashDistance;
    public float MaxDashDistance => maxDashDistance;
    public float StopDistanceFromTarget => stopDistanceFromTarget;
    public bool PierceWalls => pierceWalls;
    public float WallStopSkin => wallStopSkin;
    public int ComboDashes => Mathf.Max(1, comboDashes);
    public float DashCooldownAfterCombo => dashCooldownAfterCombo;
    public float DamageMultiplier => damageMultiplier;
    public int MaxBounces => maxBounces;
    public float MinSpeedAfterBounce => minSpeedAfterBounce;
    public float MaxSpeedClamp => maxSpeedClamp;
    public PhysicsMaterial2D BounceMaterial => bounceMaterial;
    public float CameraShakeDuration => cameraShakeDuration;
    public float CameraShakeMagnitude => cameraShakeMagnitude;
    public float StartShakeDuration => startShakeDuration;
    public float StartShakeMagnitude => startShakeMagnitude;
    public float HitStopDuration => hitStopDuration;
    public float HitStopTimeScale => hitStopTimeScale;
    public string DashAnimatorTrigger => dashAnimatorTrigger;
    public string EndAnimatorTrigger => endAnimatorTrigger;
    public SoundType DashSoundType => dashSoundType;
    public GameObject DashVfxPrefab => dashVfxPrefab;
    public float DashVfxLifetime => dashVfxLifetime;

    /// <summary>
    /// Fraccion del recorrido completada en un instante normalizado del dash. Es lo que da el
    /// punch a la salida: con la curva por defecto la mayor parte de la distancia se cubre al
    /// principio y el final decelera. Cae a un reparto lineal si la curva se queda sin claves.
    /// </summary>
    public float EvaluateDashProgress(float normalizedTime)
    {
        if (dashCurve == null || dashCurve.length < 2) return normalizedTime;

        return dashCurve.Evaluate(normalizedTime);
    }
}
