using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Entrada, movimiento y carga del dash del jugador. No implementa ningun dash: la ejecucion
/// se delega en el <see cref="DashMoveBase"/> que aporta la mascara equipada, que es el mismo
/// componente que usa el boss correspondiente.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerDashActor), typeof(PlayerMaskController))]
public class PlayerDashController2D : MonoBehaviour
{
    private const float InputDeadZoneSqr = 0.01f;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Dash Charge + Slowmo")]
    [Tooltip("Escala de tiempo mientras se mantiene pulsado el dash")]
    [SerializeField] private float slowMoScale = 0.25f;

    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int ChargeHash = Animator.StringToHash("Charge");

    private Rigidbody2D body;
    private PlayerDashActor dashActor;
    private PlayerMaskController maskController;
    private PlayerHealth playerHealth;

    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool isCharging;
    private bool isCooldown;
    private bool isRunAnimating;
    private float chargeTimer;
    private int dashesUsed;
    private float defaultFixedDeltaTime;
    private Coroutine cooldownRoutine;

    /// <summary>True mientras el dash de la mascara equipada esta en curso.</summary>
    public bool IsDashing => ActiveMove != null && ActiveMove.IsDashing;

    /// <summary>True mientras se mantiene pulsado el dash acumulando carga.</summary>
    public bool IsCharging => isCharging;

    /// <summary>True mientras el combo esta en cooldown.</summary>
    public bool IsInCooldown => isCooldown;

    /// <summary>Carga acumulada entre 0 y 1.</summary>
    public float ChargeProgress
    {
        get
        {
            DashProfile profile = ActiveProfile;
            if (profile == null || profile.MaxChargeTime <= 0f) return 0f;
            return Mathf.Clamp01(chargeTimer / profile.MaxChargeTime);
        }
    }

    /// <summary>Dashes que permite el combo de la mascara equipada.</summary>
    public int MaxComboDashes
    {
        get
        {
            DashProfile profile = ActiveProfile;
            return profile != null ? profile.ComboDashes : 1;
        }
    }

    /// <summary>Dashes que quedan antes de entrar en cooldown.</summary>
    public int DashesRemaining => Mathf.Clamp(MaxComboDashes - dashesUsed, 0, MaxComboDashes);

    /// <summary>Mascara equipada actualmente.</summary>
    public MaskDefinition CurrentMask => maskController != null ? maskController.CurrentMask : null;

    private DashMoveBase ActiveMove => maskController != null ? maskController.CurrentMove : null;
    private DashProfile ActiveProfile => ActiveMove != null ? ActiveMove.Profile : null;
    private Animator Animator => dashActor != null ? dashActor.Animator : null;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        dashActor = GetComponent<PlayerDashActor>();
        maskController = GetComponent<PlayerMaskController>();
        playerHealth = GetComponent<PlayerHealth>();

        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        GamePause.PauseChanged += OnPauseChanged;

        if (maskController != null) maskController.MaskEquipped += OnMaskEquipped;
    }

    private void OnDisable()
    {
        GamePause.PauseChanged -= OnPauseChanged;

        if (maskController != null) maskController.MaskEquipped -= OnMaskEquipped;

        CancelCharge(false);
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

    /// <summary>Callback de la accion Dash del Input System (teclado y mando).</summary>
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started) StartCharge();
        else if (context.canceled) ReleaseDash();
    }

    /// <summary>Equipa una mascara nueva y reinicia el combo.</summary>
    public void SetMask(MaskDefinition mask)
    {
        if (maskController == null) return;

        maskController.EquipMask(mask);
        ResetCombo();
    }

    /// <summary>Interrumpe la carga y el dash en curso. La usa el retroceso al recibir dano.</summary>
    public void EndDashState()
    {
        CancelCharge(true);

        if (ActiveMove != null) ActiveMove.CancelDash();

        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
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

        body.linearVelocity = moveInput * moveSpeed;
    }

    private void UpdateCharge()
    {
        if (!isCharging) return;

        DashProfile profile = ActiveProfile;
        if (profile == null) return;

        chargeTimer += Time.unscaledDeltaTime;

        if (chargeTimer < profile.MaxChargeTime) return;

        chargeTimer = profile.MaxChargeTime;
        ReleaseDash();
    }

    private void StartCharge()
    {
        if (GamePause.IsGameplayBlocked) return;
        if (isCharging || isCooldown || IsDashing) return;
        if (playerHealth != null && (playerHealth.IsLaunched || playerHealth.IsDead)) return;
        if (ActiveProfile == null) return;
        if (dashesUsed >= MaxComboDashes) return;

        isCharging = true;
        chargeTimer = 0f;

        ApplySlowMotion(true);

        Animator animator = Animator;
        if (animator != null) animator.SetBool(ChargeHash, true);
    }

    private void ReleaseDash()
    {
        if (!isCharging) return;

        float chargeRatio = ChargeProgress;

        CancelCharge(true);

        if (isCooldown || IsDashing) return;

        DashMoveBase move = ActiveMove;
        if (move == null) return;

        Vector2 direction = lastMoveDirection.sqrMagnitude > InputDeadZoneSqr ? lastMoveDirection : Vector2.right;

        dashesUsed++;
        StartCoroutine(DashSequence(move, DashRequest.InDirection(direction, chargeRatio)));
    }

    private IEnumerator DashSequence(DashMoveBase move, DashRequest request)
    {
        yield return move.Execute(request);

        if (dashesUsed >= MaxComboDashes) StartCooldown();
    }

    private void CancelCharge(bool restoreTimeScale)
    {
        if (!isCharging) return;

        isCharging = false;
        chargeTimer = 0f;

        ApplySlowMotion(false, restoreTimeScale);

        Animator animator = Animator;
        if (animator != null) animator.SetBool(ChargeHash, false);
    }

    private void ApplySlowMotion(bool enable, bool restoreTimeScale = true)
    {
        if (enable)
        {
            Time.timeScale = slowMoScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime * slowMoScale;
            return;
        }

        Time.fixedDeltaTime = defaultFixedDeltaTime;

        if (restoreTimeScale && !GamePause.IsGameplayBlocked) Time.timeScale = 1f;
    }

    private void StartCooldown()
    {
        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        DashProfile profile = ActiveProfile;
        float cooldown = profile != null ? profile.DashCooldownAfterCombo : 0f;

        isCooldown = true;

        yield return new WaitForSeconds(cooldown);

        ResetCombo();
    }

    private void ResetCombo()
    {
        if (cooldownRoutine != null)
        {
            StopCoroutine(cooldownRoutine);
            cooldownRoutine = null;
        }

        dashesUsed = 0;
        isCooldown = false;
    }

    private void OnPauseChanged(bool paused)
    {
        if (!paused) return;

        CancelCharge(false);
        moveInput = Vector2.zero;
    }

    private void OnMaskEquipped(MaskDefinition mask)
    {
        CancelCharge(true);
        ResetCombo();

        isRunAnimating = false;
    }
}
