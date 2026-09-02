using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Pinta la tabla del ranking en el pop up del menu principal. Se refresca al abrirse el panel y
/// cada vez que <see cref="RankingStore.Changed"/> avisa de una marca nueva.
///
/// Se escribe la tabla entera en un unico texto en lugar de crear una fila por marca: las
/// columnas se alinean con la etiqueta de espaciado fijo de TextMeshPro, asi que no hace falta
/// ni layout ni un objeto por linea, y ordenar o vaciar la tabla es solo reconstruir la cadena.
/// </summary>
public class RankingBoard : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Texto donde se pinta la tabla completa")]
    [SerializeField] private TextMeshProUGUI boardLabel;

    [Header("Contenido")]
    [Tooltip("Numero maximo de marcas que se listan")]
    [Min(1)]
    [SerializeField] private int maxRows = 10;

    [Tooltip("Texto que se muestra cuando todavia no hay ninguna marca")]
    [SerializeField] private string emptyMessage = "SIN RECORDS TODAVIA";

    [Tooltip("Anchura de caracter fija que alinea las columnas, en em")]
    [Range(0.4f, 1f)]
    [SerializeField] private float monospaceWidth = 0.6f;

    private void OnEnable()
    {
        RankingStore.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        RankingStore.Changed -= Refresh;
    }

    /// <summary>Vuelve a leer el ranking y redibuja la tabla.</summary>
    public void Refresh()
    {
        if (boardLabel == null) return;

        boardLabel.richText = true;
        boardLabel.text = BuildBoardText();
    }

    private string BuildBoardText()
    {
        IReadOnlyList<RankingEntry> entries = RankingStore.Entries;

        StringBuilder builder = new StringBuilder();
        builder.Append($"<mspace={monospaceWidth:0.##}em>");

        if (entries.Count == 0)
        {
            builder.Append(emptyMessage);
            return builder.ToString();
        }

        int rowCount = Mathf.Min(maxRows, entries.Count);

        for (int i = 0; i < rowCount; i++)
        {
            RankingEntry entry = entries[i];
            string name = (entry.playerName ?? string.Empty).PadRight(PlayerSession.MaxNameLength);

            builder.Append($"{i + 1,2}  {name}  {RunTimer.FormatTime(entry.timeSeconds)}");

            if (i < rowCount - 1) builder.Append('\n');
        }

        return builder.ToString();
    }
}
