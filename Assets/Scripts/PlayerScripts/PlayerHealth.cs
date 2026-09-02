using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Vida del jugador, retroceso al recibir dano e invulnerabilidades. Notifica los cambios de
/// vida por evento para que el HUD no la consulte cada frame, y se publica de forma estatica
/// para que los componentes de dano no tengan que resolverla con GetComponent en cada
/// fotograma de fisica.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    private const float BlinkSpeed = 0.1f;
    private const float KnockbackDamping = 0.96f;

    private static readonly WaitForSeconds BlinkWait = new WaitForSeconds(BlinkSpeed);

    [Header("Settings")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] private float sideForce = 18f;
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float postLandingInvincibility = 0.2f;

    [Header("Post-Dash Invincibility")]
    [Tooltip("Tiempo de invulnerabilidad despues del dash (segundos)")]
    [SerializeField] private float postDashInvincibilityTime = 0.15f;

    [Header("Vibracion al recibir dano")]
    [Tooltip("Intensidad de la vibracion del mando al recibir un impacto")]
    [Range(0f, 1f)]
    [SerializeField] private float hitRumbleIntensity = 0.4f;

    [Tooltip("Duracion de la vibracion al recibir un impacto (segundos reales)")]
    [SerializeField] private float hitRumbleDuration = 0.12f;

    [Header("Events")]
    public UnityEvent OnPlayerDeath;

    private int hp;
    private Rigidbody2D rb;
    private PlayerDashController2D dashController;
    private SpriteRenderer spriteRenderer;
    private Transform spriteTransform;

    private WaitForSeconds postDashWait;
    private Coroutine launchRoutine;
    private Coroutine invincibilityRoutine;

    private bool isLaunched;
    private bool isInvincible;
    private bool isDashInvincible;
    private bool isDead;

    /// <summary>Jugador activo en la escena, o null si no hay ninguno.</summary>
    public static PlayerHealth Active { get; private set; }

    /// <summary>Se dispara al cambiar la vida (vida actual, vida maxima).</summary>
    public event Action<int, int> HealthChanged;

    public bool IsLaunched => isLaunched;
    public bool IsDead => isDead;
    public int CurrentHP => hp;
    public int MaxHP => maxHP;

    /// <summary>True mientras el jugador esta dasheando. Lo consultan los danos por contacto.</summary>
    public bool IsDashing => dashController != null && dashController.IsDashing;

    /// <summary>True si ahora mismo el jugador no puede recibir dano.</summary>
    public bool IsInvulnerable => isLaunched || isInvincible || isDashInvincible;

    private void Awake()
    {
        hp = maxHP;
        rb = GetComponent<Rigidbody2D>();
        dashController = GetComponent<PlayerDashController2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null) spriteTransform = spriteRenderer.transform;

        postDashWait = new WaitForSeconds(postDashInvincibilityTime);

        if (OnPlayerDeath == null) OnPlayerDeath = new UnityEvent();
    }

    private void OnEnable()
    {
        Active = this;
    }

    private void OnDisable()
    {
        if (Active == this) Active = null;
    }

    private void Start()
    {
        HealthChanged?.Invoke(hp, maxHP);
    }

    /// <summary>Aplica dano al jugador y lo lanza en la direccion del golpe.</summary>
    public void TakeDamage(int amount, Vector2 knockbackDir, float forceMultiplier, bool ignoreInvincibility = false)
    {
        if (isDead) return;
        if (!ignoreInvincibility && IsInvulnerable) return;

        hp = Mathf.Max(0, hp - amount);

        SoundManager.PlaySound(SoundType.PLAYER_HIT);
        HealthChanged?.Invoke(hp, maxHP);
        StartCoroutine(HitRumbleRoutine());

        if (hp <= 0)
        {
            Die();
            return;
        }

        StopHealthRoutines();
        launchRoutine = StartCoroutine(ParabolicLaunch(knockbackDir, forceMultiplier));
    }

    /// <summary>Restaura la vida al maximo. Se usa entre rondas del boss rush.</summary>
    public void HealToFull()
    {
        hp = maxHP;
        HealthChanged?.Invoke(hp, maxHP);
    }

    /// <summary>Concede una ventana breve de invulnerabilidad al terminar un dash.</summary>
    public void GrantPostDashInvincibility()
    {
        if (isLaunched || isDead) return;
        if (invincibilityRoutine != null) return;

        invincibilityRoutine = StartCoroutine(PostDashInvincibilityRoutine());
    }

    /// <summary>Activa o desactiva la invulnerabilidad mientras el dash esta en curso.</summary>
    public void SetDashInvincibility(bool invincible)
    {
        isDashInvincible = invincible;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        StopHealthRoutines();

        if (dashController != null) dashController.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        OnPlayerDeath?.Invoke();
    }

    /// <summary>
    /// Corta el retroceso y el parpadeo en curso dejando al jugador visible. Sustituye al
    /// StopAllCoroutines anterior, que podia dejar el sprite apagado o la invulnerabilidad
    /// encendida para siempre si se interrumpia a mitad de parpadeo.
    /// </summary>
    private void StopHealthRoutines()
    {
        if (launchRoutine != null)
        {
            StopCoroutine(launchRoutine);
            launchRoutine = null;
        }

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        isInvincible = false;

        if (spriteRenderer != null && !spriteRenderer.enabled) spriteRenderer.enabled = true;
    }

    private IEnumerator ParabolicLaunch(Vector2 direction, float forceMultiplier)
    {
        isLaunched = true;
        if (dashController != null) dashController.EndDashState();

        Vector3 startLocalPosition = spriteTransform != null ? spriteTransform.localPosition : Vector3.zero;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(Mathf.Sign(direction.x) * sideForce * forceMultiplier, 0f), ForceMode2D.Impulse);

        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / knockbackDuration;

            if (spriteTransform != null)
            {
                float heightOffset = 4f * progress * (1f - progress) * jumpHeight;
                spriteTransform.localPosition = new Vector3(
                    startLocalPosition.x,
                    startLocalPosition.y + heightOffset,
                    startLocalPosition.z);
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x * KnockbackDamping, rb.linearVelocity.y);

            yield return null;
        }

        if (spriteTransform != null) spriteTransform.localPosition = startLocalPosition;

        rb.linearVelocity = Vector2.zero;
        isLaunched = false;
        launchRoutine = null;

        invincibilityRoutine = StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float timer = 0f;

        while (timer < postLandingInvincibility)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return BlinkWait;
            timer += BlinkSpeed;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        isInvincible = false;
        invincibilityRoutine = null;
    }

    private IEnumerator PostDashInvincibilityRoutine()
    {
        isInvincible = true;

        yield return postDashWait;

        isInvincible = false;
        invincibilityRoutine = null;
    }

    /// <summary>Golpe corto de vibracion del mando al recibir un impacto del boss.</summary>
    private IEnumerator HitRumbleRoutine()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null || hitRumbleIntensity <= 0f || hitRumbleDuration <= 0f) yield break;

        gamepad.SetMotorSpeeds(hitRumbleIntensity, hitRumbleIntensity);

        // En tiempo real para que se note igual aunque el impacto dispare un hit stop.
        yield return new WaitForSecondsRealtime(hitRumbleDuration);

        if (Gamepad.current == gamepad) gamepad.ResetHaptics();
    }
}
