using System.Text;
using UnityEngine;

/// <summary>
/// Nombre del jugador de la partida en curso. Vive en PlayerPrefs y no en una variable estatica
/// porque tiene que cruzar el cambio de escena del menu al juego y sobrevivir a un reinicio de
/// la partida desde el menu de pausa, que recarga la escena entera.
/// </summary>
public static class PlayerSession
{
    /// <summary>Longitud maxima del nombre, en plan marcador de arcade.</summary>
    public const int MaxNameLength = 8;

    private const string NameKey = "ranking_player_name";
    private const string DefaultName = "AAA";

    /// <summary>Nombre con el que se registrara la partida en el ranking.</summary>
    public static string PlayerName
    {
        get
        {
            string stored = PlayerPrefs.GetString(NameKey, DefaultName);
            return string.IsNullOrEmpty(stored) ? DefaultName : stored;
        }
        set
        {
            string sanitized = Sanitize(value);

            PlayerPrefs.SetString(NameKey, string.IsNullOrEmpty(sanitized) ? DefaultName : sanitized);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Deja el nombre en formato de marcador: mayusculas, solo letras y numeros, y recortado a
    /// la longitud maxima. Filtrar aqui evita que un nombre con saltos de linea o etiquetas de
    /// texto enriquecido rompa el dibujado de la tabla.
    /// </summary>
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        StringBuilder builder = new StringBuilder(MaxNameLength);

        foreach (char character in raw)
        {
            if (builder.Length >= MaxNameLength) break;
            if (!char.IsLetterOrDigit(character)) continue;

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Borra el nombre guardado de la memoria en caso de derrota.
    /// </summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(NameKey);
        PlayerPrefs.Save();
    }
}
