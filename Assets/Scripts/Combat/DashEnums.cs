/// <summary>Bando de un <see cref="DashActor"/>. Determina a quien hace dano su dash.</summary>
public enum DashFaction
{
    Player,
    Boss
}

/// <summary>Forma en la que un actor se orienta hacia la direccion de su dash.</summary>
public enum DashFacingMode
{
    /// <summary>No se reorienta.</summary>
    None,

    /// <summary>Invierte el flipX del SpriteRenderer.</summary>
    SpriteFlipX,

    /// <summary>Invierte el signo de la escala X del GameObject raiz.</summary>
    RootScaleX,

    /// <summary>Invierte el signo de la escala X del transform del sprite.</summary>
    SpriteScaleX
}

/// <summary>
/// Dash disponible en el juego. Cada valor se corresponde con un <see cref="DashMoveBase"/>.
/// Para anadir un boss nuevo: crea su componente de dash, anade su valor aqui y registralo
/// en <see cref="DashMoveCatalog"/>.
/// </summary>
public enum DashMoveKind
{
    /// <summary>Dash recto basico del jugador sin mascara.</summary>
    Basic,

    /// <summary>Embestida pesada de Oniki.</summary>
    Oniki,

    /// <summary>Dash con rebotes de Glorbo.</summary>
    Glorbo,

    /// <summary>Carga horizontal de pared a pared de Qetza.</summary>
    Qetza
}
