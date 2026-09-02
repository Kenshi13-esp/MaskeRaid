using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Base de datos local del ranking. Guarda las marcas en un JSON dentro de
/// <see cref="Application.persistentDataPath"/>, que es la carpeta de datos de usuario del
/// juego: sobrevive a cerrar el juego y no se toca al reinstalar los assets.
///
/// Se usa un fichero y no PlayerPrefs porque una lista de marcas es una coleccion, y meterla en
/// PlayerPrefs obligaria a serializarla a mano en una clave suelta.
///
/// La carga es diferida y se queda en cache: leer disco en cada consulta seria absurdo para un
/// dato que solo cambia al terminar una partida.
/// </summary>
public static class RankingStore
{
    /// <summary>Numero maximo de marcas que se conservan. El resto se descarta al ordenar.</summary>
    public const int MaxEntries = 10;

    /// <summary>
    /// True para poner las partidas mas rapidas arriba, que es el orden util en un ranking de
    /// velocidad. Ponerlo en false lo invierte y deja las mas lentas primero.
    /// </summary>
    public const bool FastestFirst = true;

    private const string FileName = "ranking.json";

    private static List<RankingEntry> entries;

    /// <summary>Se dispara cuando el ranking cambia, para que la UI abierta se refresque.</summary>
    public static event Action Changed;

    /// <summary>Ruta del fichero de ranking en la carpeta de datos del usuario.</summary>
    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>Marcas guardadas, ya ordenadas.</summary>
    public static IReadOnlyList<RankingEntry> Entries
    {
        get
        {
            EnsureLoaded();
            return entries;
        }
    }

    /// <summary>
    /// Registra una partida completada y devuelve su puesto empezando en 1, o -1 si no ha
    /// entrado en la tabla.
    /// </summary>
    public static int AddRun(string playerName, float timeSeconds)
    {
        EnsureLoaded();

        RankingEntry entry = new RankingEntry(PlayerSession.Sanitize(playerName), Mathf.Max(0f, timeSeconds));

        entries.Add(entry);
        SortEntries();

        if (entries.Count > MaxEntries) entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

        Save();
        Changed?.Invoke();

        int index = entries.IndexOf(entry);
        return index < 0 ? -1 : index + 1;
    }

    /// <summary>Borra todas las marcas guardadas.</summary>
    public static void Clear()
    {
        EnsureLoaded();

        entries.Clear();
        Save();
        Changed?.Invoke();
    }

    private static void EnsureLoaded()
    {
        if (entries != null) return;

        entries = new List<RankingEntry>();

        try
        {
            if (!File.Exists(FilePath)) return;

            RankingData data = JsonUtility.FromJson<RankingData>(File.ReadAllText(FilePath));
            if (data?.entries == null) return;

            foreach (RankingEntry entry in data.entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.playerName)) entries.Add(entry);
            }

            SortEntries();
        }
        catch (Exception exception)
        {
            // Un fichero corrupto no debe impedir jugar: se arranca con el ranking vacio.
            Debug.LogWarning($"[Ranking] No se pudo leer '{FilePath}': {exception.Message}");
            entries.Clear();
        }
    }

    private static void Save()
    {
        try
        {
            RankingData data = new RankingData { entries = entries };
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Ranking] No se pudo guardar '{FilePath}': {exception.Message}");
        }
    }

    private static void SortEntries()
    {
        entries.Sort(CompareEntries);
    }

    private static int CompareEntries(RankingEntry first, RankingEntry second)
    {
        int comparison = first.timeSeconds.CompareTo(second.timeSeconds);

        return FastestFirst ? comparison : -comparison;
    }

    /// <summary>
    /// Envoltorio de la lista. JsonUtility no serializa colecciones en la raiz del documento,
    /// asi que necesita una clase con la lista dentro.
    /// </summary>
    [Serializable]
    private class RankingData
    {
        public List<RankingEntry> entries = new List<RankingEntry>();
    }
}
