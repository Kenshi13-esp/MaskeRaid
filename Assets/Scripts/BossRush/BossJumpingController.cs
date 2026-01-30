using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BossHealth))]
public class BossJumpingController : MonoBehaviour, IBossController
{
    [Header("Movement Settings")]
    [SerializeField] private float jumpDuration = 1.2f;
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float idleTimeBetweenAttacks = 1.2f;
    [SerializeField] private float timeBetweenPatterns = 1.5f;

    [Header("Jump Settings")]
    [SerializeField] private int jumpsPhaseOne = 3;
    [SerializeField] private int jumpsPhaseTwo = 5;
    [SerializeField] private float offScreenHeight = 15f;
    [SerializeField] private float shadowMoveDelay = 0.15f;

    [Header("Dash Settings")]
    [SerializeField] private int dashesPhaseOne = 1;
    [SerializeField] private int dashesPhaseTwo = 3;
    [SerializeField] private float dashExtraDistance = 5f;

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

    [Header("Shadow")]
    [SerializeField] private GameObject shadowPrefab;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private GameObject shadowInstance;
    private SpriteRenderer shadowRenderer;

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
            originalColor = spriteRenderer.color;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        CreateShadow();
    }

    private void CreateShadow()
    {
        if (shadowPrefab != null)
        {
            shadowInstance = Instantiate(shadowPrefab, transform.position, Quaternion.identity);
            shadowRenderer = shadowInstance.GetComponent<SpriteRenderer>();
        }
        else if (spriteRenderer != null)
        {
            shadowInstance = new GameObject("Shadow");
            shadowRenderer = shadowInstance.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = spriteRenderer.sprite;
            shadowRenderer.color = new Color(0, 0, 0, 0.4f);
            shadowRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
            shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        }

        if (shadowInstance != null)
        {
            shadowInstance.transform.localScale = transform.localScale * 0.8f;
            shadowInstance.transform.position = transform.position;
        }
    }

    private void LateUpdate()
    {
        if (shadowInstance != null && !isInAir)
        {
            shadowInstance.transform.position = transform.position;
        }
    }

    private void OnDestroy()
    {
        if (shadowInstance != null)
        {
            Destroy(shadowInstance);
        }
    }

    public void ActivateBoss()
    {
        if (!isActive && !bossHealth.IsDead)
        {
            isActive = true;
            StartCoroutine(BossBehaviorLoop());
        }
    }

    private IEnumerator BossBehaviorLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (isActive && !bossHealth.IsDead)
        {
            CheckPhaseTransition();

            int jumpCount = isPhaseTwo ? jumpsPhaseTwo : jumpsPhaseOne;
            for (int i = 0; i < jumpCount; i++)
            {
                if (bossHealth.IsDead) yield break;
                yield return StartCoroutine(JumpRoutine());
                yield return new WaitForSeconds(idleTimeBetweenAttacks / speedMultiplier);
            }

            yield return new WaitForSeconds(timeBetweenPatterns / speedMultiplier);

            int dashCount = isPhaseTwo ? dashesPhaseTwo : dashesPhaseOne;
            for (int i = 0; i < dashCount; i++)
            {
                if (bossHealth.IsDead) yield break;
                yield return StartCoroutine(DashRoutine());
                yield return new WaitForSeconds(idleTimeBetweenAttacks / speedMultiplier);
            }

            yield return new WaitForSeconds(timeBetweenPatterns / speedMultiplier);
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

            if (pDash != null && pDash.IsDashing)
            {
                if (isDashingBoss)
                {
                    pDash.EndDashState();
                    Vector2 dir = (collision.transform.position - transform.position).normalized;
                    ph.TakeDamage(areaDamage, dir, 1f);
                    lastContactDamageTime = Time.time;
                }
                return;
            }

            Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
            int damage = isDashingBoss ? areaDamage : contactDamage;
            float force = isDashingBoss ? 1f : contactForce;
            ph.TakeDamage(damage, knockbackDir, force);
            lastContactDamageTime = Time.time;
        }
    }

    private IEnumerator JumpRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        Vector2 currentPos = rb.position;
        Vector2 offScreenPosStart = new Vector2(currentPos.x, currentPos.y + offScreenHeight);
        Vector2 targetLandingPos = ClampToArena(player.position);
        Vector2 offScreenPosEnd = new Vector2(targetLandingPos.x, targetLandingPos.y + offScreenHeight);

        isInAir = true;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), true);

        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        float halfDuration = (jumpDuration / speedMultiplier) / 2f;
        float delayDuration = shadowMoveDelay / speedMultiplier;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            rb.MovePosition(Vector2.Lerp(currentPos, offScreenPosStart, t));
            
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        rb.MovePosition(offScreenPosStart);

        yield return new WaitForSeconds(delayDuration);

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            Vector2 currentOffScreenPos = Vector2.Lerp(offScreenPosStart, offScreenPosEnd, t);
            rb.MovePosition(currentOffScreenPos);
            
            if (shadowInstance != null)
            {
                shadowInstance.transform.position = Vector2.Lerp(currentPos, targetLandingPos, t);
            }

            yield return null;
        }

        rb.MovePosition(offScreenPosEnd);

        if (animator != null)
        {
            animator.SetTrigger("Fall");
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            rb.MovePosition(Vector2.Lerp(offScreenPosEnd, targetLandingPos, t));
            
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(0f, 1f, t);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        rb.MovePosition(targetLandingPos);

        if (spriteRenderer != null)
        {
            Color c = isPhaseTwo ? phaseTwoColor : originalColor;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), false);
        isInAir = false;

        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }

        ExecuteImpact();
    }

    private Vector2 ClampToArena(Vector2 targetPos)
    {
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider == null) return targetPos;

        float checkRadius = 1f;
        if (myCollider is CircleCollider2D circleCol)
        {
            checkRadius = circleCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
        }
        else if (myCollider is BoxCollider2D boxCol)
        {
            checkRadius = Mathf.Max(boxCol.size.x, boxCol.size.y) * 0.5f * Mathf.Max(transform.localScale.x, transform.localScale.y);
        }

        checkRadius += 0.5f;

        Collider2D hit = Physics2D.OverlapCircle(targetPos, checkRadius, obstacleLayer);
        if (hit != null)
        {
            Vector2 directionFromWall = (targetPos - (Vector2)hit.transform.position).normalized;
            float maxDistance = 20f;
            
            for (float distance = checkRadius + 1f; distance < maxDistance; distance += 0.5f)
            {
                Vector2 testPos = (Vector2)hit.transform.position + directionFromWall * distance;
                if (Physics2D.OverlapCircle(testPos, checkRadius, obstacleLayer) == null)
                {
                    return testPos;
                }
            }
            
            return rb.position;
        }

        return targetPos;
    }

    private IEnumerator DashRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        isDashingBoss = true;

        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }

        Vector2 startPos = rb.position;
        Vector2 playerPos = new Vector2(player.position.x, player.position.y);
        Vector2 directionToPlayer = (playerPos - startPos).normalized;
        
        Vector2 rawTargetPos = playerPos + directionToPlayer * dashExtraDistance;

        Collider2D myCollider = GetComponent<Collider2D>();
        float colliderRadius = 0.5f;
        if (myCollider != null)
        {
            if (myCollider is CircleCollider2D circleCol)
            {
                colliderRadius = circleCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
            }
            else if (myCollider is BoxCollider2D boxCol)
            {
                colliderRadius = Mathf.Max(boxCol.size.x, boxCol.size.y) * 0.5f * Mathf.Max(transform.localScale.x, transform.localScale.y);
            }
        }

        float safeDistance = colliderRadius + 0.5f;
        Vector2 targetPos = ClampToArena(rawTargetPos);
        Vector2 dashDirection = (targetPos - startPos).normalized;
        float maxDashDistance = Vector2.Distance(startPos, targetPos);

        RaycastHit2D[] allHits = Physics2D.RaycastAll(startPos, dashDirection, maxDashDistance, obstacleLayer);
        if (allHits.Length > 0)
        {
            float closestDistance = maxDashDistance;
            foreach (RaycastHit2D h in allHits)
            {
                float dist = Vector2.Distance(startPos, h.point);
                if (dist < closestDistance && dist > 0.1f)
                {
                    closestDistance = dist;
                    targetPos = h.point - dashDirection * safeDistance;
                }
            }
        }

        targetPos = ClampToArena(targetPos);
        float finalDistance = Vector2.Distance(startPos, targetPos);

        if (finalDistance < 0.5f)
        {
            isDashingBoss = false;
            yield break;
        }

        float duration = finalDistance / (dashSpeed * speedMultiplier);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector2 nextPos = Vector2.Lerp(startPos, targetPos, t);
            
            Collider2D obstacleHit = Physics2D.OverlapCircle(nextPos, colliderRadius, obstacleLayer);
            if (obstacleHit != null)
            {
                Vector2 directionFromObstacle = (rb.position - (Vector2)obstacleHit.transform.position).normalized;
                rb.MovePosition(rb.position + directionFromObstacle * 0.1f);
                break;
            }
            
            rb.MovePosition(nextPos);
            yield return null;
        }

        isDashingBoss = false;

        if (animator != null)
        {
            animator.SetTrigger("Idle");
        }
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
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * offScreenHeight, 1f);
    }
}
