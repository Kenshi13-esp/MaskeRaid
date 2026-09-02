using UnityEngine;

/// <summary>
/// Dano por contacto de un boss (o de una de sus hitbox hijas) al jugador, con cooldown
/// propio. Delega en <see cref="PlayerContact"/> para no resolver componentes ni escribir
/// trazas en cada fotograma de fisica que dura el contacto.
/// </summary>
public class BossContactDamage2D : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float knockbackForceMultiplier = 1f;

    private float lastHitTime = float.NegativeInfinity;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    private void TryDealDamage(Collider2D other)
    {
        if (Time.time < lastHitTime + hitCooldown) return;
        if (!PlayerContact.TryGetDamageablePlayer(other, out PlayerHealth playerHealth)) return;

        lastHitTime = Time.time;

        Vector2 knockbackDirection = PlayerContact.ResolveKnockbackDirection(transform.position, other.transform.position);
        playerHealth.TakeDamage(damage, knockbackDirection, knockbackForceMultiplier);
    }
}
