using UnityEngine;

/// <summary>
/// Peticion de dash: hacia donde se lanza y con cuanta carga. Es el unico dato que
/// diferencia a la IA de un boss (que apunta a un objetivo) de la entrada del jugador
/// (que apunta hacia donde se mueve), de modo que ambos comparten el mismo dash.
/// </summary>
public readonly struct DashRequest
{
    /// <summary>Direccion normalizada del dash.</summary>
    public readonly Vector2 Direction;

    /// <summary>Carga acumulada, entre 0 y 1. Escala la distancia recorrida.</summary>
    public readonly float ChargeRatio;

    /// <summary>Objetivo opcional al que perseguir. Si es null, el dash es libre.</summary>
    public readonly Transform Target;

    private DashRequest(Vector2 direction, float chargeRatio, Transform target)
    {
        Direction = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
        ChargeRatio = Mathf.Clamp01(chargeRatio);
        Target = target;
    }

    /// <summary>True si el dash debe dirigirse a un objetivo concreto.</summary>
    public bool HasTarget => Target != null;

    /// <summary>Crea un dash libre en una direccion con la carga indicada.</summary>
    public static DashRequest InDirection(Vector2 direction, float chargeRatio = 1f)
    {
        return new DashRequest(direction, chargeRatio, null);
    }

    /// <summary>Crea un dash dirigido a un objetivo desde la posicion de origen indicada.</summary>
    public static DashRequest Towards(Vector2 origin, Transform target, float chargeRatio = 1f)
    {
        Vector2 direction = target != null ? (Vector2)target.position - origin : Vector2.right;
        return new DashRequest(direction, chargeRatio, target);
    }
}
