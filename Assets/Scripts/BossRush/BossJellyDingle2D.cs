using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BossHealth))]
public class BossJellyDingle2D : MonoBehaviour, IBossController
{
    public enum State { Idle, ChargeUp, Dashing, Stunned }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Phase 1 Settings")]
    [SerializeField] private float phase1_chargeUpTime = 0.6f;
    [SerializeField] private float phase1_dashSpeed = 16f;
    [SerializeField] private float phase1_dashDuration = 0.9f;
    [SerializeField] private float phase1_timeBetweenDashes = 0.7f;

    [Header("Phase 2 Settings")]
    [SerializeField] private float phase2_chargeUpTime = 0.4f;
    [SerializeField] private float phase2_dashSpeed = 22f;
    [SerializeField] private float phase2_dashDuration = 1.3f;
    [SerializeField] private float phase2_timeBetweenDashes = 0.5f;
    [SerializeField] private Color phase2_color = Color.red;
    [SerializeField] private float phase2_healthPercent = 0.5f;

    [Header("Bounces")]
    [SerializeField] private int maxBouncesBeforeStop = 6;
    [SerializeField] private float minSpeedAfterBounce = 12f;
    [SerializeField] private float maxSpeedClamp = 18f;

    [Header("Walls")]
    [SerializeField] private LayerMask wallsMask;

    [Header("Stun")]
    [SerializeField] private float stunTime = 0.8f;

    [Header("Damage On Contact")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float hitCooldown = 0.35f;

    private Rigidbody2D rb;
    private BossHealth bossHealth;
    private State state = State.Idle;
    private int bouncesLeft;
    private float lastHitTime;
    private bool isActive = false;
    private Collider2D physicalCollider;
    private bool isPhase2 = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Animator animator;

    private float ChargeUpTime => isPhase2 ? phase2_chargeUpTime : phase1_chargeUpTime;
    private float DashSpeed => isPhase2 ? phase2_dashSpeed : phase1_dashSpeed;
    private float DashDuration => isPhase2 ? phase2_dashDuration : phase1_dashDuration;
    private float TimeBetweenDashes => isPhase2 ? phase2_timeBetweenDashes : phase1_timeBetweenDashes;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<BossHealth>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

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
                Debug.Log("[BossJelly] Ignorando colisiones físicas con el Player");
            }
        }
    }

    public void ActivateBoss()
    {
        if (!isActive)
        {
            isActive = true;
            StartCoroutine(BossLoop());
        }
    }

    private IEnumerator BossLoop()
    {
        while (isActive && !bossHealth.IsDead)
        {
            CheckPhaseTransition();
            
            // 1) Preparacion - mirar hacia el jugador
            state = State.ChargeUp;
            rb.linearVelocity = Vector2.zero;
            FacePlayer();

            yield return new WaitForSeconds(ChargeUpTime);

            // 2) Dash hacia el player
            if (player != null)
            {
                Vector2 dir = ((Vector2)player.position - rb.position).normalized;
                StartDash(dir);
            }
            else
            {
                StartDash(Random.insideUnitCircle.normalized);
            }

            // 3) Mantener dash durante X tiempo (con rebotes)
            float t = 0f;
            while (t < DashDuration && state == State.Dashing)
            {
                t += Time.deltaTime;

                float spd = rb.linearVelocity.magnitude;
                float speedCap = isPhase2 ? maxSpeedClamp * 1.3f : maxSpeedClamp;
                if (spd > speedCap)
                    rb.linearVelocity = rb.linearVelocity.normalized * speedCap;

                FaceMovementDirection();
                yield return null;
            }

            // Si se quedo sin rebotes, se aturde un momento
            if (state == State.Stunned)
            {
                rb.linearVelocity = Vector2.zero;
                
                if (animator != null)
                {
                    animator.SetTrigger("Idle");
                }
                
                yield return new WaitForSeconds(stunTime);
            }

            // 4) Pausa - Idle cuando para de moverse
            state = State.Idle;
            rb.linearVelocity = Vector2.zero;
            
            if (animator != null)
            {
                animator.SetTrigger("Idle");
            }
            
            FacePlayer();
            yield return new WaitForSeconds(TimeBetweenDashes);
        }
    }
    
    private void CheckPhaseTransition()
    {
        if (!isPhase2 && bossHealth != null)
        {
            float currentHP = bossHealth.CurrentHP;
            float maxHP = bossHealth.MaxHP;
            
            if (currentHP <= maxHP * phase2_healthPercent)
            {
                isPhase2 = true;
                
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = phase2_color;
                }
                
                Debug.Log("[BossJelly] ¡FASE 2 ACTIVADA!");
            }
        }
    }
    
    private void FacePlayer()
    {
        if (player == null || spriteRenderer == null) return;
        
        Vector2 dirToPlayer = (Vector2)player.position - rb.position;
        
        if (dirToPlayer.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (dirToPlayer.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }
    
    private void FaceMovementDirection()
    {
        if (spriteRenderer == null) return;
        
        Vector2 velocity = rb.linearVelocity;
        
        if (velocity.x > 0.1f)
        {
            spriteRenderer.flipX = false;
        }
        else if (velocity.x < -0.1f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void StartDash(Vector2 dir)
    {
        state = State.Dashing;
        bouncesLeft = maxBouncesBeforeStop;

        rb.linearVelocity = dir * DashSpeed;
        
        if (animator != null)
        {
            animator.SetTrigger("Roll");
        }
        
        FaceMovementDirection();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Rebote con paredes
        if (((1 << collision.gameObject.layer) & wallsMask) != 0)
        {
            if (state != State.Dashing) return;

            bouncesLeft--;

            float spd = rb.linearVelocity.magnitude;
            if (spd < minSpeedAfterBounce)
                rb.linearVelocity = rb.linearVelocity.normalized * minSpeedAfterBounce;

            if (bouncesLeft <= 0)
            {
                state = State.Stunned;
                rb.linearVelocity = Vector2.zero;
                
                if (animator != null)
                {
                    animator.SetTrigger("Idle");
                }
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

        PlayerDashController2D dashController = playerCol.GetComponent<PlayerDashController2D>();
        if (dashController != null && dashController.IsDashing)
            return;

        lastHitTime = Time.time;

        PlayerHealth hp = playerCol.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            Vector2 knockbackDir = (playerCol.transform.position - transform.position).normalized;
            hp.TakeDamage(contactDamage, knockbackDir, 1f);
        }
    }
}

