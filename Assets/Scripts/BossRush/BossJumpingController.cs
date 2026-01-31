using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BossHealth))]
public class BossJumpingController : MonoBehaviour, IBossController
{
    [Header("Movement Settings")]
    [SerializeField] private float jumpDuration = 1.6f;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float idleTimeBetweenAttacks = 3.5f;
    [SerializeField] private float timeBetweenPatterns = 4.0f;

    [Header("Jump Settings")]
    [SerializeField] private int jumpsPhaseOne = 3;
    [SerializeField] private int jumpsPhaseTwo = 5;
    [SerializeField] private float offScreenHeight = 30f;
    [SerializeField] private float shadowMoveDelay = 0.15f;
    [SerializeField] private float prejumpDelay = 0.8f;

    [Header("Dash Settings")]
    [SerializeField] private int dashesPhaseOne = 1;
    [SerializeField] private int dashesPhaseTwo = 3;
    [SerializeField] private float repositionSpeed = 8f;
    [SerializeField] private float repositionThreshold = 0.5f;

    [Header("Impact & Damage")]
    [SerializeField] private int areaDamage = 2;
    [SerializeField] private float areaRadius = 3.8f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private GameObject impactVFXPrefab;

    [Header("Phase Two")]
    [SerializeField] private Color phaseTwoColor = Color.red;
    [SerializeField] private float phaseTwoSpeedMultiplier = 1.4f;
    [SerializeField] private float phaseTwoHealthPercent = 0.5f;

    [Header("Shadow")]
    [SerializeField] private GameObject shadowPrefab;
    [SerializeField] private string shadowMarkerName = "Quetza_Shadow";

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private GameObject shadowInstance;
    private SpriteRenderer shadowRenderer;
    private Transform shadowMarker;
    private Vector2 shadowMarkerLocalOffset;
    private Vector2 shadowStartPos;
    private Vector2 shadowEndPos;
    private Collider2D physicalCollider;

    private bool isPhaseTwo = false;
    private bool isInAir = false;
    private float speedMultiplier = 1f;
    private bool isActive = false;

    public bool IsInAir => isInAir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        bossHealth = GetComponent<BossHealth>();
        
        shadowMarker = transform.Find(shadowMarkerName);
        if (shadowMarker == null)
        {
            Debug.LogWarning($"No se encontró el hijo '{shadowMarkerName}' que marca la posición de la sombra.");
        }

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

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (!col.isTrigger)
            {
                physicalCollider = col;
                break;
            }
        }
    }

    private void Start()
    {
        if (physicalCollider != null && player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(physicalCollider, playerCollider, true);
                Debug.Log("[BossQetza] Ignorando colisiones físicas con el Player");
            }
        }
    }

    private void CreateShadow(Vector2 bossStartPos, Vector2 bossTargetPos)
    {
        if (shadowPrefab != null)
        {
            if (shadowMarker != null)
            {
                shadowMarkerLocalOffset = shadowMarker.localPosition;
            }
            else
            {
                shadowMarkerLocalOffset = Vector2.zero;
            }
            
            shadowStartPos = bossStartPos + shadowMarkerLocalOffset;
            shadowEndPos = bossTargetPos + shadowMarkerLocalOffset;
            
            shadowInstance = Instantiate(shadowPrefab, shadowStartPos, Quaternion.identity);
            shadowRenderer = shadowInstance.GetComponent<SpriteRenderer>();
        }
        else
        {
            Debug.LogWarning("No se ha asignado el shadowPrefab en el Inspector.");
        }
    }

    private void DestroyShadow()
    {
        if (shadowInstance != null)
        {
            Destroy(shadowInstance);
            shadowInstance = null;
            shadowRenderer = null;
        }
    }

    private void LateUpdate()
    {
    }

    private void OnDestroy()
    {
        DestroyShadow();
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

    private IEnumerator JumpRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        rb.WakeUp();
        
        if (animator != null)
        {
            animator.SetTrigger("Prejump");
        }

        yield return new WaitForSeconds(prejumpDelay / speedMultiplier);

        Vector2 currentPos = rb.position;
        Vector2 offScreenPosStart = new Vector2(currentPos.x, currentPos.y + offScreenHeight);
        Vector2 targetLandingPos = ClampToArena(player.position);
        Vector2 offScreenPosEnd = new Vector2(targetLandingPos.x, targetLandingPos.y + offScreenHeight);

        CreateShadow(currentPos, targetLandingPos);

        isInAir = true;
                
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

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
                shadowInstance.transform.position = Vector2.Lerp(shadowStartPos, shadowEndPos, t);
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

            yield return null;
        }

        rb.MovePosition(targetLandingPos);

        if (myCollider != null)
        {
            myCollider.enabled = true;
        }

        isInAir = false;
        
        DestroyShadow();

        if (animator != null)
        {
            animator.SetTrigger("Stand");
        }

        ExecuteImpact(shadowEndPos);
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

    private IEnumerator RepositionToPlayerY()
    {
        if (player == null || bossHealth.IsDead) yield break;

        Vector2 currentPos = rb.position;
        float initialTargetY = player.position.y;
        float distanceY = Mathf.Abs(initialTargetY - currentPos.y);

        if (distanceY < repositionThreshold)
        {
            yield break;
        }

        if (animator != null)
        {
            animator.SetTrigger("Reposition");
        }

        Vector2 startPos = currentPos;
        float startY = currentPos.y;
        float duration = distanceY / (repositionSpeed * speedMultiplier);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (bossHealth.IsDead) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float currentTargetY = player.position.y;
            float interpolatedY = Mathf.Lerp(startY, currentTargetY, t);
            
            Vector2 newPos = new Vector2(currentPos.x, interpolatedY);
            rb.MovePosition(newPos);
            
            yield return new WaitForFixedUpdate();
        }

        Vector2 finalPos = new Vector2(currentPos.x, player.position.y);
        rb.MovePosition(finalPos);
        
        if (animator != null)
        {
            animator.SetTrigger("Stand");
        }
    }

    private IEnumerator DashRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        yield return StartCoroutine(RepositionToPlayerY());

        if (bossHealth.IsDead) yield break;

        rb.WakeUp();

        Vector2 dashStartPos = rb.position;
        
        float horizontalDirection = Mathf.Sign(player.position.x - dashStartPos.x);
        if (Mathf.Abs(horizontalDirection) < 0.01f)
        {
            horizontalDirection = 1f;
        }
        
        Vector2 dashDirection = new Vector2(horizontalDirection, 0f).normalized;
        FaceDirection(dashDirection);

        if (animator != null)
        {
            animator.SetTrigger("Dash");
        }

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

        float maxRayDistance = 100f;
        RaycastHit2D wallHit = Physics2D.Raycast(dashStartPos, dashDirection, maxRayDistance, obstacleLayer);
        
        if (wallHit.collider == null)
        {
            if (animator != null)
            {
                animator.SetTrigger("Stand");
            }
            yield break;
        }

        float safeDistance = colliderRadius + 0.5f;
        Vector2 targetPos = wallHit.point - dashDirection * safeDistance;
        float totalDistance = Vector2.Distance(dashStartPos, targetPos);

        if (totalDistance < 0.5f)
        {
            if (animator != null)
            {
                animator.SetTrigger("Stand");
            }
            yield break;
        }

        bool hitWall = false;

        while (!hitWall)
        {
            if (bossHealth.IsDead) 
            {
                yield break;
            }
            
            Vector2 nextPos = rb.position + dashDirection * dashSpeed * speedMultiplier * Time.fixedDeltaTime;
            
            RaycastHit2D immediateWallCheck = Physics2D.Raycast(rb.position, dashDirection, colliderRadius + 0.5f, obstacleLayer);
            if (immediateWallCheck.collider != null)
            {
                hitWall = true;
                break;
            }
            
            Collider2D obstacleHit = Physics2D.OverlapCircle(nextPos, colliderRadius * 0.9f, obstacleLayer);
            if (obstacleHit != null)
            {
                hitWall = true;
                break;
            }
            
            if (Vector2.Distance(dashStartPos, nextPos) >= totalDistance)
            {
                rb.MovePosition(targetPos);
                hitWall = true;
                break;
            }
            
            rb.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();
        }

        if (animator != null)
        {
            animator.SetTrigger("Stand");
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

    private void ExecuteImpact(Vector2 targetPos)
    {
        Vector2 impactPosition = targetPos;
        
        if (impactVFXPrefab != null)
        {
            GameObject impactVFX = Instantiate(impactVFXPrefab, targetPos, Quaternion.identity);
            
            Animator impactAnimator = impactVFX.GetComponent<Animator>();
            if (impactAnimator != null)
            {
                impactAnimator.SetTrigger("End");
            }
        }
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(impactPosition, areaRadius, playerLayer);
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
                Vector2 knockbackDir = ((Vector2)h.transform.position - impactPosition).normalized;
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
        Vector3 impactPosition = transform.position;
        
        if (shadowMarker != null)
        {
            impactPosition = shadowMarker.position;
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(impactPosition, areaRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * offScreenHeight, 1f);
    }
}
