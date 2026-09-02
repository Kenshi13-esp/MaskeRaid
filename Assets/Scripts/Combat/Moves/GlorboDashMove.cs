using System.Collections;
using UnityEngine;

/// <summary>
/// Dash con rebotes de Glorbo: se lanza a velocidad constante y rebota en las paredes
/// hasta agotar los rebotes o el tiempo del perfil. Lo usa tanto el boss como el jugador
/// cuando lleva la mascara de Glorbo.
/// </summary>
public class GlorboDashMove : DashMoveBase
{
    private PhysicsMaterial2D originalMaterial;
    private bool originalMaterialCached;
    private int bouncesLeft;
    private bool isBouncing;

    /// <summary>True si el dash termino porque se agotaron los rebotes.</summary>
    public bool RanOutOfBounces { get; private set; }

    protected override IEnumerator PerformDash(DashRequest request)
    {
        Rigidbody2D body = Body;

        RanOutOfBounces = false;
        bouncesLeft = Mathf.Max(1, Profile.MaxBounces);
        isBouncing = true;

        ApplyBounceMaterial(true);

        body.linearVelocity = request.Direction * Profile.DashSpeed;

        float elapsed = 0f;
        float duration = Mathf.Max(MinimumDashDuration, Profile.DashDuration);

        while (elapsed < duration && isBouncing)
        {
            yield return WaitForPhysicsStep;

            elapsed += Time.fixedDeltaTime;
            ClampBounceSpeed();
            Actor.FaceDirection(body.linearVelocity);
        }

        isBouncing = false;
        ApplyBounceMaterial(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsDashing || !isBouncing) return;
        if (!Actor.IsWall(collision.gameObject)) return;

        bouncesLeft--;

        Rigidbody2D body = Body;
        float minSpeed = Profile.MinSpeedAfterBounce;

        if (body.linearVelocity.sqrMagnitude < minSpeed * minSpeed)
        {
            body.linearVelocity = body.linearVelocity.normalized * minSpeed;
        }

        ShakeCamera();

        if (bouncesLeft <= 0)
        {
            isBouncing = false;
            RanOutOfBounces = true;
            body.linearVelocity = Vector2.zero;
        }
    }

    private void ClampBounceSpeed()
    {
        Rigidbody2D body = Body;
        float maxSpeed = Profile.MaxSpeedClamp;

        if (body.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            body.linearVelocity = body.linearVelocity.normalized * maxSpeed;
        }
    }

    private void ApplyBounceMaterial(bool apply)
    {
        PhysicsMaterial2D bounceMaterial = Profile.BounceMaterial;
        if (bounceMaterial == null) return;

        Rigidbody2D body = Body;

        if (apply)
        {
            if (!originalMaterialCached)
            {
                originalMaterial = body.sharedMaterial;
                originalMaterialCached = true;
            }

            body.sharedMaterial = bounceMaterial;
            return;
        }

        if (originalMaterialCached) body.sharedMaterial = originalMaterial;
    }
}
