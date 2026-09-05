using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Base de datos local del ranking. Guarda todas las marcas en un JSON dentro de
/// <see cref="Application.persistentDataPath"/>.
/// </summary>
public static class RankingStore
{
    /// <summary>Numero maximo de marcas que se muestran en la pantalla de ranking.</summary>
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

    /// <summary>Marcas guardadas, ya ordenadas (Solo devuelve el Top 10 para la UI).</summary>
    public static IReadOnlyList<RankingEntry> Entries
    {
        get
        {
            EnsureLoaded();
            // Novedad: Calculamos cuántos elementos devolver (máximo 10)
            int count = Mathf.Min(entries.Count, MaxEntries);
            // Devolvemos solo ese fragmento de la lista
            return entries.GetRange(0, count);
        }
    }

    /// <summary>
    /// Registra una partida completada y devuelve su puesto empezando en 1, o -1 si no ha
    /// entrado en el Top 10.
    /// </summary>
    public static int AddRun(string playerName, float timeSeconds)
    {
        EnsureLoaded();

        RankingEntry entry = new RankingEntry(PlayerSession.Sanitize(playerName), Mathf.Max(0f, timeSeconds));

        entries.Add(entry);
        SortEntries();

        // Novedad: Se ha eliminado la línea que borraba los excedentes.
        // Ahora TODOS los registros se quedan en la lista 'entries' y se guardan.

        Save();
        Changed?.Invoke();

        int index = entries.IndexOf(entry);
        
        return index + 1;
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

    [Serializable]
    private class RankingData
    {
        public List<RankingEntry> entries = new List<RankingEntry>();
    }
}