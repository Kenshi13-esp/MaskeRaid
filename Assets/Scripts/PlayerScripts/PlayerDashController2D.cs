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
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("Animator que se usa durante el dash con rebote (Glorbo)")]
    [SerializeField] private RuntimeAnimatorController glorboAnimatorController;
    [Tooltip("Animator que se usa con el poder de Oniki/Hulk")]
    [SerializeField] private RuntimeAnimatorController onikiAnimatorController;

    [Header("Dash Abilities")]
    [Tooltip("Habilidad de dash inicial (default)")]
    [SerializeField] private DashAbility defaultDashAbility;
    [Tooltip("Habilidad básica que restaura el animator original")]
    [SerializeField] private DashAbility basicDashAbility;
    [Tooltip("Habilidad de Hulk que activa el animator de Oniki")]
    [SerializeField] private DashAbility hulkDashAbility;

    [Header("Dash Charge + Slowmo")]
    [SerializeField] private float slowMoScale = 0.25f;

    [Header("Walls (no atraviesa paredes)")]
    [SerializeField] private LayerMask wallsMask;
    [SerializeField] private float wallSkin = 0.02f;

    [Header("Dash Hitbox")]
    [SerializeField] private DashHitbox2D dashHitbox;

    [Header("Dash VFX")]
    [SerializeField] private Transform dashVfxSpawnPoint;
    
    [Header("Bounce Physics Material")]
    [Tooltip("Material de física para rebotes (usar JellyBounce)")]
    [SerializeField] private PhysicsMaterial2D bounceMaterial;

    private DashAbility currentDashAbility;
    
    private Rigidbody2D rb;
    private PhysicsMaterial2D originalMaterial;
    private RuntimeAnimatorController originalAnimatorController;
    private PlayerHealth playerHealth;
    private BoxCollider2D boxCollider;
    private Collider2D[] enemyColliders;
    
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int ChargeHash = Animator.StringToHash("Charge");
    private static readonly int DashHash = Animator.StringToHash("Dash");

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
    
    private int bouncesLeft;
    private bool isBouncing;
    private Vector3 originalScale;
    private AudioSource chargeAudioSource;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        boxCollider = GetComponent<BoxCollider2D>();
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (animator != null)
            originalAnimatorController = animator.runtimeAnimatorController;
            
        originalScale = transform.localScale;
        originalMaterial = rb.sharedMaterial;
        
        chargeAudioSource = gameObject.AddComponent<AudioSource>();
        chargeAudioSource.loop = true;
        chargeAudioSource.playOnAwake = false;

        if (defaultDashAbility != null)
        {
            currentDashAbility = defaultDashAbility;
        }
        else
        {
            Debug.LogWarning("[PlayerDash] No se asignó defaultDashAbility. El dash no funcionará correctamente.");
        }

        CacheEnemyColliders();
    }

    private void CacheEnemyColliders()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject[] bosses = GameObject.FindGameObjectsWithTag("Boss");
        
        int totalEnemies = enemies.Length + bosses.Length;
        enemyColliders = new Collider2D[totalEnemies];
        
        int index = 0;
        foreach (GameObject enemy in enemies)
        {
            Collider2D col = enemy.GetComponent<Collider2D>();
            if (col != null) enemyColliders[index++] = col;
        }
        foreach (GameObject boss in bosses)
        {
            Collider2D col = boss.GetComponent<Collider2D>();
            if (col != null) enemyColliders[index++] = col;
        }
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
        Debug.Log($"[Dash] OnDash llamado. Fase: {ctx.phase}, started: {ctx.started}, performed: {ctx.performed}, canceled: {ctx.canceled}");
        if (ctx.started || ctx.performed) StartCharge();
        if (ctx.canceled) ReleaseDash();
    }

    private void Update()
    {
        if (isCharging && currentDashAbility != null)
        {
            chargeTimer += Time.unscaledDeltaTime;
            
            float maxCharge = currentDashAbility.MaxChargeTime;
            if (chargeTimer >= maxCharge)
            {
                chargeTimer = maxCharge;
                ReleaseDash();
            }
        }
        
        if (animator != null)
        {
            bool isMoving = moveInput.sqrMagnitude > 0.01f && !isDashing;
            animator.SetBool(RunHash, isMoving);
        }
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsLaunched)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        if (isDashing)
        {
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
        if (playerHealth != null && playerHealth.IsLaunched)
        {
            Debug.Log("[Dash] No se puede cargar: Player está launched");
            return;
        }
        if (isDashing)
        {
            Debug.Log("[Dash] No se puede cargar: ya está dasheando");
            return;
        }
        if (isCooldown)
        {
            Debug.Log("[Dash] No se puede cargar: está en cooldown");
            return;
        }
        if (isCharging)
        {
            Debug.Log("[Dash] No se puede cargar: ya está cargando");
            return;
        }
        if (currentDashAbility == null)
        {
            Debug.Log("[Dash] No se puede cargar: currentDashAbility es null");
            return;
        }
        if (dashesUsed >= currentDashAbility.ComboDashes)
        {
            Debug.Log($"[Dash] No se puede cargar: dashesUsed ({dashesUsed}) >= ComboDashes ({currentDashAbility.ComboDashes})");
            return;
        }

        Debug.Log($"[Dash] ¡CARGANDO DASH! Poder: {currentDashAbility.AbilityName}");
        isCharging = true;
        chargeTimer = 0f;

        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        
        if (animator != null)
            animator.SetBool(ChargeHash, true);
    }

    private void ReleaseDash()
    {
        if (!isCharging) return;
        if (isDashing || isCooldown) return;
        if (currentDashAbility == null) return;

        isCharging = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        
        if (animator != null)
        {
            animator.SetBool(ChargeHash, false);
            animator.SetTrigger(DashHash);
        }

        float maxCharge = currentDashAbility.MaxChargeTime;
        float t = Mathf.Clamp01(chargeTimer / maxCharge);
        float dashDistance = Mathf.Lerp(currentDashAbility.MinDashDistance, currentDashAbility.MaxDashDistance, t);

        Vector2 dashDir = (lastMoveDir.sqrMagnitude > 0.01f) ? lastMoveDir : Vector2.right;

        if (currentDashAbility.EnableWallBounce)
        {
            Debug.Log($"[Dash] Iniciando dash CON REBOTE. Dir: {dashDir}, Dist: {dashDistance}");
            StartCoroutine(DashRoutine_WithBounce(dashDir, dashDistance));
        }
        else
        {
            Debug.Log($"[Dash] Iniciando dash NORMAL. Dir: {dashDir}, Dist: {dashDistance}");
            StartCoroutine(DashRoutine_ByDuration(dashDir, dashDistance));
        }
    }

    private IEnumerator DashRoutine_ByDuration(Vector2 dashDir, float dashDistance)
    {
        isDashing = true;
        rb.linearVelocity = Vector2.zero;

        if (playerHealth != null)
            playerHealth.SetDashInvincibility(true);

        if (boxCollider != null && enemyColliders != null)
        {
            foreach (Collider2D enemyCol in enemyColliders)
            {
                if (enemyCol != null)
                    Physics2D.IgnoreCollision(boxCollider, enemyCol, true);
            }
        }

        dashSerialCounter++;
        if (dashHitbox != null && currentDashAbility != null)
            dashHitbox.BeginDash(dashSerialCounter, currentDashAbility.DamageMultiplier);

        SpawnDashVFX(dashDir);

        Vector2 startPos = rb.position;
        Vector2 target = startPos + dashDir * dashDistance;

        RaycastHit2D hit = Physics2D.Raycast(startPos, dashDir, dashDistance, wallsMask);
        if (hit.collider != null)
            target = hit.point - dashDir * wallSkin;

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, currentDashAbility.DashDuration);

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

        if (boxCollider != null && enemyColliders != null)
        {
            foreach (Collider2D enemyCol in enemyColliders)
            {
                if (enemyCol != null)
                    Physics2D.IgnoreCollision(boxCollider, enemyCol, false);
            }
        }

        if (playerHealth != null)
            playerHealth.SetDashInvincibility(false);

        isDashing = false;
        
        dashesUsed++;

        if (playerHealth != null)
            playerHealth.GrantPostDashInvincibility();

        if (currentDashAbility != null && dashesUsed >= currentDashAbility.ComboDashes)
        {
            if (cooldownCoroutine != null)
                StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }
    }
    
    private IEnumerator DashRoutine_WithBounce(Vector2 dashDir, float dashDistance)
    {
        Debug.Log($"[Dash] DashRoutine_WithBounce iniciado. BounceSpeed: {currentDashAbility.BounceSpeed}, MaxBounces: {currentDashAbility.MaxBounces}, Duration: {currentDashAbility.DashDuration}");
        
        isDashing = true;
        isBouncing = true;
        bouncesLeft = currentDashAbility.MaxBounces;
        
        rb.linearVelocity = Vector2.zero;
        
        if (bounceMaterial != null)
        {
            rb.sharedMaterial = bounceMaterial;
            Debug.Log($"[Dash] Material de física cambiado a: {bounceMaterial.name} (bounciness: {bounceMaterial.bounciness})");
        }
        else
        {
            Debug.LogWarning("[Dash] No se asignó bounceMaterial. El rebote puede no funcionar correctamente.");
        }

        if (playerHealth != null)
            playerHealth.SetDashInvincibility(true);

        if (boxCollider != null && enemyColliders != null)
        {
            foreach (Collider2D enemyCol in enemyColliders)
            {
                if (enemyCol != null)
                    Physics2D.IgnoreCollision(boxCollider, enemyCol, true);
            }
        }

        dashSerialCounter++;
        if (dashHitbox != null && currentDashAbility != null)
            dashHitbox.BeginDash(dashSerialCounter, currentDashAbility.DamageMultiplier);

        SpawnDashVFX(dashDir);
        
        rb.linearVelocity = dashDir * currentDashAbility.BounceSpeed;
        Debug.Log($"[Dash] Velocidad aplicada: {rb.linearVelocity}, Magnitud: {rb.linearVelocity.magnitude}");

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, currentDashAbility.DashDuration);

        while (elapsed < dur && isBouncing)
        {
            elapsed += Time.unscaledDeltaTime;
            
            float spd = rb.linearVelocity.magnitude;
            if (spd > currentDashAbility.MaxSpeedClamp)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * currentDashAbility.MaxSpeedClamp;
            }
            
            yield return null;
        }
        
        Debug.Log($"[Dash] DashRoutine_WithBounce terminado. Elapsed: {elapsed}, isBouncing: {isBouncing}");
        
        rb.linearVelocity = Vector2.zero;
        isBouncing = false;
        
        rb.sharedMaterial = originalMaterial;
        Debug.Log($"[Dash] Material de física restaurado a: {(originalMaterial != null ? originalMaterial.name : "null")}");

        if (dashHitbox != null)
            dashHitbox.EndDash();

        if (boxCollider != null && enemyColliders != null)
        {
            foreach (Collider2D enemyCol in enemyColliders)
            {
                if (enemyCol != null)
                    Physics2D.IgnoreCollision(boxCollider, enemyCol, false);
            }
        }

        if (playerHealth != null)
            playerHealth.SetDashInvincibility(false);

        isDashing = false;
        
        dashesUsed++;

        if (playerHealth != null)
            playerHealth.GrantPostDashInvincibility();

        if (currentDashAbility != null && dashesUsed >= currentDashAbility.ComboDashes)
        {
            if (cooldownCoroutine != null)
                StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isBouncing || !isDashing)
        {
            return;
        }
        
        if (((1 << collision.gameObject.layer) & wallsMask) != 0)
        {
            bouncesLeft--;
            Debug.Log($"[Dash] ¡REBOTE! Rebotes restantes: {bouncesLeft}, Velocidad: {rb.linearVelocity.magnitude}");
            
            float spd = rb.linearVelocity.magnitude;
            if (spd < currentDashAbility.MinSpeedAfterBounce)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * currentDashAbility.MinSpeedAfterBounce;
                Debug.Log($"[Dash] Velocidad reforzada a: {rb.linearVelocity.magnitude}");
            }
            
            if (bouncesLeft <= 0)
            {
                isBouncing = false;
                rb.linearVelocity = Vector2.zero;
                Debug.Log($"[Dash] Sin rebotes. Deteniendo.");
            }
        }
    }

    private IEnumerator CooldownRoutine()
    {
        if (currentDashAbility == null) yield break;
        
        isCooldown = true;
        rb.linearVelocity = Vector2.zero;

        float elapsed = 0f;
        while(elapsed < currentDashAbility.DashCooldownAfterCombo)
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
        if (currentDashAbility == null || currentDashAbility.DashVfxPrefab == null || dashVfxSpawnPoint == null) return;

        Vector3 spawnPos = dashVfxSpawnPoint.position;
        GameObject vfx = Instantiate(currentDashAbility.DashVfxPrefab, spawnPos, Quaternion.identity);

        SpriteRenderer vfxSR = vfx.GetComponentInChildren<SpriteRenderer>();
        if (vfxSR != null && spriteRenderer != null)
            vfxSR.flipX = spriteRenderer.flipX;
    }
    
    public void SetDashAbility(DashAbility newAbility)
    {
        if (newAbility == null)
        {
            Debug.LogWarning("[PlayerDash] Intentando asignar DashAbility nulo. Ignorado.");
            return;
        }
        
        currentDashAbility = newAbility;
        Debug.Log($"[PlayerDash] ¡Nuevo poder obtenido: {newAbility.AbilityName}! {newAbility.Description}");
        
        dashesUsed = 0;
        isCooldown = false;
        
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }
        
        if (animator != null)
        {
            if (basicDashAbility != null && newAbility == basicDashAbility && originalAnimatorController != null)
            {
                animator.runtimeAnimatorController = originalAnimatorController;
                Debug.Log($"[PlayerDash] ¡Animator restaurado al original (Player_Controller)!");
            }
            else if (hulkDashAbility != null && newAbility == hulkDashAbility && onikiAnimatorController != null)
            {
                animator.runtimeAnimatorController = onikiAnimatorController;
                Debug.Log($"[PlayerDash] ¡Animator cambiado permanentemente a Oniki (HulkDash)!");
            }
            else if (newAbility.EnableWallBounce && glorboAnimatorController != null)
            {
                animator.runtimeAnimatorController = glorboAnimatorController;
                Debug.Log($"[PlayerDash] ¡Animator cambiado permanentemente a Glorbo!");
            }
        }
        
        StartCoroutine(PowerUpVisualFeedback());
    }
    
    private IEnumerator PowerUpVisualFeedback()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        Color powerUpColor = new Color(0.3f, 1f, 0.3f, 1f);
        
        float duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 6f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, powerUpColor, t);
            
            float scaleMultiplier = 1f + Mathf.Sin(elapsed * 10f) * 0.1f;
            transform.localScale = originalScale * scaleMultiplier;
            
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
        transform.localScale = originalScale;
        
        Debug.Log($"[PowerUp] NUEVA HABILIDAD ACTIVA:");
        Debug.Log($"  - Nombre: {currentDashAbility.AbilityName}");
        Debug.Log($"  - Dashes en combo: {currentDashAbility.ComboDashes}");
        Debug.Log($"  - Tiempo de carga: {currentDashAbility.MaxChargeTime}s");
        Debug.Log($"  - Cooldown: {currentDashAbility.DashCooldownAfterCombo}s");
        Debug.Log($"  - Rebote en paredes: {(currentDashAbility.EnableWallBounce ? "SÍ" : "NO")}");
    }
    
    public DashAbility GetCurrentDashAbility()
    {
        return currentDashAbility;
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
