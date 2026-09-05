using System.Collections;
using UnityEngine;

/// <summary>
/// IA de Glorbo: se prepara, se lanza rebotando por la arena y se aturde al agotar los
/// rebotes. El movimiento lo ejecuta <see cref="GlorboDashMove"/>, el mismo componente que
/// hereda el jugador cuando equipa la mascara de Glorbo.
/// </summary>
[RequireComponent(typeof(BossHealth), typeof(BossDashActor), typeof(GlorboDashMove))]
public class BossGlorboController : MonoBehaviour, IBossController
{
    private const string IdleTrigger = "Idle";
    private const string AnticipationTrigger = "Anticipation";

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Fase 1")]
    [SerializeField] private float phase1_chargeUpTime = 0.6f;
    [SerializeField] private float phase1_timeBetweenDashes = 0.7f;

    [Header("Fase 2")]
    [SerializeField] private float phase2_chargeUpTime = 0.4f;
    [SerializeField] private float phase2_timeBetweenDashes = 0.5f;
    [SerializeField] private Color phase2_color = Color.red;

    [Tooltip("Fraccion de vida a la que entra en fase 2 (0.5 = a la mitad justa)")]
    [Range(0f, 1f)]
    [SerializeField] private float phase2_healthPercent = 0.5f;
    [Tooltip("Perfil de dash que se aplica al entrar en fase 2. Vacio = mantiene el de fase 1")]
    [SerializeField] private DashProfile phase2DashProfile;

    [Header("Stun")]
    [Tooltip("Tiempo aturdido tras agotar los rebotes del dash")]
    [SerializeField] private float stunTime = 0.8f;

    [Header("Damage On Contact")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float hitCooldown = 0.35f;

    private BossHealth bossHealth;
    private BossDashActor dashActor;
    private GlorboDashMove dashMove;

    private bool isActive;
    private bool isPhase2;
    private float lastHitTime;

    private float ChargeUpTime => isPhase2 ? phase2_chargeUpTime : phase1_chargeUpTime;
    private float TimeBetweenDashes => isPhase2 ? phase2_timeBetweenDashes : phase1_timeBetweenDashes;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
        dashActor = GetComponent<BossDashActor>();
        dashMove = GetComponent<GlorboDashMove>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
    }

    /// <summary>Arranca el patron de ataque del boss.</summary>
    public void ActivateBoss()
    {
        if (isActive || bossHealth.IsDead) return;

        isActive = true;
        StartCoroutine(BossLoop());
    }

    /// <summary>
    /// El cambio de fase se comprueba cada fotograma y no al empezar cada embestida: asi entra
    /// en fase 2 en el golpe exacto que baja la vida a la mitad, sin esperar a que acabe el
    /// ciclo de ataque en curso.
    /// </summary>
    private void Update()
    {
        if (!isActive || isPhase2 || bossHealth.IsDead) return;

        CheckPhaseTransition();
    }

    private IEnumerator ChargeAttack(float chargeTime)
    {
        if (player != null) dashActor.FaceDirection(player.position - transform.position);

        dashActor.SetAnimatorTrigger(AnticipationTrigger);

        yield return new WaitForSeconds(chargeTime);
    }

    private IEnumerator BossLoop()
    {
        while (isActive && !bossHealth.IsDead)
        {
            FacePlayer();
            yield return ChargeAttack(ChargeUpTime);

            yield return dashMove.Execute(BuildDashRequest());

            if (dashMove.RanOutOfBounces)
            {
                dashActor.SetAnimatorTrigger(IdleTrigger);
                yield return new WaitForSeconds(stunTime);
            }

            dashActor.SetAnimatorTrigger(IdleTrigger);
            FacePlayer();

            yield return new WaitForSeconds(TimeBetweenDashes);
        }
    }

    private DashRequest BuildDashRequest()
    {
        if (player == null) return DashRequest.InDirection(Random.insideUnitCircle.normalized);

        Vector2 origin = dashActor.Body.position;
        return DashRequest.InDirection((Vector2)player.position - origin);
    }

    private void CheckPhaseTransition()
    {
        if (isPhase2) return;
        if (!bossHealth.IsAtOrBelowRatio(phase2_healthPercent)) return;

        isPhase2 = true;

        SoundManager.PlaySound(SoundType.BOSS_PHASE_CHANGE);

        ApplyPhase2Color();

        if (phase2DashProfile != null) dashMove.Profile = phase2DashProfile;
    }

    /// <summary>
    /// Tine al boss a traves del destello de daño. La fase cambia en el mismo fotograma del
    /// golpe, asi que el destello esta activo: escribir el color a pelo se perderia al
    /// restaurarse el destello.
    /// </summary>
    private void ApplyPhase2Color()
    {
        SpriteRenderer spriteRenderer = dashActor.SpriteRenderer;
        if (spriteRenderer == null) return;

        DamageFlashEffect damageFlash = GetComponent<DamageFlashEffect>();

        if (damageFlash != null) damageFlash.SetBaseColor(spriteRenderer, phase2_color);
        else spriteRenderer.color = phase2_color;
    }

    private void FacePlayer()
    {
        if (player == null) return;

        dashActor.FaceDirection((Vector2)player.position - dashActor.Body.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamageToPlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealDamageToPlayer(collision.collider);
    }

    private void TryDealDamageToPlayer(Collider2D other)
    {
        if (Time.time < lastHitTime + hitCooldown) return;
        if (!PlayerContact.TryGetDamageablePlayer(other, out PlayerHealth playerHealth)) return;

        lastHitTime = Time.time;

        Vector2 knockbackDirection = PlayerContact.ResolveKnockbackDirection(transform.position, other.transform.position);
        playerHealth.TakeDamage(contactDamage, knockbackDirection, 1f);
    }
}
