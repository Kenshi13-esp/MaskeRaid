using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BossHealth))]
public class BossJumpingController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float jumpSpeed = 15f; 
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float idleTimeBetweenAttacks = 0.8f;
    [SerializeField] private float timeBetweenPatterns = 1.5f;

    [Header("Jump Visuals")]
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float jumpScaleMultiplier = 1.4f;

    [Header("Impact & Damage")]
    [SerializeField] private int areaDamage = 2;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float areaRadius = 3.8f;
    [SerializeField] private float contactForce = 8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private GameObject impactVFXPrefab;

    [Header("Phase Two")]
    [SerializeField] private Color phaseTwoColor = Color.red;
    [SerializeField] private float phaseTwoSpeedMultiplier = 1.4f;
    [SerializeField] private float phaseTwoHealthPercent = 0.5f;

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Transform spriteTransform;
    private Vector3 originalScale;
    private Vector3 spriteOriginalLocalPos;

    private bool isPhaseTwo = false;
    private bool isInAir = false;
    private bool isDashingBoss = false;
    private float speedMultiplier = 1f;
    private bool isActive = false;
    private float lastContactDamageTime;
    private const float CONTACT_DAMAGE_COOLDOWN = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        bossHealth = GetComponent<BossHealth>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteTransform = spriteRenderer.transform;
            spriteOriginalLocalPos = spriteTransform.localPosition;
        }
        
        originalScale = transform.localScale;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public void ActivateBoss()
    {
        if (!isActive && !bossHealth.IsDead)
        {
            StartCoroutine(BossBehaviorLoop());
        }
    }

    private IEnumerator BossBehaviorLoop()
    {
        isActive = true;
        yield return new WaitForSeconds(1f);

        while (isActive && !bossHealth.IsDead)
        {
            CheckPhaseTransition();

            for (int i = 0; i < 3; i++)
            {
                if (bossHealth.IsDead) yield break;
                yield return StartCoroutine(JumpRoutine());
                yield return new WaitForSeconds(idleTimeBetweenAttacks / speedMultiplier);
            }

            yield return new WaitForSeconds(timeBetweenPatterns);

            if (bossHealth.IsDead) yield break;
            yield return StartCoroutine(DashRoutine());
            yield return new WaitForSeconds(timeBetweenPatterns);
        }
    }

    private void CheckPhaseTransition()
    {
        if (!isPhaseTwo && bossHealth.GetCurrentHealth() <= bossHealth.GetMaxHealth() * phaseTwoHealthPercent)
        {
            isPhaseTwo = true;
            speedMultiplier = phaseTwoSpeedMultiplier;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = phaseTwoColor;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (bossHealth.IsDead || isInAir) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
            PlayerDashController2D pDash = collision.gameObject.GetComponent<PlayerDashController2D>();
            if (ph == null) return;

            if (Time.time < lastContactDamageTime + CONTACT_DAMAGE_COOLDOWN) return;

            if (pDash != null)
            {
                if (isDashingBoss && pDash.IsDashing)
                {
                    pDash.EndDashState();
                    Vector2 dir = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(areaDamage, dir, 1f);
                    lastContactDamageTime = Time.time;
                }
                else if (pDash.IsDashing && !isDashingBoss)
                {
                    return;
                }
                else if (isDashingBoss && !pDash.IsDashing)
                {
                    Vector2 dir = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(areaDamage, dir, 1f);
                    lastContactDamageTime = Time.time;
                }
                else if (!isDashingBoss)
                {
                    Vector2 dir = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(contactDamage, dir, contactForce);
                    lastContactDamageTime = Time.time;
                }
            }
        }
    }

    private IEnumerator JumpRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        Vector2 startPos = rb.position;
        Vector2 targetPos = player.position; 
        float distance = Vector2.Distance(startPos, targetPos);

        Vector2 dir = (targetPos - startPos).normalized;
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance, obstacleLayer);
        if (hit.collider != null)
        {
            targetPos = hit.point - dir * 0.8f;
        }

        isInAir = true;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), true);

        float duration = distance / (jumpSpeed * speedMultiplier);
        duration = Mathf.Max(duration, 0.6f);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));
            
            if (spriteTransform != null)
            {
                float arc = 4f * t * (1f - t);
                spriteTransform.localPosition = new Vector3(
                    spriteOriginalLocalPos.x,
                    spriteOriginalLocalPos.y + (arc * jumpHeight),
                    spriteOriginalLocalPos.z
                );
                transform.localScale = originalScale * Mathf.Lerp(1f, jumpScaleMultiplier, arc);
            }
            
            yield return null;
        }

        if (spriteTransform != null)
        {
            spriteTransform.localPosition = spriteOriginalLocalPos;
        }
        transform.localScale = originalScale;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), false);
        isInAir = false;
        ExecuteImpact();
    }

    private IEnumerator DashRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        isDashingBoss = true;
        Vector2 startPos = rb.position;
        Vector2 targetPos = player.position;
        
        Vector2 dir = (targetPos - startPos).normalized;
        targetPos += dir * 3f;

        float distance = Vector2.Distance(startPos, targetPos);
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance, obstacleLayer);
        if (hit.collider != null)
        {
            targetPos = hit.point - dir * 1f;
        }

        float duration = Vector2.Distance(startPos, targetPos) / (dashSpeed * speedMultiplier);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rb.MovePosition(Vector2.Lerp(startPos, targetPos, elapsed / duration));
            yield return null;
        }

        isDashingBoss = false;
    }

    private void ExecuteImpact()
    {
        if (impactVFXPrefab != null)
        {
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
        }
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, areaRadius, playerLayer);
        foreach (Collider2D h in hits)
        {
            PlayerHealth ph = h.GetComponent<PlayerHealth>();
            PlayerDashController2D dashController = h.GetComponent<PlayerDashController2D>();

            if (dashController != null && dashController.IsDashing)
            {
                continue;
            }

            if (ph != null)
            {
                Vector2 knockbackDir = (h.transform.position - transform.position).normalized;
                if (knockbackDir.sqrMagnitude < 0.01f)
                {
                    knockbackDir = Vector2.right;
                }
                ph.TakeDamage(areaDamage, knockbackDir, 1f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, areaRadius);
    }
}
