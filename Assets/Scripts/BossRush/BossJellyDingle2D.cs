using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossJellyDingle2D : MonoBehaviour, IBossController
{
    public enum State { Idle, ChargeUp, Dashing, Stunned }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Dash Pattern")]
    [SerializeField] private float chargeUpTime = 0.6f;      // telegraph
    [SerializeField] private float dashSpeed = 16f;          // velocidad de embestida
    [SerializeField] private float dashDuration = 0.9f;      // cu�nto dura el �modo rebote�
    [SerializeField] private float timeBetweenDashes = 0.7f; // descanso

    [Header("Bounces")]
    [SerializeField] private int maxBouncesBeforeStop = 6;   // rebotes por dash
    [SerializeField] private float minSpeedAfterBounce = 12f;// para mantener energ�a
    [SerializeField] private float maxSpeedClamp = 18f;      // para que no acelere infinito

    [Header("Walls")]
    [SerializeField] private LayerMask wallsMask;

    [Header("Stun")]
    [SerializeField] private float stunTime = 0.8f;

    [Header("Damage On Contact")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float hitCooldown = 0.35f;

    private Rigidbody2D rb;
    private State state = State.Idle;
    private int bouncesLeft;
    private float lastHitTime;
    private bool isActive = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void ActivateBoss()
    {
        if (!isActive)
        {
            isActive = true;
            StartCoroutine(BossLoop());
        }
    }

    private IEnumerator BossLoop()
    {
        while (isActive)
        {
            // 1) Preparaci�n
            state = State.ChargeUp;
            rb.linearVelocity = Vector2.zero;

            // Aqu� puedes activar animaci�n �inflate/squash� si quieres
            yield return new WaitForSeconds(chargeUpTime);

            // 2) Dash hacia el player
            if (player != null)
            {
                Vector2 dir = ((Vector2)player.position - rb.position).normalized;
                StartDash(dir);
            }
            else
            {
                // si no hay player, dash random
                StartDash(Random.insideUnitCircle.normalized);
            }

            // 3) Mantener dash durante X tiempo (con rebotes)
            float t = 0f;
            while (t < dashDuration && state == State.Dashing)
            {
                t += Time.deltaTime;

                // Clamp velocidad para no explotar
                float spd = rb.linearVelocity.magnitude;
                if (spd > maxSpeedClamp)
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeedClamp;

                yield return null;
            }

            // Si se qued� sin rebotes, se aturde un momento
            if (state == State.Stunned)
            {
                rb.linearVelocity = Vector2.zero;
                yield return new WaitForSeconds(stunTime);
            }

            // 4) Pausa
            state = State.Idle;
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(timeBetweenDashes);
        }
    }

    private void StartDash(Vector2 dir)
    {
        state = State.Dashing;
        bouncesLeft = maxBouncesBeforeStop;

        rb.linearVelocity = dir * dashSpeed;
    }

    // Rebote controlado + contador de rebotes
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Rebote con paredes
        if (((1 << collision.gameObject.layer) & wallsMask) != 0)
        {
            if (state != State.Dashing) return;

            bouncesLeft--;

            // Refuerzo: mantener velocidad m�nima para que se sienta �Dingle�
            float spd = rb.linearVelocity.magnitude;
            if (spd < minSpeedAfterBounce)
                rb.linearVelocity = rb.linearVelocity.normalized * minSpeedAfterBounce;

            if (bouncesLeft <= 0)
            {
                // Se �revienta� / se para y se aturde
                state = State.Stunned;
                rb.linearVelocity = Vector2.zero;
            }
        }

        // Da�o por contacto al player (si choca)
        if (collision.collider.CompareTag("Player"))
        {
            TryDealDamageToPlayer(collision.collider);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            TryDealDamageToPlayer(collision.collider);
        }
    }

    private void TryDealDamageToPlayer(Collider2D playerCol)
    {
        if (Time.time < lastHitTime + hitCooldown) return;

        PlayerDashController2D dashController = playerCol.GetComponent<PlayerDashController2D>();
        if (dashController != null && dashController.IsDashing)
            return;

        lastHitTime = Time.time;

        PlayerHealth hp = playerCol.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            Vector2 knockbackDir = (playerCol.transform.position - transform.position).normalized;
            hp.TakeDamage(contactDamage, knockbackDir, 1f);
        }
    }
}

