using UnityEngine;

public class BossDashDamage : MonoBehaviour
{
    [Header("Dash Damage Settings")]
    [SerializeField] private int damage = 2;
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float damageCooldown = 0.3f;
    [SerializeField] private LayerMask playerLayer;

    private CircleCollider2D dashCollider;
    private float lastDamageTime = -999f;

    private void Awake()
    {
        dashCollider = GetComponent<CircleCollider2D>();
        if (dashCollider != null)
        {
            dashCollider.isTrigger = true;
            dashCollider.enabled = false;
        }
    }

    public void EnableDashCollider(bool enable)
    {
        if (dashCollider != null)
        {
            dashCollider.enabled = enable;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        PlayerDashController2D playerDash = collision.gameObject.GetComponent<PlayerDashController2D>();

        if (playerHealth != null)
        {
            if (playerDash != null && playerDash.IsDashing)
            {
                return;
            }

            Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
            playerHealth.TakeDamage(damage, knockbackDir, knockbackForce);
            lastDamageTime = Time.time;
        }
    }
}
