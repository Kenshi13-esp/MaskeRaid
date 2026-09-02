using UnityEngine;

/// <summary>
/// Reglas compartidas del dano por contacto al jugador. Centraliza la comprobacion
/// "es el jugador y ahora mismo puede recibir dano" que antes repetia cada componente de
/// dano, y la resuelve con la referencia estatica <see cref="PlayerHealth.Active"/> en lugar
/// de dos GetComponent por cada fotograma de fisica en contacto.
/// </summary>
public static class PlayerContact
{
    private const string PlayerTag = "Player";
    private const float MinimumSqrDistance = 0.0001f;

    /// <summary>
    /// Devuelve true si el collider es el jugador y no esta muerto ni dasheando. No comprueba
    /// la invulnerabilidad a proposito: un golpe absorbido debe seguir consumiendo el cooldown
    /// del atacante, como hacia el codigo original, para no encadenar golpes justo al acabar el
    /// parpadeo. De descartar el dano se encarga <see cref="PlayerHealth.TakeDamage"/>.
    /// </summary>
    public static bool TryGetDamageablePlayer(Collider2D collider, out PlayerHealth playerHealth)
    {
        playerHealth = null;

        if (collider == null || !collider.CompareTag(PlayerTag)) return false;

        PlayerHealth active = PlayerHealth.Active;
        if (active == null || active.IsDead || active.IsDashing) return false;

        playerHealth = active;
        return true;
    }

    /// <summary>Direccion de retroceso desde el origen del golpe hacia el jugador.</summary>
    public static Vector2 ResolveKnockbackDirection(Vector2 origin, Vector2 playerPosition)
    {
        Vector2 direction = playerPosition - origin;

        return direction.sqrMagnitude > MinimumSqrDistance ? direction.normalized : Vector2.right;
    }
}
