using System.Collections;
using UnityEngine;

/// <summary>
/// Congelacion muy breve del juego en el instante del impacto ("hit stop"). Es lo que le da
/// peso a un golpe: una pausa de pocos fotogramas en el momento del contacto se lee mucho
/// mejor que ralentizar el mundo mientras el jugador apunta.
///
/// Escribe en <see cref="Time.timeScale"/> igual que <see cref="GamePause"/> y
/// <see cref="SlowMotion"/>, y el reparto de mando es estricto: ignora las peticiones con la
/// jugabilidad bloqueada, abandona sin restaurar nada si el jugador pausa a mitad de
/// congelacion, y al terminar devuelve el tiempo a la escala que pida la camara lenta en vez
/// de asumir que es la normal.
/// </summary>
public static class HitStop
{
    /// <summary>Techo de seguridad: una congelacion mas larga se percibe como un tiron.</summary>
    private const float MaxFreezeDuration = 0.25f;

    private const string RunnerName = "[HitStop]";

    private static HitStopRunner runner;

    /// <summary>True mientras hay una congelacion de impacto en curso.</summary>
    public static bool IsFrozen => runner != null && runner.IsFrozen;

    /// <summary>
    /// Congela el juego durante los segundos reales indicados. Un <paramref name="frozenTimeScale"/>
    /// de 0 detiene el mundo por completo; un valor pequeno lo deja avanzar a rastras.
    /// </summary>
    public static void Freeze(float duration, float frozenTimeScale = 0f)
    {
        if (duration <= 0f) return;
        if (GamePause.IsGameplayBlocked) return;

        HitStopRunner host = ResolveRunner();
        if (host == null) return;

        host.Freeze(Mathf.Min(duration, MaxFreezeDuration), Mathf.Clamp01(frozenTimeScale));
    }

    /// <summary>Corta la congelacion en curso y devuelve el tiempo a la escala que toque.</summary>
    public static void Cancel()
    {
        if (runner == null) return;

        runner.CancelFreeze();
    }

    /// <summary>Limpia la referencia al anfitrion, que no sobrevive entre sesiones de juego.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetState()
    {
        runner = null;
    }

    private static HitStopRunner ResolveRunner()
    {
        if (runner != null) return runner;

        GameObject host = new GameObject(RunnerName) { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(host);

        runner = host.AddComponent<HitStopRunner>();
        return runner;
    }
}

/// <summary>
/// Anfitrion de la corrutina de <see cref="HitStop"/>. Vive en un objeto oculto y persistente
/// porque la congelacion tiene que seguir contando con el tiempo del juego detenido, y eso
/// solo lo puede hacer un MonoBehaviour que siga recibiendo Update.
/// </summary>
internal class HitStopRunner : MonoBehaviour
{
    private Coroutine freezeRoutine;

    /// <summary>True mientras hay una congelacion en curso.</summary>
    public bool IsFrozen => freezeRoutine != null;

    /// <summary>Arranca una congelacion, reemplazando la que hubiera en curso.</summary>
    public void Freeze(float duration, float frozenTimeScale)
    {
        if (freezeRoutine != null) StopCoroutine(freezeRoutine);

        freezeRoutine = StartCoroutine(FreezeRoutine(duration, frozenTimeScale));
    }

    /// <summary>Interrumpe la congelacion y restaura el tiempo.</summary>
    public void CancelFreeze()
    {
        if (freezeRoutine == null) return;

        StopCoroutine(freezeRoutine);
        freezeRoutine = null;

        Restore();
    }

    private IEnumerator FreezeRoutine(float duration, float frozenTimeScale)
    {
        Time.timeScale = frozenTimeScale;

        float remaining = duration;

        while (remaining > 0f)
        {
            if (GamePause.IsGameplayBlocked)
            {
                freezeRoutine = null;
                yield break;
            }

            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        freezeRoutine = null;
        Restore();
    }

    private void Restore()
    {
        if (GamePause.IsGameplayBlocked) return;

        Time.timeScale = SlowMotion.CurrentScale;
    }

    private void OnDisable()
    {
        CancelFreeze();
    }
}
