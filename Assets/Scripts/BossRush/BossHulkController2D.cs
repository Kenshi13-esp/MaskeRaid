using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BossHealth))]
public class BossHulkController2D : MonoBehaviour, IBossController
{
    public enum BossPhase { Phase1, Phase2 }
    public enum State { Idle, Charging, Dashing, Recovery }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Phase Management")]
    [SerializeField] private int phase2HPThreshold = 3;
    [SerializeField] private float phaseTransitionDelay = 1f;

    [Header("=== FASE 1 ===")]
    [Header("Fase 1: Dash Simple")]
    [SerializeField] private float phase1_chargeTime = 1.5f;
    [SerializeField] private float phase1_dashSpeed = 12f;
    [SerializeField] private float phase1_recoveryTime = 1f;
    [SerializeField] private float phase1_timeBetweenAttacks = 0.5f;

    [Header("=== FASE 2 ===")]
    [Header("Fase 2: Double Dash")]
    [SerializeField] private float phase2_chargeTime = 2f;
    [SerializeField] private float phase2_dashSpeed = 18f;
    [SerializeField] private float phase2_delayBetweenDashes = 0.15f;
    [SerializeField] private float phase2_recoveryTime = 1.2f;
    [SerializeField] private float phase2_timeBetweenAttacks = 0.8f;

    [Header("=== GENERAL ===")]
    [Header("Dash Settings")]
    [SerializeField] private float stopDistanceFromPlayer = 0.5f;
    
    [Header("Walls")]
    [SerializeField] private LayerMask wallsMask;

    [Header("Damage On Contact")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float knockbackForceMultiplier = 1f;

    [Header("Phase 2 Visual")]
    [SerializeField] private Color phase2Color = Color.red;

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private BossPhase currentPhase = BossPhase.Phase1;
    private State currentState = State.Idle;
    private float lastHitTime;
    private bool phaseTransitioned = false;
    private BoxCollider2D physicalCollider;
    private BoxCollider2D triggerCollider;
    private List<Collider2D> wallColliders = new List<Collider2D>();
    private Animator animator;
    private BossAttackSoundController attackSoundController;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<BossHealth>();
        animator = GetComponent<Animator>();
        attackSoundController = GetComponent<BossAttackSoundController>();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        BoxCollider2D[] colliders = GetComponents<BoxCollider2D>();
        
        foreach (var col in colliders)
        {
            if (col.isTrigger)
                triggerCollider = col;
            else
                physicalCollider = col;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("[BossHulkController2D] No se encontró el Player.");
            return;
        }

        if (physicalCollider != null && player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(physicalCollider, playerCollider, true);
                Debug.Log("[BossHulk] Ignorando colisiones físicas con el Player");
            }
        }
        
        CacheWallColliders();
    }
    
    private void CacheWallColliders()
    {
        wallColliders.Clear();
        
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & wallsMask) != 0)
            {
                Collider2D col = obj.GetComponent<Collider2D>();
                if (col != null && !col.isTrigger)
                    wallColliders.Add(col);
            }
        }
    }

    public void ActivateBoss()
    {
        StartCoroutine(BossLoop());
    }

    private void Update()
    {
        if (bossHealth == null || bossHealth.IsDead) return;

        if (!phaseTransitioned && bossHealth.CurrentHP <= phase2HPThreshold && currentPhase == BossPhase.Phase1)
        {
            StartCoroutine(TransitionToPhase2());
        }
    }

    private IEnumerator BossLoop()
    {
        while (!bossHealth.IsDead)
        {
            if (currentPhase == BossPhase.Phase1)
            {
                yield return StartCoroutine(Phase1Attack());
            }
            else
            {
                yield return StartCoroutine(Phase2Attack());
            }
        }
    }

    private IEnumerator Phase1Attack()
    {
        float timeBetween = phase1_timeBetweenAttacks;
        yield return new WaitForSeconds(timeBetween);

        yield return StartCoroutine(ChargeAttack(phase1_chargeTime));
        yield return StartCoroutine(ExecuteDashTowardsPlayer(phase1_dashSpeed));
        yield return StartCoroutine(RecoveryState(phase1_recoveryTime));
    }

    private IEnumerator Phase2Attack()
    {
        float timeBetween = phase2_timeBetweenAttacks;
        yield return new WaitForSeconds(timeBetween);

        yield return StartCoroutine(ChargeAttack(phase2_chargeTime));
        yield return StartCoroutine(ExecuteDashTowardsPlayer(phase2_dashSpeed));
        yield return StartCoroutine(ChargeAttack(phase2_delayBetweenDashes));
        yield return StartCoroutine(ExecuteDashTowardsPlayer(phase2_dashSpeed));
        yield return StartCoroutine(RecoveryState(phase2_recoveryTime));
    }

    private IEnumerator ChargeAttack(float chargeTime)
    {
        currentState = State.Charging;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        FaceDirection(directionToPlayer);

        if (animator != null)
        {
            animator.SetTrigger("Anticipation");
        }

        yield return new WaitForSeconds(chargeTime);
    }

    private IEnumerator ExecuteDashTowardsPlayer(float speed)
    {
        currentState = State.Dashing;
        
        PlayAttackSoundLoop();
        
        IgnoreWallCollisions(true);
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        yield return new WaitForFixedUpdate();
        
        Vector2 startPos = transform.position;
        Vector2 targetPos = player.position;
        Vector2 direction = (targetPos - startPos).normalized;
        float initialDistance = Vector2.Distance(startPos, targetPos);
        
        FaceDirection(direction);
        
        rb.linearVelocity = direction * speed;

        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }

        float dashStartTime = Time.time;
        
        while (currentState == State.Dashing)
        {
            float distanceToTarget = Vector2.Distance(transform.position, targetPos);
            
            if (distanceToTarget <= stopDistanceFromPlayer)
            {
                Debug.Log($"[BossHulk] ¡Llegó al Player! Distancia final: {distanceToTarget:F2}");
                currentState = State.Recovery;
                break;
            }
            
            rb.linearVelocity = direction * speed;
            yield return new WaitForFixedUpdate();
        }
        
        StopAttackSoundLoop();
        
        float dashDuration = Time.time - dashStartTime;
        float finalDistance = Vector2.Distance(transform.position, player.position);
        Debug.Log($"[BossHulk] Dash finalizado - duración: {dashDuration:F2}s, distancia final al Player: {finalDistance:F2}, estado: {currentState}");

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }
        
        IgnoreWallCollisions(false);
    }
    
    private void IgnoreWallCollisions(bool ignore)
    {
        if (physicalCollider == null) return;
        
        foreach (Collider2D wallCol in wallColliders)
        {
            if (wallCol != null)
                Physics2D.IgnoreCollision(physicalCollider, wallCol, ignore);
        }
    }

    private IEnumerator RecoveryState(float recoveryTime)
    {
        if (currentState != State.Recovery)
        {
            currentState = State.Recovery;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        yield return new WaitForSeconds(recoveryTime);

        currentState = State.Idle;
    }

    private IEnumerator TransitionToPhase2()
    {
        phaseTransitioned = true;
        
        State previousState = currentState;
        currentState = State.Recovery;
        rb.linearVelocity = Vector2.zero;

        Debug.Log("=== BOSS HULK: FASE 2 ACTIVADA ===");
        
        SoundManager.PlaySound(SoundType.BOSS_PHASE_CHANGE);
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = phase2Color;
            Debug.Log($"[BossHulk] Color cambiado a rojo (Fase 2): {phase2Color}");
        }

        yield return new WaitForSeconds(phaseTransitionDelay);

        currentPhase = BossPhase.Phase2;
        currentState = previousState;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Comentado: ya no detenemos el dash por colisiones con muros
        // El Boss ahora solo se detiene al llegar al Player
        /*
        if (((1 << collision.gameObject.layer) & wallsMask) != 0)
        {
            if (currentState == State.Dashing)
            {
                Debug.Log($"[BossHulk] Chocó con muro ({collision.gameObject.name}) durante el dash. Deteniendo.");
                currentState = State.Recovery;
            }
        }
        */
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[BossHulk] OnTriggerEnter2D detectado! GameObject: {other.gameObject.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("[BossHulk] ¡Trigger detectó Player! Intentando hacer daño...");
            TryDealDamageToPlayer(other);
        }
        else
        {
            Debug.Log($"[BossHulk] Trigger NO es Player. Tag detectado: {other.tag}");
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TryDealDamageToPlayer(other);
        }
    }

    private void TryDealDamageToPlayer(Collider2D playerCol)
    {
        Debug.Log($"[BossHulk] TryDealDamageToPlayer - lastHit: {lastHitTime}, current: {Time.time}, cooldown: {hitCooldown}");
        
        if (Time.time < lastHitTime + hitCooldown)
        {
            Debug.Log("[BossHulk] Aún en cooldown, no hace daño");
            return;
        }
        
        lastHitTime = Time.time;

        PlayerHealth hp = playerCol.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            Vector2 knockbackDirection = ((Vector2)playerCol.transform.position - (Vector2)transform.position).normalized;
            Debug.Log($"[BossHulk] ¡HACIENDO DAÑO! Damage: {contactDamage}, Knockback: {knockbackDirection}");
            hp.TakeDamage(contactDamage, knockbackDirection, knockbackForceMultiplier);
        }
        else
        {
            Debug.LogWarning("[BossHulk] ¡PlayerHealth no encontrado en el Player!");
        }
    }

    private void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = direction.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)direction * 5f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, 0.5f);
    }
    
    private void PlayAttackSoundLoop()
    {
        if (attackSoundController != null)
        {
            attackSoundController.StartAttackSound();
        }
    }
    
    private void StopAttackSoundLoop()
    {
        if (attackSoundController != null)
        {
            attackSoundController.StopAttackSound();
        }
    }
}
