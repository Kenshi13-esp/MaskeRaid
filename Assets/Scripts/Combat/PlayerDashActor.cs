using UnityEngine;

/// <summary>
/// Adapta al jugador al sistema de dash compartido: abre la hitbox de dano contra los
/// bosses, gestiona la invulnerabilidad durante el dash y reproduce el sonido puntual.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerDashActor : DashActor
{
    [Header("Player Dash Actor")]
    [Tooltip("Hitbox hija que aplica el dano del dash a los bosses")]
    [SerializeField] private DashHitbox2D dashHitbox;

    private PlayerHealth playerHealth;

    public override DashFaction Faction => DashFaction.Player;

    private PlayerHealth Health
    {
        get
        {
            if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
            return playerHealth;
        }
    }

    public override void SetDashDamageActive(bool active, DashProfile profile)
    {
        if (dashHitbox == null) return;

        if (active) dashHitbox.BeginDash(profile);
        else dashHitbox.EndDash();
    }

    /// <summary>
    /// Marca el siguiente ataque como especial, para que la hitbox aplique el dano y el golpeo
    /// fijos del especial en lugar de los del perfil de la mascara.
    /// </summary>
    public void SetSpecialDash(bool isSpecial)
    {
        if (dashHitbox == null) return;

        dashHitbox.SetSpecialDash(isSpecial);
    }

    public override void SetInvulnerable(bool invulnerable)
    {
        if (Health == null) return;

        Health.SetDashInvincibility(invulnerable);

        if (!invulnerable) Health.GrantPostDashInvincibility();
    }

    public override void PlayDashSound(SoundType soundType)
    {
        SoundManager.PlaySound(soundType);
    }
}
