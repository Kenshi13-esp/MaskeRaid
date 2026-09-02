using System;
using UnityEngine;

/// <summary>
/// Estado global de pausa. Centraliza el control de <see cref="Time.timeScale"/> para que
/// jugabilidad, UI y entrada compartan una unica fuente de verdad.
/// </summary>
public static class GamePause
{
    private const float FrozenTimeScale = 0f;
    private const float RunningTimeScale = 1f;

    /// <summary>Se dispara cada vez que cambia el estado de pausa.</summary>
    public static event Action<bool> PauseChanged;

    /// <summary>True mientras el jugador tiene el menu de pausa abierto.</summary>
    public static bool IsPaused { get; private set; }

    /// <summary>True cuando la partida ha terminado (game over o victoria).</summary>
    public static bool IsGameFinished { get; private set; }

    /// <summary>True cuando la jugabilidad no debe responder a la entrada del jugador.</summary>
    public static bool IsGameplayBlocked => IsPaused || IsGameFinished;

    /// <summary>Pausa o reanuda la partida. Se ignora si la partida ya ha terminado.</summary>
    public static void SetPaused(bool paused)
    {
        if (IsGameFinished || IsPaused == paused) return;

        IsPaused = paused;
        Time.timeScale = paused ? FrozenTimeScale : RunningTimeScale;
        PauseChanged?.Invoke(paused);
    }

    /// <summary>Congela la partida al terminar y bloquea la pausa manual.</summary>
    public static void SetGameFinished(bool finished)
    {
        IsGameFinished = finished;

        if (finished && IsPaused)
        {
            IsPaused = false;
            PauseChanged?.Invoke(false);
        }

        Time.timeScale = finished ? FrozenTimeScale : RunningTimeScale;
    }

    /// <summary>Restablece el estado global. Debe llamarse al cargar una escena de juego.</summary>
    public static void ResetState()
    {
        bool wasPaused = IsPaused;

        IsPaused = false;
        IsGameFinished = false;
        Time.timeScale = RunningTimeScale;

        if (wasPaused) PauseChanged?.Invoke(false);
    }
}
