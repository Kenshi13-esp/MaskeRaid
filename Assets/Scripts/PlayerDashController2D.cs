using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDashController2D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Visual (flip)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool flipOnlyOnX = true;

    [Header("Dash Charge + Slowmo")]
    [SerializeField] private float slowMoScale = 0.25f;
    [SerializeField] private float maxChargeTime = 0.8f;

    [Header("Dash Distance")]
    [SerializeField] private float minDashDistance = 3.5f;
    [SerializeField] private float maxDashDistance = 9f;

    [Header("Dash Duration (lo importante)")]
    [Tooltip("Tiempo que dura el dash (segundos). Más bajo = más rápido.")]
    [SerializeField] private float dashDuration = 0.10f;

    [Header("Dash Combo (exactly 2)")]
    [SerializeField] private int comboDashes = 2;
    [SerializeField] private float dashCooldownAfterCombo = 1f;

    [Header("Walls (no atraviesa paredes)")]
    [SerializeField] private LayerMask wallsMask;
    [SerializeField] private float wallSkin = 0.02f;

    [Header("Dash Hitbox")]
    [SerializeField] private DashHitbox2D dashHitbox;

    [Header("Dash VFX")]
    [SerializeField] private GameObject dashVfxPrefab;
    [SerializeField] private Transform dashVfxSpawnPoint;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;

    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;

    private bool isCharging;
    private bool isDashing;
    private bool isCooldown;

    private float chargeTimer;
    private int dashesUsed;
    private int dashSerialCounter;

    private float defaultFixedDeltaTime;
    private Coroutine cooldownCoroutine;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();
        if (ctx.canceled) v = Vector2.zero;

        moveInput = v;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDir = moveInput.normalized;
            UpdateFacing(moveInput);
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.started || ctx.performed) StartCharge();
        if (ctx.canceled) ReleaseDash();
    }

    private void Update()
    {
        if (isCharging)
        {
            chargeTimer += Time.unscaledDeltaTime;
            chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);
        }
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsLaunched)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void UpdateFacing(Vector2 input)
    {
        if (spriteRenderer == null) return;

        if (flipOnlyOnX)
        {
            if (Mathf.Abs(input.x) > 0.01f)
                spriteRenderer.flipX = input.x < 0f;
        }
        else
        {
            if (input.sqrMagnitude > 0.01f)
                spriteRenderer.flipX = input.x < 0f;
        }
    }

    private void StartCharge()
    {
        if (playerHealth != null && playerHealth.IsLaunched) return;
        if (isDashing || isCooldown) return;
        if (isCharging) return;
        if (dashesUsed >= comboDashes) return;

        isCharging = true;
        chargeTimer = 0f;

        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    private void ReleaseDash()
    {
        if (!isCharging) return;
        if (isDashing || isCooldown) return;

        isCharging = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;

        float t = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float dashDistance = Mathf.Lerp(minDashDistance, maxDashDistance, t);

        Vector2 dashDir = (lastMoveDir.sqrMagnitude > 0.01f) ? lastMoveDir : Vector2.right;

        StartCoroutine(DashRoutine_ByDuration(dashDir, dashDistance));
    }

    private IEnumerator DashRoutine_ByDuration(Vector2 dashDir, float dashDistance)
    {
        isDashing = true;
        rb.linearVelocity = Vector2.zero;

        dashSerialCounter++;
        if (dashHitbox != null)
            dashHitbox.BeginDash(dashSerialCounter);

        SpawnDashVFX(dashDir);

        Vector2 startPos = rb.position;
        Vector2 target = startPos + dashDir * dashDistance;

        RaycastHit2D hit = Physics2D.Raycast(startPos, dashDir, dashDistance, wallsMask);
        if (hit.collider != null)
            target = hit.point - dashDir * wallSkin;

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, dashDuration);

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / dur);

            Vector2 next = Vector2.Lerp(startPos, target, alpha);
            rb.MovePosition(next);

            yield return null;
        }

        rb.MovePosition(target);

        if (dashHitbox != null)
            dashHitbox.EndDash();

        isDashing = false;
        dashesUsed++;

        if (dashesUsed >= comboDashes)
        {
            if (cooldownCoroutine != null)
                StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }
    }

    private IEnumerator CooldownRoutine()
    {
        isCooldown = true;
        rb.linearVelocity = Vector2.zero;

        float elapsed = 0f;
        while(elapsed < dashCooldownAfterCombo)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        dashesUsed = 0;
        isCooldown = false;
        cooldownCoroutine = null;
    }

    private void SpawnDashVFX(Vector2 dashDir)
    {
        if (dashVfxPrefab == null || dashVfxSpawnPoint == null) return;

        Vector3 spawnPos = dashVfxSpawnPoint.position;
        GameObject vfx = Instantiate(dashVfxPrefab, spawnPos, Quaternion.identity);

        SpriteRenderer vfxSR = vfx.GetComponentInChildren<SpriteRenderer>();
        if (vfxSR != null && spriteRenderer != null)
            vfxSR.flipX = spriteRenderer.flipX;
    }

    public void EndDashState()
    {
        StopAllCoroutines();

        if (dashHitbox != null)
            dashHitbox.EndDash();

        if (isCharging)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }

        isCharging = false;
        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        if (isCooldown && cooldownCoroutine != null)
        {
            cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }
    }
}
