using UnityEngine;

/// <summary>
/// Camara lenta sostenida. La usa la carga del ataque especial: al contrario que el dash
/// normal, el especial es un recurso con cooldown, asi que dilatar el tiempo mientras se
/// apunta es un momento puntual y no un impuesto en cada ataque.
///
/// Es uno de los tres sistemas que escriben <see cref="Time.timeScale"/> y el reparto de
/// mando entre ellos es estricto: <see cref="GamePause"/> tiene prioridad absoluta y
/// <see cref="HitStop"/> puede pisar la camara lenta durante unos fotogramas, porque al
/// terminar restaura precisamente la escala que se pida aqui.
/// </summary>
public static class SlowMotion
{
    private const float NormalTimeScale = 1f;
    private const float MinScale = 0.02f;

    private static float defaultFixedDeltaTime = 0.02f;
    private static float activeScale = NormalTimeScale;

    /// <summary>True mientras la camara lenta esta pedida.</summary>
    public static bool IsActive { get; private set; }

    /// <summary>Escala de tiempo que corresponde al estado actual, sin contar pausa ni hit stop.</summary>
    public static float CurrentScale => IsActive ? activeScale : NormalTimeScale;

    /// <summary>Activa la camara lenta a la escala indicada.</summary>
    public static void Begin(float scale)
    {
        activeScale = Mathf.Clamp(scale, MinScale, NormalTimeScale);
        IsActive = true;

        if (GamePause.IsGameplayBlocked) return;

        Time.timeScale = activeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * activeScale;
    }

    /// <summary>Devuelve el tiempo a su velocidad normal.</summary>
    public static void End()
    {
        if (!IsActive) return;

        IsActive = false;
        activeScale = NormalTimeScale;

        Time.fixedDeltaTime = defaultFixedDeltaTime;

        // Con una pausa o una congelacion de impacto en curso, el timeScale es suyo: el hit
        // stop ya restaurara la velocidad normal al terminar porque lee CurrentScale.
        if (GamePause.IsGameplayBlocked || HitStop.IsFrozen) return;

        Time.timeScale = NormalTimeScale;
    }

    /// <summary>Captura el paso de fisica original antes de que nadie lo toque.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CaptureDefaults()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        activeScale = NormalTimeScale;
        IsActive = false;
    }
}
