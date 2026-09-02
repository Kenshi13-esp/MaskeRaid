using System.Collections;

/// <summary>
/// Embestida pesada de Oniki: una carga recta y comprometida que remata con un impacto
/// que sacude la camara. El boss la lanza contra el jugador (persiguiendo su posicion) y el
/// jugador la usa con la mascara de Oniki (hacia donde se mueve, con alcance por carga).
/// </summary>
public class OnikiDashMove : LinearDashMove
{
    protected override IEnumerator PerformDash(DashRequest request)
    {
        yield return base.PerformDash(request);

        ShakeCamera();
    }
}
