using UnityEngine;

/// <summary>
/// Adapta un boss al sistema de dash compartido: usa su loop de sonido de ataque y evita
/// que el jugador y el boss se empujen fisicamente. El dano de contacto lo siguen aplicando
/// los componentes de dano del propio boss.
/// </summary>
[RequireComponent(typeof(BossHealth))]
public class BossDashActor : DashActor
{
    [Header("Boss Dash Actor")]
    [Tooltip("Vacio = se busca en este GameObject")]
    [SerializeField] private BossAttackSoundController attackSoundController;

    [Tooltip("Ignora las colisiones fisicas con el jugador para que no se empujen")]
    [SerializeField] private bool ignorePlayerCollisions = true;

    private BossHealth bossHealth;

    public override DashFaction Faction => DashFaction.Boss;

    /// <summary>Vida del boss que ejecuta el dash.</summary>
    public BossHealth Health
    {
        get
        {
            if (bossHealth == null) bossHealth = GetComponent<BossHealth>();
            return bossHealth;
        }
    }

    private void Start()
    {
        if (ignorePlayerCollisions) IgnorePlayerCollisions();
    }

    public override void PlayDashSound(SoundType soundType)
    {
        ResolveSoundController()?.StartAttackSound();
    }

    public override void StopDashSound()
    {
        ResolveSoundController()?.StopAttackSound();
    }

    private BossAttackSoundController ResolveSoundController()
    {
        if (attackSoundController == null) attackSoundController = GetComponent<BossAttackSoundController>();
        return attackSoundController;
    }

    private void IgnorePlayerCollisions()
    {
        Collider2D bossCollider = PhysicalCollider;
        if (bossCollider == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null) return;

        Physics2D.IgnoreCollision(bossCollider, playerCollider, true);
    }
}
