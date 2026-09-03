using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el nombre del boss activo encima de su barra de vida.
/// Se engancha al evento <see cref="BossHealth.ActiveBossChanged"/> para mostrar
/// u ocultar el texto automaticamente cuando aparece o muere un boss.
/// </summary>
public class BossNameHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;

    private void OnEnable()
    {
        BossHealth.ActiveBossChanged += UpdateName;
        UpdateName(BossHealth.ActiveBoss);
    }

    private void OnDisable()
    {
        BossHealth.ActiveBossChanged -= UpdateName;
        UpdateName(null);
    }

    /// <summary>Actualiza el texto con el nombre del boss activo o lo oculta si no hay boss.</summary>
    private void UpdateName(BossHealth boss)
    {
        if (nameText == null) return;

        if (boss != null && !string.IsNullOrEmpty(boss.BossDisplayName))
        {
            nameText.text = boss.BossDisplayName;
            nameText.gameObject.SetActive(true);
        }
        else
        {
            nameText.gameObject.SetActive(false);
        }
    }
}
