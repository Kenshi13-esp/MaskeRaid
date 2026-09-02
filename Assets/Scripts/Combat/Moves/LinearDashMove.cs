using System.Collections;
using UnityEngine;

/// <summary>
/// Dash en linea recta hacia un destino. Es la base de los dashes que no rebotan:
/// resuelve el destino (direccion libre o persecucion de un objetivo) y recorre la
/// distancia en el tiempo que marque el perfil.
/// </summary>
public abstract class LinearDashMove : DashMoveBase
{
    protected override IEnumerator PerformDash(DashRequest request)
    {
        Vector2 origin = Body.position;
        Vector2 destination = ResolveDestination(request);

        yield return MoveTo(destination, ResolveDuration(Vector2.Distance(origin, destination)));
    }
}
