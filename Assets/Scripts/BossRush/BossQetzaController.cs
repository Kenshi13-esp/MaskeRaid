using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IA de Qetza: alterna saltos con caida en area y cargas horizontales. La carga la ejecuta
/// <see cref="QetzaDashMove"/>, el mismo componente que hereda el jugador cuando equipa la
/// mascara de Qetza.
/// </summary>
[RequireComponent(typeof(BossHealth), typeof(BossDashActor), typeof(QetzaDashMove))]
public class BossQetzaController : MonoBehaviour, IBossController
{
    private const float MinimumSqrDirection = 0.01f;
    private const float DashKnockbackMultiplier = 1.2f;
    private const float AreaKnockbackMultiplier = 1f;
    private const float ArenaScanRadius = 100f;
    private const string ImpactEndTrigger = "End";

    private static readonly int PrejumpTrigger = Animator.StringToHash("Prejump");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int FallTrigger = Animator.StringToHash("Fall");
    private static readonly int StandTrigger = Animator.StringToHash("Stand");
    private static readonly int RepositionTrigger = Animator.StringToHash("Reposition");

    [Header("Movement Settings")]
    [SerializeField] private float jumpDuration = 1.6f;
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
    [SerializeField] private int dashDamage = 1;
    [SerializeField] private float dashDamageCooldown = 0.3f;

    [Header("Impact & Damage")]
    [SerializeField] private int areaDamage = 2;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private Transform offsetAnimationTransform;
    
    [Header("Arena Bounds")]
    [SerializeField] private float arenaPaddingFromWalls = 2f;

    [Header("Phase Two")]
    [SerializeField] private Color phaseTwoColor = Color.red;
    [SerializeField] private float phaseTwoSpeedMultiplier = 1.4f;

    [Tooltip("Fraccion de vida a la que entra en fase 2 (0.5 = a la mitad justa)")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoHealthPercent = 0.5f;
    [Tooltip("Perfil de dash que se aplica al entrar en fase 2. Vacio = mantiene el de fase 1")]
    [SerializeField] private DashProfile phaseTwoDashProfile;

    [Header("Shadow")]
    [SerializeField] private GameObject shadowPrefab;
    [SerializeField] private string shadowMarkerName = "Quetza_Shadow";

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private QetzaDashMove dashMove;
    private Transform player;
    private SpriteRenderer spriteRenderer;
    private GameObject shadowInstance;
    private Transform shadowMarker;
    private Vector2 shadowMarkerLocalOffset;
    private Vector2 shadowStartPos;
    private Vector2 shadowEndPos;

    private bool isPhaseTwo = false;
    private bool isInAir = false;
    private float speedMultiplier = 1f;
    private bool isActive = false;
    private CapsuleCollider2D impactCollider;
    private Collider2D bodyCollider;
    private float lastDashDamageTime;

    private static readonly List<Collider2D> OverlapResults = new List<Collider2D>(8);
    private ContactFilter2D playerContactFilter;

    private Bounds arenaBounds;
    private bool arenaBoundsCalculated = false;

    public bool IsInAir => isInAir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        bossHealth = GetComponent<BossHealth>();
        dashMove = GetComponent<QetzaDashMove>();
        bodyCollider = GetComponent<Collider2D>();

        playerContactFilter = new ContactFilter2D { useTriggers = true };
        playerContactFilter.SetLayerMask(playerLayer);

        shadowMarker = transform.Find(shadowMarkerName);
        if (shadowMarker == null)
        {
            Debug.LogWarning($"No se encontró el hijo '{shadowMarkerName}' que marca la posición de la sombra.");
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (offsetAnimationTransform != null)
        {
            impactCollider = offsetAnimationTransform.GetComponent<CapsuleCollider2D>();
            if (impactCollider == null)
            {
                Debug.LogWarning("[BossQetza] No se encontró CapsuleCollider2D en OffsetAnimation");
            }
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Start()
    {
        CalculateArenaBounds();
    }

    private void Update()
    {
        if (dashMove != null && dashMove.IsDashing) CheckDashCollision(dashMove.CurrentDirection);

        // El cambio de fase se comprueba cada fotograma y no al empezar cada patron: asi entra
        // en fase 2 en el golpe exacto que baja la vida a la mitad.
        if (isActive && !isPhaseTwo && !bossHealth.IsDead) CheckPhaseTransition();
    }
    
    private void CalculateArenaBounds()
    {
        Collider2D[] walls = Physics2D.OverlapCircleAll(transform.position, ArenaScanRadius, obstacleLayer);
        
        if (walls.Length == 0)
        {
            Debug.LogWarning("[BossQetza] No se encontraron paredes para calcular límites de arena");
            arenaBounds = new Bounds(Vector3.zero, new Vector3(100f, 100f, 0f));
            arenaBoundsCalculated = true;
            return;
        }
        
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        
        foreach (Collider2D wall in walls)
        {
            Bounds wallBounds = wall.bounds;
            
            if (wallBounds.min.x < transform.position.x && wallBounds.max.x > wallBounds.min.x)
                minX = Mathf.Min(minX, wallBounds.max.x);
            if (wallBounds.max.x > transform.position.x && wallBounds.min.x < wallBounds.max.x)
                maxX = Mathf.Max(maxX, wallBounds.min.x);
            if (wallBounds.min.y < transform.position.y && wallBounds.max.y > wallBounds.min.y)
                minY = Mathf.Min(minY, wallBounds.max.y);
            if (wallBounds.max.y > transform.position.y && wallBounds.min.y < wallBounds.max.y)
                maxY = Mathf.Max(maxY, wallBounds.min.y);
        }
        
        minX += arenaPaddingFromWalls;
        maxX -= arenaPaddingFromWalls;
        minY += arenaPaddingFromWalls;
        maxY -= arenaPaddingFromWalls;
        
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        
        arenaBounds = new Bounds(center, size);
        arenaBoundsCalculated = true;
        
        Debug.Log($"[BossQetza] Arena bounds calculados: Centro={center}, Tamaño={size}");
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
        }
    }

    private void OnDestroy()
    {
        DestroyShadow();
    }

    /// <summary>Arranca el patron de ataque del boss.</summary>
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
        if (isPhaseTwo || !bossHealth.IsAtOrBelowRatio(phaseTwoHealthPercent)) return;

        isPhaseTwo = true;
        speedMultiplier = phaseTwoSpeedMultiplier;

        if (spriteRenderer != null)
        {
            // A traves del destello: la fase cambia en el fotograma del golpe, con el destello
            // activo, y un color escrito a pelo se perderia al restaurarse.
            DamageFlashEffect damageFlash = GetComponent<DamageFlashEffect>();

            if (damageFlash != null) damageFlash.SetBaseColor(spriteRenderer, phaseTwoColor);
            else spriteRenderer.color = phaseTwoColor;
        }

        if (phaseTwoDashProfile != null) dashMove.Profile = phaseTwoDashProfile;
    }

    private IEnumerator JumpRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        rb.WakeUp();
        
        if (animator != null)
        {
            animator.SetTrigger(PrejumpTrigger);
        }

        yield return new WaitForSeconds(prejumpDelay / speedMultiplier);

        Vector2 currentPos = rb.position;
        Vector2 offScreenPosStart = new Vector2(currentPos.x, currentPos.y + offScreenHeight);
        Vector2 targetLandingPos = ClampToArena(player.position);
        Vector2 offScreenPosEnd = new Vector2(targetLandingPos.x, targetLandingPos.y + offScreenHeight);

        CreateShadow(currentPos, targetLandingPos);

        isInAir = true;

        if (bodyCollider != null) bodyCollider.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger(JumpTrigger);
        }
        
        SoundManager.PlaySound(SoundType.QETZA_ATTACK_1);

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
        Vector2 finalClampedShadowPos = shadowEndPos;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            Vector2 desiredShadowPos = Vector2.Lerp(shadowStartPos, shadowEndPos, t);
            Vector2 clampedShadowPos = ClampShadowPosition(desiredShadowPos);
            finalClampedShadowPos = clampedShadowPos;
            
            float offsetY = offScreenHeight;
            Vector2 currentOffScreenPos = new Vector2(clampedShadowPos.x, clampedShadowPos.y + offsetY);
            rb.MovePosition(currentOffScreenPos);
            
            if (shadowInstance != null)
            {
                shadowInstance.transform.position = clampedShadowPos;
            }

            yield return null;
        }

        Vector2 finalOffScreenPos = new Vector2(finalClampedShadowPos.x, finalClampedShadowPos.y + offScreenHeight);
        rb.MovePosition(finalOffScreenPos);

        if (animator != null)
        {
            animator.SetTrigger(FallTrigger);
        }
        
        SoundManager.PlaySound(SoundType.QETZA_ATTACK_2);

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            rb.MovePosition(Vector2.Lerp(finalOffScreenPos, finalClampedShadowPos, t));

            yield return null;
        }

        rb.MovePosition(finalClampedShadowPos);

        if (bodyCollider != null) bodyCollider.enabled = true;

        isInAir = false;
        
        DestroyShadow();

        if (animator != null)
        {
            animator.SetTrigger(StandTrigger);
        }

        ExecuteImpact(finalClampedShadowPos);
    }

    private Vector2 ClampToArena(Vector2 targetPos)
    {
        if (bodyCollider == null) return targetPos;

        float checkRadius = 1f;
        if (bodyCollider is CircleCollider2D circleCol)
        {
            checkRadius = circleCol.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
        }
        else if (bodyCollider is BoxCollider2D boxCol)
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

    private Vector2 ClampShadowPosition(Vector2 shadowPos)
    {
        if (!arenaBoundsCalculated) return shadowPos;

        return new Vector2(
            Mathf.Clamp(shadowPos.x, arenaBounds.min.x, arenaBounds.max.x),
            Mathf.Clamp(shadowPos.y, arenaBounds.min.y, arenaBounds.max.y));
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
            animator.SetTrigger(RepositionTrigger);
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
            animator.SetTrigger(StandTrigger);
        }
    }

    private IEnumerator DashRoutine()
    {
        if (player == null || bossHealth.IsDead) yield break;

        yield return StartCoroutine(RepositionToPlayerY());

        if (bossHealth.IsDead) yield break;

        rb.WakeUp();

        float horizontalDirection = player.position.x >= rb.position.x ? 1f : -1f;
        Vector2 dashDirection = new Vector2(horizontalDirection, 0f);

        yield return dashMove.Execute(DashRequest.InDirection(dashDirection));
    }

    /// <summary>
    /// Busca al jugador dentro de la capsula de impacto reutilizando un buffer (sin asignar
    /// memoria por consulta) y le aplica el dano indicado. La comparten la carga horizontal
    /// y la caida en area, que antes repetian la misma consulta con arrays temporales.
    /// </summary>
    private bool TryDamagePlayerInImpactArea(int amount, Vector2 knockbackDirection, float forceMultiplier)
    {
        if (impactCollider == null) return false;

        Transform colliderTransform = impactCollider.transform;

        int hitCount = Physics2D.OverlapCapsule(
            colliderTransform.position,
            impactCollider.size,
            impactCollider.direction,
            colliderTransform.eulerAngles.z,
            playerContactFilter,
            OverlapResults);

        for (int i = 0; i < hitCount; i++)
        {
            if (!PlayerContact.TryGetDamageablePlayer(OverlapResults[i], out PlayerHealth playerHealth)) continue;

            playerHealth.TakeDamage(amount, knockbackDirection, forceMultiplier);
            return true;
        }

        return false;
    }

    private void CheckDashCollision(Vector2 dashDirection)
    {
        if (Time.time - lastDashDamageTime < dashDamageCooldown) return;

        Vector2 knockbackDirection = dashDirection.sqrMagnitude > MinimumSqrDirection
            ? dashDirection.normalized
            : Vector2.right;

        if (TryDamagePlayerInImpactArea(dashDamage, knockbackDirection, DashKnockbackMultiplier))
        {
            lastDashDamageTime = Time.time;
        }
    }

    private void ExecuteImpact(Vector2 targetPos)
    {
        SoundManager.PlaySound(SoundType.QETZA_GROUND_SLAM);

        if (impactVFXPrefab != null)
        {
            GameObject impactVFX = Instantiate(impactVFXPrefab, targetPos, Quaternion.identity);

            Animator impactAnimator = impactVFX.GetComponent<Animator>();
            if (impactAnimator != null) impactAnimator.SetTrigger(ImpactEndTrigger);
        }

        if (impactCollider == null)
        {
            Debug.LogWarning("[BossQetza] Impact collider no disponible");
            return;
        }

        PlayerHealth activePlayer = PlayerHealth.Active;
        Vector2 playerPosition = activePlayer != null ? (Vector2)activePlayer.transform.position : targetPos;
        Vector2 knockbackDirection = PlayerContact.ResolveKnockbackDirection(targetPos, playerPosition);

        TryDamagePlayerInImpactArea(areaDamage, knockbackDirection, AreaKnockbackMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        if (impactCollider != null)
        {
            Gizmos.color = Color.red;
            Vector3 colliderPosition = impactCollider.transform.position;
            Vector2 size = impactCollider.size;
            
            if (impactCollider.direction == CapsuleDirection2D.Vertical)
            {
                Gizmos.DrawWireCube(colliderPosition, new Vector3(size.x, size.y, 0.1f));
            }
            else
            {
                Gizmos.DrawWireCube(colliderPosition, new Vector3(size.y, size.x, 0.1f));
            }
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * offScreenHeight, 1f);
    }
}
