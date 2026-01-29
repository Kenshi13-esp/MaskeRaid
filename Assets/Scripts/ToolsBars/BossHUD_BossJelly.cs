using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class BossHealthHUD_NoTouch : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject hudRoot; // BossHUD (panel)
    [SerializeField] private Image fillImage;    // HP_Fill

    [Header("Boss Find")]
    [Tooltip("Si tu boss root tiene Tag 'Boss', ponlo aquí. Si lo dejas vacío, buscará el primer BossHealth activo.")]
    [SerializeField] private string bossTag = "Boss";

    [Header("Options")]
    [SerializeField] private bool hideWhenNoBoss = true;

    private BossHealth boss;

    // Campos privados en BossHealth
    private FieldInfo hpField;
    private FieldInfo maxHpField;

    private void Awake()
    {
        if (hudRoot == null) hudRoot = gameObject;

        // BossHealth: private int hp; [SerializeField] private int maxHP;
        var t = typeof(BossHealth);
        hpField = t.GetField("hp", BindingFlags.Instance | BindingFlags.NonPublic);
        maxHpField = t.GetField("maxHP", BindingFlags.Instance | BindingFlags.NonPublic);

        if (hpField == null || maxHpField == null)
            Debug.LogError("BossHealthHUD: No encuentro los campos privados 'hp' o 'maxHP' en BossHealth.");

        TryFindBoss();
        RefreshVisibility();
        UpdateBar(); // pinta al inicio
    }

    private void Update()
    {
        // Si no hay boss o está desactivado o muerto: intenta encontrar otro (boss rush)
        if (boss == null || !boss.gameObject.activeInHierarchy || boss.IsDead)
        {
            TryFindBoss();
            RefreshVisibility();
        }

        UpdateBar();
    }

    private void TryFindBoss()
    {
        // 1) Por Tag (recomendado)
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

        // 2) Fallback: primer BossHealth activo en escena
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

        if (boss == null || hpField == null || maxHpField == null)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        int hp = (int)hpField.GetValue(boss);
        int max = (int)maxHpField.GetValue(boss);
        float pct = (max <= 0) ? 0f : Mathf.Clamp01((float)hp / max);

        fillImage.fillAmount = pct;
    }

    private void RefreshVisibility()
    {
        if (!hideWhenNoBoss || hudRoot == null) return;
        hudRoot.SetActive(boss != null);
    }
}
