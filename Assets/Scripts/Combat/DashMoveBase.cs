using System.Collections;
using UnityEngine;

/// <summary>
/// Base de todos los dashes del juego. Resuelve el ciclo de vida comun (orientacion,
/// invulnerabilidad, ventana de dano, sonido, animacion y VFX) y delega el movimiento
/// concreto en cada subclase.
///
/// El componente no sabe si lo lleva un boss o el jugador: obtiene esa informacion del
/// <see cref="DashActor"/> del GameObject, por lo que el mismo script vale como ataque de
/// boss y como dash del jugador cuando lleva la mascara de ese boss.
/// </summary>
[RequireComponent(typeof(DashActor))]
public abstract class DashMoveBase : MonoBehaviour
{
    protected const float MinimumDashDuration = 0.01f;

    /// <summary>
    /// Espera de paso de fisica compartida. Se reutiliza en lugar de instanciarla en cada
    /// iteracion del bucle del dash, que asignaba memoria en cada fotograma de fisica.
    /// </summary>
    protected static readonly WaitForFixedUpdate WaitForPhysicsStep = new WaitForFixedUpdate();

    [Header("Dash Move")]
    [Tooltip("Perfil de ajustes. En el jugador lo asigna la mascara equipada")]
    [SerializeField] private DashProfile profile;

    private DashActor actor;
    private Coroutine dashRoutine;

    /// <summary>Perfil activo. La mascara del jugador o la fase del boss puede sustituirlo.</summary>
    public DashProfile Profile
    {
        get => profile;
        set => profile = value;
    }

    /// <summary>Actor que ejecuta este dash.</summary>
    public DashActor Actor
    {
        get
        {
            if (actor == null) actor = GetComponent<DashActor>();
            return actor;
        }
    }

    /// <summary>True mientras el dash esta en curso.</summary>
    public bool IsDashing { get; private set; }

    /// <summary>Direccion del dash en curso o del ultimo ejecutado.</summary>
    public Vector2 CurrentDirection { get; private set; } = Vector2.right;

    protected Rigidbody2D Body => Actor != null ? Actor.Body : null;

    /// <summary>
    /// Lanza el dash. Devuelve la corrutina para que una IA pueda esperar a que termine
    /// con <c>yield return move.Execute(...)</c>.
    /// </summary>
    public Coroutine Execute(DashRequest request)
    {
        if (IsDashing) return dashRoutine;

        if (profile == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Falta el DashProfile en '{name}'. El dash no se ejecuta.", this);
            return null;
        }

        if (Actor == null || Body == null) return null;

        dashRoutine = StartCoroutine(RunDash(request));
        return dashRoutine;
    }

    /// <summary>Interrumpe el dash en curso y devuelve al actor a su estado normal.</summary>
    public void CancelDash()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        if (IsDashing) FinishDash();
    }

    protected virtual void OnDisable()
    {
        CancelDash();
    }

    /// <summary>Movimiento concreto del dash. Cada boss implementa el suyo.</summary>
    protected abstract IEnumerator PerformDash(DashRequest request);

    /// <summary>Prepara el actor al inicio del dash.</summary>
    protected virtual void BeginDash(DashRequest request)
    {
        Actor.FaceDirection(request.Direction);
        Actor.SetInvulnerable(true);
        Actor.SetDashDamageActive(true, profile.DamageMultiplier);
        Actor.PlayDashSound(profile.DashSoundType);
        Actor.SetAnimatorTrigger(profile.DashAnimatorTrigger);
        SpawnDashVfx();
    }

    /// <summary>Devuelve el actor a su estado normal al terminar o cancelar el dash.</summary>
    protected virtual void FinishDash()
    {
        IsDashing = false;

        if (Actor == null) return;

        if (Body != null) Body.linearVelocity = Vector2.zero;

        Actor.SetDashDamageActive(false, 1f);
        Actor.SetInvulnerable(false);
        Actor.StopDashSound();

        if (profile != null) Actor.SetAnimatorTrigger(profile.EndAnimatorTrigger);
    }

    /// <summary>Calcula el destino del dash. Las subclases pueden redefinir la regla.</summary>
    protected virtual Vector2 ResolveDestination(DashRequest request)
    {
        Vector2 origin = Body.position;
        Vector2 destination;

        if (request.HasTarget)
        {
            Vector2 toTarget = (Vector2)request.Target.position - origin;
            float reachableDistance = Mathf.Max(0f, toTarget.magnitude - profile.StopDistanceFromTarget);
            destination = origin + request.Direction * reachableDistance;
        }
        else
        {
            destination = origin + request.Direction * ResolveDashDistance(request.ChargeRatio);
        }

        return profile.PierceWalls ? destination : ClampDestinationToWalls(origin, destination);
    }

    /// <summary>Distancia recorrida segun la carga acumulada.</summary>
    protected float ResolveDashDistance(float chargeRatio)
    {
        return Mathf.Lerp(profile.MinDashDistance, profile.MaxDashDistance, Mathf.Clamp01(chargeRatio));
    }

    /// <summary>Recorta el destino para no atravesar paredes.</summary>
    protected Vector2 ClampDestinationToWalls(Vector2 origin, Vector2 destination)
    {
        Vector2 delta = destination - origin;
        float distance = delta.magnitude;

        if (distance <= Mathf.Epsilon) return destination;

        Vector2 direction = delta / distance;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, Actor.WallMask);

        return hit.collider != null ? hit.point - direction * profile.WallStopSkin : destination;
    }

    /// <summary>Duracion del recorrido segun el perfil (fija o derivada de la velocidad).</summary>
    protected float ResolveDuration(float distance)
    {
        if (!profile.UseFixedSpeed || profile.DashSpeed <= 0f)
        {
            return Mathf.Max(MinimumDashDuration, profile.DashDuration);
        }

        return Mathf.Max(MinimumDashDuration, distance / profile.DashSpeed);
    }

    /// <summary>Desplaza el cuerpo hasta el destino de forma continua en pasos de fisica.</summary>
    protected IEnumerator MoveTo(Vector2 destination, float duration)
    {
        Rigidbody2D body = Body;
        Vector2 origin = body.position;

        if (duration <= MinimumDashDuration)
        {
            body.MovePosition(destination);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return WaitForPhysicsStep;

            elapsed += Time.fixedDeltaTime;
            body.MovePosition(Vector2.Lerp(origin, destination, Mathf.Clamp01(elapsed / duration)));
        }

        body.MovePosition(destination);
    }

    /// <summary>Sacude la camara segun el perfil. Se usa en el impacto de cada dash.</summary>
    protected void ShakeCamera()
    {
        if (profile == null || profile.CameraShakeDuration <= 0f) return;
        CameraShake.Shake(profile.CameraShakeDuration, profile.CameraShakeMagnitude);
    }

    private IEnumerator RunDash(DashRequest request)
    {
        IsDashing = true;
        CurrentDirection = request.Direction;

        BeginDash(request);

        yield return PerformDash(request);

        dashRoutine = null;
        FinishDash();
    }

    private void SpawnDashVfx()
    {
        GameObject vfxPrefab = profile.DashVfxPrefab;
        if (vfxPrefab == null) return;

        GameObject vfx = Instantiate(vfxPrefab, Actor.VfxSpawnPoint.position, Quaternion.identity);

        SpriteRenderer vfxRenderer = vfx.GetComponentInChildren<SpriteRenderer>();
        if (vfxRenderer != null && Actor.SpriteRenderer != null)
        {
            vfxRenderer.flipX = Actor.SpriteRenderer.flipX;
        }

        if (profile.DashVfxLifetime > 0f) Destroy(vfx, profile.DashVfxLifetime);
    }
}
