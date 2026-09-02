using System.Collections;
using UnityEngine;

/// <summary>
/// IA de Oniki: telegrafia el ataque y lanza embestidas contra el jugador. El movimiento del
/// dash lo ejecuta <see cref="OnikiDashMove"/>, el mismo componente que hereda el jugador
/// cuando equipa la mascara de Oniki.
/// </summary>
[RequireComponent(typeof(BossHealth), typeof(BossDashActor), typeof(OnikiDashMove))]
public class BossOnikiController : MonoBehaviour, IBossController
{
    private const string AnticipationTrigger = "Anticipation";

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Phase Management")]
    [Tooltip("Fraccion de vida a la que entra en fase 2 (0.5 = a la mitad justa)")]
    [Range(0f, 1f)]
    [SerializeField] private float phase2HealthPercent = 0.5f;
    [SerializeField] private float phaseTransitionDelay = 1f;
    [Tooltip("Perfil de dash que se aplica al entrar en fase 2. Vacio = mantiene el de fase 1")]
    [SerializeField] private DashProfile phase2DashProfile;
    [SerializeField] private Color phase2Color = Color.red;

    [Header("Fase 1")]
    [SerializeField] private float phase1_chargeTime = 1.5f;
    [SerializeField] private float phase1_recoveryTime = 1f;
    [SerializeField] private float phase1_timeBetweenAttacks = 0.5f;

    [Header("Fase 2")]
    [SerializeField] private float phase2_chargeTime = 2f;
    [SerializeField] private float phase2_delayBetweenDashes = 0.15f;
    [SerializeField] private float phase2_recoveryTime = 1.2f;
    [SerializeField] private float phase2_timeBetweenAttacks = 0.8f;

    [Header("Damage On Contact")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private float knockbackForceMultiplier = 1f;

    private BossHealth bossHealth;
    private BossDashActor dashActor;
    private OnikiDashMove dashMove;
    private DamageFlashEffect damageFlashEffect;

    private bool isActive;
    private bool isPhase2;
    private float lastHitTime;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
        dashActor = GetComponent<BossDashActor>();
        dashMove = GetComponent<OnikiDashMove>();
        damageFlashEffect = GetComponent<DamageFlashEffect>();

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

        if (player == null)
        {
            Debug.LogError("[BossOniki] No se encontro el Player. El boss no se activa.", this);
            return;
        }

        isActive = true;
        StartCoroutine(BossLoop());
    }

    private void Update()
    {
        if (!isActive || isPhase2 || bossHealth.IsDead) return;

        if (bossHealth.IsAtOrBelowRatio(phase2HealthPercent)) StartCoroutine(TransitionToPhase2());
    }

    private IEnumerator BossLoop()
    {
        while (isActive && !bossHealth.IsDead)
        {
            yield return isPhase2 ? Phase2Attack() : Phase1Attack();
        }
    }

    private IEnumerator Phase1Attack()
    {
        yield return new WaitForSeconds(phase1_timeBetweenAttacks);

        yield return ChargeAttack(phase1_chargeTime);
        yield return ExecuteDash();
        yield return new WaitForSeconds(phase1_recoveryTime);
    }

    private IEnumerator Phase2Attack()
    {
        yield return new WaitForSeconds(phase2_timeBetweenAttacks);

        yield return ChargeAttack(phase2_chargeTime);
        yield return ExecuteDash();

        yield return ChargeAttack(phase2_delayBetweenDashes);
        yield return ExecuteDash();

        yield return new WaitForSeconds(phase2_recoveryTime);
    }

    private IEnumerator ChargeAttack(float chargeTime)
    {
        if (player != null) dashActor.FaceDirection(player.position - transform.position);

        dashActor.SetAnimatorTrigger(AnticipationTrigger);

        yield return new WaitForSeconds(chargeTime);
    }

    private IEnumerator ExecuteDash()
    {
        if (player == null || bossHealth.IsDead) yield break;

        yield return dashMove.Execute(DashRequest.Towards(dashActor.Body.position, player));
    }

    private IEnumerator TransitionToPhase2()
    {
        isPhase2 = true;

        SoundManager.PlaySound(SoundType.BOSS_PHASE_CHANGE);

        if (dashActor.SpriteRenderer != null)
        {
            // A traves del destello: la fase cambia en el fotograma del golpe, con el destello
            // activo, y un color escrito a pelo se perderia al restaurarse.
            if (damageFlashEffect != null) damageFlashEffect.SetBaseColor(dashActor.SpriteRenderer, phase2Color);
            else dashActor.SpriteRenderer.color = phase2Color;
        }

        if (phase2DashProfile != null) dashMove.Profile = phase2DashProfile;

        yield return new WaitForSeconds(phaseTransitionDelay);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamageToPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealDamageToPlayer(other);
    }

    private void TryDealDamageToPlayer(Collider2D other)
    {
        if (Time.time < lastHitTime + hitCooldown) return;
        if (!PlayerContact.TryGetDamageablePlayer(other, out PlayerHealth playerHealth)) return;

        lastHitTime = Time.time;

        Vector2 knockbackDirection = PlayerContact.ResolveKnockbackDirection(transform.position, other.transform.position);
        playerHealth.TakeDamage(contactDamage, knockbackDirection, knockbackForceMultiplier);
    }
}
