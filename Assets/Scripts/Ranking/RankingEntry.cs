using System;

/// <summary>
/// Una marca del ranking: quien jugo, cuanto tardo en completar el juego entero y cuando lo
/// consiguio. Es una clase serializable plana porque <see cref="UnityEngine.JsonUtility"/> es
/// lo que la escribe en disco.
/// </summary>
[Serializable]
public class RankingEntry
{
    /// <summary>Nombre del jugador tal y como lo escribio en el menu.</summary>
    public string playerName;

    /// <summary>Tiempo total de la partida en segundos.</summary>
    public float timeSeconds;

    /// <summary>Fecha en que se consiguio la marca, en formato ISO 8601.</summary>
    public string dateIso;

    public RankingEntry()
    {
    }

    public RankingEntry(string playerName, float timeSeconds)
    {
        this.playerName = playerName;
        this.timeSeconds = timeSeconds;
        dateIso = DateTime.UtcNow.ToString("o");
    }
}
