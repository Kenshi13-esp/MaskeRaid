using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.6f;

    private float timer;

    private void Update()
    {
        if (timer > 0f)
            timer -= Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (timer > 0f) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth hp = other.GetComponent<PlayerHealth>();
        if (hp == null) return;

        hp.TakeDamage(damage);
        timer = hitCooldown;
    }
}
