using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BossHealth))]
public class BossHulkController2D : MonoBehaviour
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
    [SerializeField] private float phase1_dashDuration = 1f;
    [SerializeField] private float phase1_recoveryTime = 1f;
    [SerializeField] private float phase1_timeBetweenAttacks = 0.5f;

    [Header("Fase 1: Visual Width")]
    [SerializeField] private Vector2 phase1_chargeScale = new Vector2(2f, 1f);

    [Header("=== FASE 2 ===")]
    [Header("Fase 2: Double Dash")]
    [SerializeField] private float phase2_chargeTime = 2f;
    [SerializeField] private float phase2_dashSpeed = 18f;
    [SerializeField] private float phase2_dashDuration = 0.6f;
    [SerializeField] private float phase2_delayBetweenDashes = 0.15f;
    [SerializeField] private float phase2_recoveryTime = 1.2f;
    [SerializeField] private float phase2_timeBetweenAttacks = 0.8f;

    [Header("Fase 2: Visual Width")]
    [SerializeField] private Vector2 phase2_chargeScale = new Vector2(2.5f, 1f);

    [Header("=== GENERAL ===")]
    [Header("Walls")]
    [SerializeField] private LayerMask wallsMask;

    [Header("Damage On Contact")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color chargingColor = Color.yellow;
    [SerializeField] private Color dashingColor = Color.red;

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private BossPhase currentPhase = BossPhase.Phase1;
    private State currentState = State.Idle;

    private Vector3 originalScale;
    private float lastHitTime;
    private bool phaseTransitioned = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<BossHealth>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalScale = spriteRenderer.transform.localScale;
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("[BossHulkController2D] No se encontró el Player.");
            return;
        }

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

            yield return null;
        }
    }

    private IEnumerator Phase1Attack()
    {
        float timeBetween = phase1_timeBetweenAttacks;
        yield return new WaitForSeconds(timeBetween);

        Vector2 targetPos = player.position;
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

        yield return StartCoroutine(ChargeAttack(phase1_chargeTime, phase1_chargeScale));

        yield return StartCoroutine(ExecuteDash(direction, phase1_dashSpeed, phase1_dashDuration));

        yield return StartCoroutine(RecoveryState(phase1_recoveryTime));
    }

    private IEnumerator Phase2Attack()
    {
        float timeBetween = phase2_timeBetweenAttacks;
        yield return new WaitForSeconds(timeBetween);

        yield return StartCoroutine(ChargeAttack(phase2_chargeTime, phase2_chargeScale));

        Vector2 firstTargetPos = player.position;
        Vector2 firstDirection = (firstTargetPos - (Vector2)transform.position).normalized;
        
        yield return StartCoroutine(ExecuteDash(firstDirection, phase2_dashSpeed, phase2_dashDuration));

        yield return new WaitForSeconds(phase2_delayBetweenDashes);

        Vector2 secondTargetPos = player.position;
        Vector2 secondDirection = (secondTargetPos - (Vector2)transform.position).normalized;
        
        yield return StartCoroutine(ExecuteDash(secondDirection, phase2_dashSpeed, phase2_dashDuration));

        yield return StartCoroutine(RecoveryState(phase2_recoveryTime));
    }

    private IEnumerator ChargeAttack(float chargeTime, Vector2 scaleMultiplier)
    {
        currentState = State.Charging;
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = chargingColor;
            spriteRenderer.transform.localScale = new Vector3(
                originalScale.x * scaleMultiplier.x,
                originalScale.y * scaleMultiplier.y,
                originalScale.z
            );
        }

        yield return new WaitForSeconds(chargeTime);

        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localScale = originalScale;
        }
    }

    private IEnumerator ExecuteDash(Vector2 direction, float speed, float duration)
    {
        currentState = State.Dashing;
        rb.linearVelocity = direction * speed;

        if (spriteRenderer != null)
            spriteRenderer.color = dashingColor;

        float elapsed = 0f;
        while (elapsed < duration && currentState == State.Dashing)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }

    private IEnumerator RecoveryState(float recoveryTime)
    {
        currentState = State.Recovery;
        rb.linearVelocity = Vector2.zero;

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

        if (spriteRenderer != null)
        {
            float flashTime = 0.1f;
            for (int i = 0; i < 5; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(flashTime);
                spriteRenderer.color = normalColor;
                yield return new WaitForSeconds(flashTime);
            }
        }

        yield return new WaitForSeconds(phaseTransitionDelay);

        currentPhase = BossPhase.Phase2;
        currentState = previousState;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallsMask) != 0)
        {
            if (currentState == State.Dashing)
            {
                rb.linearVelocity = Vector2.zero;
                currentState = State.Recovery;
            }
        }

        if (collision.collider.CompareTag("Player"))
        {
            TryDealDamageToPlayer(collision.collider);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            TryDealDamageToPlayer(collision.collider);
        }
    }

    private void TryDealDamageToPlayer(Collider2D playerCol)
    {
        if (Time.time < lastHitTime + hitCooldown) return;
        lastHitTime = Time.time;

        PlayerHealth hp = playerCol.GetComponent<PlayerHealth>();
        if (hp != null)
            hp.TakeDamage(contactDamage);
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
