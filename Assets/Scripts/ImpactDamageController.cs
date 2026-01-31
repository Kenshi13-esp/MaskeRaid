using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
public class ImpactDamageController : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damage = 2;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Timing")]
    [SerializeField] private float damageActiveDelay = 0.1f;
    [SerializeField] private float damageActiveDuration = 0.3f;
    
    private CapsuleCollider2D damageCollider;
    private bool damageActive;
    private float damageTimer;

    private void Awake()
    {
        damageCollider = GetComponent<CapsuleCollider2D>();
        damageCollider.isTrigger = true;
        damageCollider.enabled = false;
    }

    private void Start()
    {
        Invoke(nameof(ActivateDamage), damageActiveDelay);
    }

    private void ActivateDamage()
    {
        damageCollider.enabled = true;
        damageActive = true;
        damageTimer = damageActiveDuration;
    }

    private void Update()
    {
        if (!damageActive) return;

        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0f)
        {
            damageActive = false;
            damageCollider.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!damageActive) return;
        
        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        PlayerDashController2D dashController = collision.GetComponent<PlayerDashController2D>();

        if (dashController != null && dashController.IsDashing) return;

        if (playerHealth != null)
        {
            Vector2 knockbackDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
            if (knockbackDir.sqrMagnitude < 0.01f)
            {
                knockbackDir = Vector2.right;
            }
            playerHealth.TakeDamage(damage, knockbackDir, knockbackForce);
        }
    }
}
