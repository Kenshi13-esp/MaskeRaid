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

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private BossPhase currentPhase = BossPhase.Phase1;
    private State currentState = State.Idle;
    private float lastHitTime;
    private bool phaseTransitioned = false;
    private BoxCollider2D physicalCollider;
    private BoxCollider2D triggerCollider;
    private List<Collider2D> wallColliders = new List<Collider2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<BossHealth>();

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
        yield return new WaitForSeconds(phase2_delayBetweenDashes);
        yield return StartCoroutine(ExecuteDashTowardsPlayer(phase2_dashSpeed));
        yield return StartCoroutine(RecoveryState(phase2_recoveryTime));
    }

    private IEnumerator ChargeAttack(float chargeTime)
    {
        currentState = State.Charging;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        yield return new WaitForSeconds(chargeTime);
    }

    private IEnumerator ExecuteDashTowardsPlayer(float speed)
    {
        currentState = State.Dashing;
        
        IgnoreWallCollisions(true);
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        yield return new WaitForFixedUpdate();
        
        Vector2 startPos = transform.position;
        Vector2 targetPos = player.position;
        Vector2 direction = (targetPos - startPos).normalized;
        float initialDistance = Vector2.Distance(startPos, targetPos);
        
        rb.linearVelocity = direction * speed;

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
        
        float dashDuration = Time.time - dashStartTime;
        float finalDistance = Vector2.Distance(transform.position, player.position);
        Debug.Log($"[BossHulk] Dash finalizado - duración: {dashDuration:F2}s, distancia final al Player: {finalDistance:F2}, estado: {currentState}");

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        
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

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)direction * 5f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, 0.5f);
    }
}
