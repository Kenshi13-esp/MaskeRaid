using UnityEngine;

public class BossContactDamage : MonoBehaviour
{
    [Header("Contact Damage Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private LayerMask playerLayer;

    private BossJumpingController bossController;
    private float lastDamageTime = -999f;

    private void Awake()
    {
        bossController = GetComponent<BossJumpingController>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        if (((1 << collision.gameObject.layer) & playerLayer) == 0) return;

        if (bossController != null && bossController.IsInAir) return;

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
