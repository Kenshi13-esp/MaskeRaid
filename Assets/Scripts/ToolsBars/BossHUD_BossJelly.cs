using UnityEngine;
using UnityEngine.UI;

public class BossHealthHUD_NoTouch : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private Image fillImage;

    [Header("Boss Find")]
    [Tooltip("Si tu boss root tiene Tag 'Boss', ponlo aquí. Si lo dejas vacío, buscará el primer BossHealth activo.")]
    [SerializeField] private string bossTag = "Boss";

    [Header("Options")]
    [SerializeField] private bool hideWhenNoBoss = false;

    private BossHealth boss;

    private void Awake()
    {
        if (hudRoot == null) hudRoot = gameObject;

        EnsureBottomPosition();
        TryFindBoss();
        RefreshVisibility();
        UpdateBar();
    }

    private void Update()
    {
        if (boss == null || !boss.gameObject.activeInHierarchy || boss.IsDead)
        {
            TryFindBoss();
            RefreshVisibility();
        }

        UpdateBar();
    }

    private void TryFindBoss()
    {
        if (!string.IsNullOrEmpty(bossTag))
        {
            var go = GameObject.FindGameObjectWithTag(bossTag);
            if (go != null)
            {
                var bh = go.GetComponent<BossHealth>();
                if (bh != null && go.activeInHierarchy && !bh.IsDead)
                {
                    boss = bh;
                    return;
                }
            }
        }

        BossHealth any = FindFirstObjectByType<BossHealth>();
        if (any != null && any.gameObject.activeInHierarchy && !any.IsDead)
        {
            boss = any;
            return;
        }

        boss = null;
    }

    private void UpdateBar()
    {
        if (fillImage == null) return;

        if (boss == null)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        int hp = boss.GetCurrentHealth();
        int max = boss.GetMaxHealth();
        float pct = (max <= 0) ? 0f : Mathf.Clamp01((float)hp / max);

        fillImage.fillAmount = pct;
    }

    private void RefreshVisibility()
    {
        if (!hideWhenNoBoss || hudRoot == null) return;
        hudRoot.SetActive(boss != null);
    }

    private void EnsureBottomPosition()
    {
        if (hudRoot == null) return;

        RectTransform rectTransform = hudRoot.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 20f);
    }
}

