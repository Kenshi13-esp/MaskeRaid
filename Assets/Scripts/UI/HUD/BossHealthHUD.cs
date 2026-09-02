using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida del boss activo. Se engancha al evento <see cref="BossHealth.ActiveBossChanged"/>
/// en lugar de comprobar el boss activo en cada fotograma, y solo escribe en la imagen cuando
/// el relleno cambia para no forzar reconstrucciones del canvas.
/// </summary>
public class BossHealthHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private Image fillImage;

    [Header("Opciones")]
    [SerializeField] private bool hideWhenNoBoss = false;

    [Tooltip("Coloca la barra en la parte inferior de la pantalla al arrancar")]
    [SerializeField] private bool anchorToBottom = true;

    [SerializeField] private Vector2 bottomOffset = new Vector2(0f, 20f);

    private BossHealth trackedBoss;
    private float lastFillAmount = -1f;

    private void Awake()
    {
        if (hudRoot == null) hudRoot = gameObject;
        if (anchorToBottom) ApplyBottomAnchor();
    }

    private void OnEnable()
    {
        BossHealth.ActiveBossChanged += TrackBoss;
        TrackBoss(BossHealth.ActiveBoss);
    }

    private void OnDisable()
    {
        BossHealth.ActiveBossChanged -= TrackBoss;
        TrackBoss(null);
    }

    private void TrackBoss(BossHealth boss)
    {
        if (trackedBoss == boss) return;

        if (trackedBoss != null) trackedBoss.HealthChanged -= OnBossHealthChanged;

        trackedBoss = boss;

        if (trackedBoss != null)
        {
            trackedBoss.HealthChanged += OnBossHealthChanged;
            OnBossHealthChanged(trackedBoss.CurrentHP, trackedBoss.MaxHP);
        }
        else
        {
            OnBossHealthChanged(0, 0);
        }

        if (hideWhenNoBoss && hudRoot != null && hudRoot.activeSelf != (trackedBoss != null))
        {
            hudRoot.SetActive(trackedBoss != null);
        }
    }

    private void OnBossHealthChanged(int currentHP, int maxHP)
    {
        if (fillImage == null) return;

        float fill = maxHP <= 0 ? 0f : Mathf.Clamp01((float)currentHP / maxHP);
        if (Mathf.Approximately(fill, lastFillAmount)) return;

        lastFillAmount = fill;
        fillImage.fillAmount = fill;
    }

    private void ApplyBottomAnchor()
    {
        RectTransform rectTransform = hudRoot != null ? hudRoot.GetComponent<RectTransform>() : null;
        if (rectTransform == null) return;

        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = bottomOffset;
    }
}
