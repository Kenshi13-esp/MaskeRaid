using UnityEngine;

/// <summary>
/// Traduce un <see cref="DashMoveKind"/> en su componente de dash. Es el unico punto del
/// codigo que conoce esa correspondencia: para anadir un boss nuevo basta con crear su
/// <see cref="DashMoveBase"/>, anadir el valor al enum y registrarlo aqui.
/// </summary>
public static class DashMoveCatalog
{
    /// <summary>
    /// Devuelve el componente de dash del tipo indicado en el GameObject, anadiendolo si
    /// aun no lo tiene. Los componentes se reutilizan para no crear basura al cambiar de
    /// mascara.
    /// </summary>
    public static DashMoveBase AttachMove(GameObject owner, DashMoveKind kind)
    {
        if (owner == null) return null;

        switch (kind)
        {
            case DashMoveKind.Oniki: return GetOrAdd<OnikiDashMove>(owner);
            case DashMoveKind.Glorbo: return GetOrAdd<GlorboDashMove>(owner);
            case DashMoveKind.Qetza: return GetOrAdd<QetzaDashMove>(owner);
            default: return GetOrAdd<BasicDashMove>(owner);
        }
    }

    private static T GetOrAdd<T>(GameObject owner) where T : DashMoveBase
    {
        T existingMove = owner.GetComponent<T>();

        return existingMove != null ? existingMove : owner.AddComponent<T>();
    }
}
