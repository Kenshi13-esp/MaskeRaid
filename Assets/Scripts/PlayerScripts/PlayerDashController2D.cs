using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Entrada y ataques del jugador. No implementa ningun dash: la ejecucion se delega en los
/// <see cref="DashMoveBase"/>, que son los mismos componentes que usan los bosses.
///
/// Hay dos ataques con contabilidad independiente:
/// - Dash normal (O): ataque principal, sin carga y con cooldown corto. Siempre es el mismo
///   movimiento, independiente de la mascara equipada.
/// - Especial (R2): se mantiene pulsado para cargar con camara lenta y al soltar ejecuta el
///   dash de la mascara, que es el poder robado a cada boss. Sin mascara de boss el especial
///   es un dash cargado normal. Sus usos por tanda son los del perfil de la mascara mas las
///   cargas extra que ya se hayan robado, que son permanentes.
///
/// La camara lenta vive solo en la carga del especial. Al ser un recurso con cooldown, dilatar
/// el tiempo es un momento puntual y no un impuesto en cada ataque, que era el problema de
/// aplicarla al dash de siempre.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerDashActor), typeof(PlayerMaskController))]
public class PlayerDashController2D : MonoBehaviour
{
    private const float InputDeadZoneSqr = 0.01f;
    private const float MinTimeScaleForCompensation = 0.01f;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("Velocidad aparente mientras se carga el especial, respecto a la normal")]
    [Range(0f, 1f)]
    [SerializeField] private float chargeMoveSpeedMultiplier = 0.5f;

    [Header("Dash normal")]
    [Tooltip("Movimiento del ataque principal. Vacio = se busca en este GameObject")]
    [SerializeField] private NormalDashMove normalDashMove;

    [Header("Especial")]
    [Tooltip("Nombre de la accion del especial en el asset de input")]
    [SerializeField] private string specialActionName = "Special";

    [Tooltip("Escala de tiempo mientras se carga el especial (1 = sin camara lenta)")]
    [Range(0.05f, 1f)]
    [SerializeField] private float specialSlowMoScale = 0.3f;

    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int ChargeHash = Animator.StringToHash("Charge");

    private readonly DashSlot normalSlot = new DashSlot();
    private readonly DashSlot specialSlot = new DashSlot();

    private Rigidbody2D body;
    private PlayerDashActor dashActor;
    private PlayerMaskController maskController;
    private PlayerHealth playerHealth;
    private PlayerInput playerInput;
    private InputAction specialAction;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool isCharging;
    private bool isRunAnimating;
    private float chargeTimer;

    /// <summary>True mientras cualquiera de los dos ataques esta en curso.</summary>
    public bool IsDashing => IsMoveDashing(NormalMove) || IsMoveDashing(SpecialMove);

    /// <summary>True mientras se mantiene pulsado el especial acumulando carga.</summary>
    public bool IsCharging => isCharging;

    /// <summary>True mientras el especial esta en cooldown.</summary>
    public bool IsInCooldown => specialSlot.IsInCooldown;

    /// <summary>True si el ataque principal esta disponible ahora mismo.</summary>
    public bool IsNormalDashReady => normalSlot.CanUse(NormalMaxDashes);

    /// <summary>Recuperacion del ataque principal entre 0 y 1. Llega a 1 cuando vuelve a estar listo.</summary>
    public float NormalDashCooldownProgress => normalSlot.CooldownProgress;

    /// <summary>Carga acumulada del especial entre 0 y 1.</summary>
    public float ChargeProgress
    {
        get
        {
            DashProfile profile = SpecialProfile;
            if (profile == null || profile.MaxChargeTime <= 0f) return 0f;
            return Mathf.Clamp01(chargeTimer / profile.MaxChargeTime);
        }
    }

    /// <summary>
    /// Usos seguidos del especial: los que permite la mascara equipada mas las cargas extra
    /// acumuladas de forma permanente por las mascaras ya conseguidas.
    /// </summary>
    public int MaxComboDashes => ResolveMaxDashes(SpecialProfile) + ExtraSpecialCharges;

    /// <summary>Cargas extra del especial ganadas de forma permanente al robar poderes.</summary>
    public int ExtraSpecialCharges => maskController != null ? maskController.ExtraSpecialCharges : 0;

    /// <summary>Usos del especial que quedan antes de entrar en cooldown.</summary>
    public int DashesRemaining => Mathf.Clamp(MaxComboDashes - specialSlot.DashesUsed, 0, MaxComboDashes);

    /// <summary>Mascara equipada actualmente.</summary>
    public MaskDefinition CurrentMask => maskController != null ? maskController.CurrentMask : null;

    /// <summary>
    /// Direccion del ataque en curso, o la ultima direccion de movimiento si no hay ninguno.
    /// La consume el feedback visual para estirar al personaje en el eje correcto.
    /// </summary>
    public Vector2 DashDirection
    {
        get
        {
            if (IsMoveDashing(NormalMove)) return NormalMove.CurrentDirection;
            if (IsMoveDashing(SpecialMove)) return SpecialMove.CurrentDirection;

            return lastMoveDirection;
        }
    }

    /// <summary>Movimiento del ataque principal.</summary>
    private DashMoveBase NormalMove => normalDashMove;

    /// <summary>Movimiento del especial, que aporta la mascara equipada.</summary>
    private DashMoveBase SpecialMove => maskController != null ? maskController.CurrentMove : null;

    private DashProfile NormalProfile => normalDashMove != null ? normalDashMove.Profile : null;
    private DashProfile SpecialProfile => SpecialMove != null ? SpecialMove.Profile : null;
    private int NormalMaxDashes => ResolveMaxDashes(NormalProfile);
    private Animator Animator => dashActor != null ? dashActor.Animator : null;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        dashActor = GetComponent<PlayerDashActor>();
        maskController = GetComponent<PlayerMaskController>();
        playerHealth = GetComponent<PlayerHealth>();
        playerInput = GetComponent<PlayerInput>();

        if (normalDashMove == null) normalDashMove = GetComponent<NormalDashMove>();

        if (normalDashMove == null)
        {
            Debug.LogError("[PlayerDash] Falta el NormalDashMove del ataque principal.", this);
        }
    }

    private void OnEnable()
    {
        GamePause.PauseChanged += OnPauseChanged;

        if (maskController != null) maskController.MaskEquipped += OnMaskEquipped;

        SubscribeSpecialAction();
    }

    private void OnDisable()
    {
        GamePause.PauseChanged -= OnPauseChanged;

        if (maskController != null) maskController.MaskEquipped -= OnMaskEquipped;

        UnsubscribeSpecialAction();
        CancelCharge();
    }

    /// <summary>
    /// Engancha la accion del especial por codigo en lugar de por UnityEvent del PlayerInput:
    /// asi basta con que la accion exista en el asset de input, sin cablear nada en la escena.
    /// </summary>
    private void SubscribeSpecialAction()
    {
        if (playerInput == null || playerInput.actions == null) return;

        specialAction = playerInput.actions.FindAction(specialActionName);

        if (specialAction == null)
        {
            Debug.LogWarning(
                $"[PlayerDash] No existe la accion '{specialActionName}' en el asset de input: " +
                "el ataque especial no respondera hasta que se cree.", this);
            return;
        }

        specialAction.started += OnSpecial;
        specialAction.canceled += OnSpecial;
    }

    private void UnsubscribeSpecialAction()
    {
        if (specialAction == null) return;

        specialAction.started -= OnSpecial;
        specialAction.canceled -= OnSpecial;
        specialAction = null;
    }

    /// <summary>Callback de la accion Move del Input System (teclado y mando).</summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        // Con el menu abierto el stick navega la interfaz: no debe mover ni girar al jugador.
        if (GamePause.IsGameplayBlocked)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 value = context.canceled ? Vector2.zero : context.ReadValue<Vector2>();

        moveInput = value.sqrMagnitude > 1f ? value.normalized : value;

        if (moveInput.sqrMagnitude <= InputDeadZoneSqr) return;

        lastMoveDirection = moveInput.normalized;
        dashActor.FaceDirection(moveInput);
    }

    /// <summary>Callback de la accion Dash: ataque principal, sale en cuanto se pulsa.</summary>
    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        TryNormalDash();
    }

    /// <summary>Callback de la accion Special: carga mientras se mantiene y sale al soltar.</summary>
    public void OnSpecial(InputAction.CallbackContext context)
    {
        if (context.started) StartCharge();
        else if (context.canceled) ReleaseSpecial();
    }

    /// <summary>Equipa una mascara nueva y reinicia el especial.</summary>
    public void SetMask(MaskDefinition mask)
    {
        if (maskController == null) return;

        maskController.EquipMask(mask);
        specialSlot.Reset();
    }

    /// <summary>Interrumpe la carga y el ataque en curso. La usa el retroceso al recibir dano.</summary>
    public void EndDashState()
    {
        CancelCharge();

        if (NormalMove != null) NormalMove.CancelDash();
        if (SpecialMove != null) SpecialMove.CancelDash();

        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        normalSlot.Tick(deltaTime);
        specialSlot.Tick(deltaTime);

        UpdateCharge();
        UpdateRunAnimation();
    }

    private void UpdateRunAnimation()
    {
        bool isRunning = moveInput.sqrMagnitude > InputDeadZoneSqr && !IsDashing;
        if (isRunning == isRunAnimating) return;

        Animator animator = Animator;
        if (animator == null) return;

        isRunAnimating = isRunning;
        animator.SetBool(RunHash, isRunning);
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsLaunched)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (IsDashing) return;

        body.linearVelocity = moveInput * ResolveCurrentSpeed();
    }

    /// <summary>
    /// Velocidad de desplazamiento actual. Mientras se carga el especial se compensa la escala
    /// de tiempo, para que la camara lenta no convierta al jugador en un caracol: lo que se
    /// ajusta es la movilidad aparente en pantalla, no la del mundo dilatado.
    /// </summary>
    private float ResolveCurrentSpeed()
    {
        if (!isCharging) return moveSpeed;

        float timeScale = Time.timeScale;
        float scaleCompensation = timeScale > MinTimeScaleForCompensation ? 1f / timeScale : 1f;

        return moveSpeed * chargeMoveSpeedMultiplier * scaleCompensation;
    }

    private void UpdateCharge()
    {
        if (!isCharging) return;

        DashProfile profile = SpecialProfile;
        if (profile == null) return;

        // En tiempo real: la ventana para apuntar dura lo mismo en segundos de reloj
        // independientemente de cuanto dilate el tiempo la camara lenta.
        chargeTimer += Time.unscaledDeltaTime;

        if (chargeTimer < profile.MaxChargeTime) return;

        chargeTimer = profile.MaxChargeTime;
        ReleaseSpecial();
    }

    private void TryNormalDash()
    {
        // Cargando el especial no se dashea: los dos ataques comparten cuerpo, hitbox e
        // invulnerabilidad, asi que solo puede haber uno en curso.
        if (isCharging || !CanAttack()) return;

        DashMoveBase move = NormalMove;
        if (move == null || move.Profile == null) return;
        if (!normalSlot.CanUse(NormalMaxDashes)) return;

        LaunchDash(move, normalSlot, 0f, false);
    }

    private void StartCharge()
    {
        if (isCharging || !CanAttack()) return;

        DashProfile profile = SpecialProfile;
        if (profile == null) return;
        if (!specialSlot.CanUse(MaxComboDashes)) return;

        isCharging = true;
        chargeTimer = 0f;

        SlowMotion.Begin(specialSlowMoScale);

        Animator animator = Animator;
        if (animator != null) animator.SetBool(ChargeHash, true);
    }

    private void ReleaseSpecial()
    {
        if (!isCharging) return;

        float chargeRatio = ChargeProgress;

        CancelCharge();

        if (!CanAttack()) return;

        DashMoveBase move = SpecialMove;
        if (move == null) return;
        if (!specialSlot.CanUse(MaxComboDashes)) return;

        LaunchDash(move, specialSlot, chargeRatio, true);
    }

    /// <summary>Requisitos comunes a los dos ataques.</summary>
    private bool CanAttack()
    {
        if (GamePause.IsGameplayBlocked) return false;
        if (IsDashing) return false;
        if (playerHealth != null && (playerHealth.IsLaunched || playerHealth.IsDead)) return false;

        return true;
    }

    private void LaunchDash(DashMoveBase move, DashSlot slot, float chargeRatio, bool isSpecial)
    {
        Vector2 direction = lastMoveDirection.sqrMagnitude > InputDeadZoneSqr ? lastMoveDirection : Vector2.right;

        // La hitbox necesita saber de que boton viene el ataque antes de abrirse: el especial
        // aplica su dano y su golpeo fijos, no los del perfil de la mascara equipada.
        if (dashActor != null) dashActor.SetSpecialDash(isSpecial);

        slot.Consume();
        StartCoroutine(DashSequence(move, slot, DashRequest.InDirection(direction, chargeRatio), isSpecial));
    }

    /// <summary>
    /// Espera a que termine el movimiento y abre el cooldown si se ha agotado la tanda. El
    /// cooldown arranca al acabar el dash y no al lanzarlo, para que el valor del perfil sea
    /// tiempo de recuperacion real.
    ///
    /// La tanda del especial se mide con <see cref="MaxComboDashes"/> y no con el perfil a
    /// secas: si no, las cargas extra robadas nunca se llegarian a usar, porque el cooldown se
    /// abriria despues de la primera.
    /// </summary>
    private IEnumerator DashSequence(DashMoveBase move, DashSlot slot, DashRequest request, bool isSpecial)
    {
        yield return move.Execute(request);

        DashProfile profile = move.Profile;
        if (profile == null) yield break;

        int maxDashes = isSpecial ? MaxComboDashes : NormalMaxDashes;

        if (slot.DashesUsed >= maxDashes) slot.BeginCooldown(profile.DashCooldownAfterCombo);
    }

    private void CancelCharge()
    {
        if (!isCharging) return;

        isCharging = false;
        chargeTimer = 0f;

        SlowMotion.End();

        Animator animator = Animator;
        if (animator != null) animator.SetBool(ChargeHash, false);
    }

    private void OnPauseChanged(bool paused)
    {
        if (!paused) return;

        CancelCharge();
        moveInput = Vector2.zero;
    }

    private void OnMaskEquipped(MaskDefinition mask)
    {
        // La mascara cambia el especial, no el ataque principal: solo se reinicia esa cuenta.
        CancelCharge();
        specialSlot.Reset();

        isRunAnimating = false;
    }

    private static bool IsMoveDashing(DashMoveBase move)
    {
        return move != null && move.IsDashing;
    }

    private static int ResolveMaxDashes(DashProfile profile)
    {
        return profile != null ? profile.ComboDashes : 1;
    }

    /// <summary>
    /// Contabilidad de un ataque: usos gastados de la tanda y cooldown pendiente. Vive aparte
    /// porque el dash normal y el especial llevan exactamente la misma cuenta por separado.
    /// </summary>
    private class DashSlot
    {
        /// <summary>Usos gastados de la tanda actual.</summary>
        public int DashesUsed { get; private set; }

        /// <summary>True mientras el ataque se esta recuperando.</summary>
        public bool IsInCooldown => cooldownRemaining > 0f;

        /// <summary>Recuperacion entre 0 y 1. Llega a 1 cuando el ataque vuelve a estar listo.</summary>
        public float CooldownProgress
        {
            get
            {
                if (cooldownDuration <= 0f || cooldownRemaining <= 0f) return 1f;
                return 1f - Mathf.Clamp01(cooldownRemaining / cooldownDuration);
            }
        }

        private float cooldownRemaining;
        private float cooldownDuration;

        /// <summary>True si queda algun uso en la tanda y no hay cooldown pendiente.</summary>
        public bool CanUse(int maxDashes)
        {
            return !IsInCooldown && DashesUsed < Mathf.Max(1, maxDashes);
        }

        /// <summary>Gasta un uso de la tanda.</summary>
        public void Consume()
        {
            DashesUsed++;
        }

        /// <summary>Abre el cooldown. Una duracion nula deja el ataque listo al momento.</summary>
        public void BeginCooldown(float duration)
        {
            if (duration <= 0f)
            {
                Reset();
                return;
            }

            cooldownDuration = duration;
            cooldownRemaining = duration;
        }

        /// <summary>Descuenta el cooldown y devuelve el ataque al estado listo al agotarse.</summary>
        public void Tick(float deltaTime)
        {
            if (cooldownRemaining <= 0f) return;

            cooldownRemaining -= deltaTime;

            if (cooldownRemaining <= 0f) Reset();
        }

        /// <summary>Deja el ataque listo y la tanda entera disponible.</summary>
        public void Reset()
        {
            DashesUsed = 0;
            cooldownRemaining = 0f;
        }
    }
}
