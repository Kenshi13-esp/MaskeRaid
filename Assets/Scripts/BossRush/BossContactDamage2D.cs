using UnityEngine;

public class BossContactDamage2D : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.5f;

    private float lastHitTime;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < lastHitTime + hitCooldown) return;

        // Daño al PLAYER
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                lastHitTime = Time.time;
            }
        }
    }
}
