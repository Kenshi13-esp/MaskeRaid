using UnityEngine;

/// <summary>
/// Ventana de dano de un impacto en area (la onda del salto de Qetza): se abre tras un
/// retardo, dura un tiempo fijo y se cierra. La ventana se programa con Invoke en lugar de
/// descontar un temporizador en Update cada fotograma.
/// </summary>
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

    private void Awake()
    {
        damageCollider = GetComponent<CapsuleCollider2D>();
        damageCollider.isTrigger = true;
        damageCollider.enabled = false;
    }

    private void OnEnable()
    {
        Invoke(nameof(ActivateDamage), damageActiveDelay);
    }

    private void OnDisable()
    {
        CancelInvoke();
        DeactivateDamage();
    }

    private void ActivateDamage()
    {
        damageActive = true;
        damageCollider.enabled = true;

        Invoke(nameof(DeactivateDamage), damageActiveDuration);
    }

    private void DeactivateDamage()
    {
        damageActive = false;

        if (damageCollider != null) damageCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!damageActive) return;
        if (((1 << other.gameObject.layer) & playerLayer.value) == 0) return;
        if (!PlayerContact.TryGetDamageablePlayer(other, out PlayerHealth playerHealth)) return;

        Vector2 knockbackDirection = PlayerContact.ResolveKnockbackDirection(transform.position, other.transform.position);
        playerHealth.TakeDamage(damage, knockbackDirection, knockbackForce);
    }
}
