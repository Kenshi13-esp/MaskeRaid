using System.Collections;
using UnityEngine;

/// <summary>
/// Carga horizontal de Qetza: atraviesa la arena en el eje X hasta detenerse justo antes
/// del muro, ignorando la carga acumulada. El boss la usa tras alinearse con el jugador y
/// el jugador la hereda con la mascara de Qetza.
/// </summary>
public class QetzaDashMove : LinearDashMove
{
    protected override Vector2 ResolveDestination(DashRequest request)
    {
        Vector2 origin = Body.position;
        float horizontalSign = request.Direction.x >= 0f ? 1f : -1f;
        Vector2 direction = new Vector2(horizontalSign, 0f);

        Actor.FaceDirection(direction);

        return ClampDestinationToWalls(origin, origin + direction * Profile.MaxDashDistance);
    }

    protected override IEnumerator PerformDash(DashRequest request)
    {
        yield return base.PerformDash(request);

        ShakeCamera();
    }
}
