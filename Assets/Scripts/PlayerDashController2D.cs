using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerDashController2D : MonoBehaviour
{
    private enum PlayerState { Normal, Charging, Dashing }

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Dash Charge + Slowmo")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slowMoScale = 0.3f;

    [Tooltip("Tiempo máximo de carga (TIEMPO REAL).")]
    [SerializeField] private float maxChargeTime = 0.8f;

    [Header("Dash Feel")]
    [SerializeField] private float minDashDistance = 3.5f;
    [SerializeField] private float maxDashDistance = 9.0f;
    [SerializeField] private float dashSpeed = 28f;

    [Header("Dash Combo (exactly 2)")]
    [SerializeField] private int comboDashes = 2;

    [Tooltip("Cooldown SOLO del dash tras el 2º dash (TIEMPO REAL).")]
    [SerializeField] private float dashCooldownAfterCombo = 1.0f;

    [Header("Walls (no atraviesa paredes)")]
    [SerializeField] private LayerMask wallsMask;
    [SerializeField] private float wallSkin = 0.02f;

    [Header("Dash Hitbox")]
    [SerializeField] private DashHitbox2D dashHitbox;

    [Header("Dash VFX")]
    [SerializeField] private GameObject dashVfxPrefab;
    [SerializeField] private Transform dashVfxSpawnPoint;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;

    private PlayerState state = PlayerState.Normal;

    private Vector2 moveInput;
    private Vector2 lastValidMoveDir = Vector2.right; // por defecto

    private float chargeTimerRealtime = 0f;

    private int dashesUsedInCombo = 0;
    private int dashSerial = 0;

    private bool dashOnCooldown = false;

    private float defaultFixedDeltaTime;

    private ContactFilter2D wallFilter;
    private readonly RaycastHit2D[] castResults = new RaycastHit2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        defaultFixedDeltaTime = Time.fixedDeltaTime;

        wallFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = wallsMask,
            useTriggers = false
        };

        if (dashHitbox != null)
            dashHitbox.SetActive(false);
    }

    private void Update()
    {
        if (state == PlayerState.Charging)
        {
            // Carga en TIEMPO REAL (no afectada por slowmo)
            chargeTimerRealtime += Time.unscaledDeltaTime;
            chargeTimerRealtime = Mathf.Min(chargeTimerRealtime, maxChargeTime);
        }
    }

    private void FixedUpdate()
    {
        // Durante el dash no aplicamos movimiento normal
        if (state == PlayerState.Dashing) return;

        // En Normal y Charging puedes moverte con WASD
        rb.linearVelocity = moveInput * moveSpeed;
    }

    // =========================================================
    // INPUT (PlayerInput = Invoke Unity Events)
    // En tu UI aparecen: Move(ctx) y Dash(ctx)
    // =========================================================

    public void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();
        moveInput = v;

        if (v.sqrMagnitude > 0.01f)
            lastValidMoveDir = v.normalized;
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        // PULSAR
        if (ctx.started)
        {
            if (state == PlayerState.Dashing) return;
            if (dashOnCooldown) return;
            if (dashesUsedInCombo >= comboDashes) return;

            state = PlayerState.Charging;
            chargeTimerRealtime = 0f;
            ApplySlowMo(true);
            return;
        }

        // SOLTAR
        if (ctx.canceled)
        {
            if (state != PlayerState.Charging) return;

            ApplySlowMo(false);

            float t = Mathf.Clamp01(chargeTimerRealtime / maxChargeTime);
            float dashDistance = Mathf.Lerp(minDashDistance, maxDashDistance, t);

            Vector2 dir = (lastValidMoveDir.sqrMagnitude > 0.01f) ? lastValidMoveDir : Vector2.right;

            StartCoroutine(DashRoutine(dir, dashDistance));
        }
    }

    // =========================================================
    // DASH
    // =========================================================

    private IEnumerator DashRoutine(Vector2 dir, float distance)
    {
        state = PlayerState.Dashing;
        rb.linearVelocity = Vector2.zero;

        dashesUsedInCombo++;
        dashSerial++;

        // VFX al empezar el dash (se destruye solo con KillSelf del Animation Event)
        SpawnDashVFX(dir);

        // Activar hitbox
        if (dashHitbox != null)
            dashHitbox.BeginDash(dashSerial);

        float remaining = distance;

        while (remaining > 0f)
        {
            float step = dashSpeed * Time.fixedDeltaTime;
            step = Mathf.Min(step, remaining);

            // Cast para NO atravesar paredes
            int hitCount = bodyCollider.Cast(dir, wallFilter, castResults, step + wallSkin);

            if (hitCount > 0)
            {
                float closest = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                    if (castResults[i].distance < closest)
                        closest = castResults[i].distance;

                float allowed = Mathf.Max(0f, closest - wallSkin);
                step = Mathf.Min(step, allowed);

                if (step <= 0f)
                    break;
            }

            rb.MovePosition(rb.position + dir * step);
            remaining -= step;

            yield return new WaitForFixedUpdate();
        }

        // Desactivar hitbox
        if (dashHitbox != null)
            dashHitbox.EndDash();

        state = PlayerState.Normal;

        // Tras el 2º dash: cooldown SOLO del dash, pero puedes moverte
        if (dashesUsedInCombo >= comboDashes)
        {
            dashesUsedInCombo = 0;
            StartCoroutine(DashCooldownRoutine());
        }
    }

    private IEnumerator DashCooldownRoutine()
    {
        dashOnCooldown = true;
        yield return new WaitForSecondsRealtime(dashCooldownAfterCombo);
        dashOnCooldown = false;
    }

    private void SpawnDashVFX(Vector2 dir)
    {
        if (dashVfxPrefab == null) return;

        Transform p = dashVfxSpawnPoint != null ? dashVfxSpawnPoint : transform;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);

        Instantiate(dashVfxPrefab, p.position, rot);
    }

    // =========================================================
    // SLOWMO + FixedDeltaTime
    // =========================================================

    private void ApplySlowMo(bool active)
    {
        if (active)
        {
            Time.timeScale = slowMoScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
    }
}


