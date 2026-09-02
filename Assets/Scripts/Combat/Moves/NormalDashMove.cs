/// <summary>
/// Dash del ataque principal del jugador: recto, sin carga y con su propio perfil.
///
/// Es un tipo distinto de <see cref="BasicDashMove"/> a proposito, para poder convivir en el
/// mismo GameObject con el dash que aporta la mascara equipada, que es el ataque especial.
/// El movimiento en si lo resuelve <see cref="LinearDashMove"/>.
/// </summary>
public class NormalDashMove : LinearDashMove
{
}
