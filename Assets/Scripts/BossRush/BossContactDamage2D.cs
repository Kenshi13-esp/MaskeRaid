using UnityEngine;

public class BossContactDamage2D : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.5f;

    private float lastHitTime;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < lastHitTime + hitCooldown) return;

        // Da�o al PLAYER
        if (other.CompareTag("Player"))
        {
            PlayerDashController2D dashController = other.GetComponent<PlayerDashController2D>();
            if (dashController != null && dashController.IsDashing)
            {
                Debug.Log("[BossContactDamage2D] Player está dasheando, no hace daño");
                return;
            }

            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                ph.TakeDamage(damage, knockbackDir, 1f);
                lastHitTime = Time.time;
                Debug.Log($"[BossContactDamage2D] Daño aplicado al player: {damage}");
            }
            else
            {
                Debug.LogWarning($"[BossContactDamage2D] Player sin componente PlayerHealth");
            }
        }
    }
}
