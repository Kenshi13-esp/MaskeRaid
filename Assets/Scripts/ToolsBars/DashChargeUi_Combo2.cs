using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class DashChargeUI_Combo2_NoTouch : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerDashController2D dash;
    [SerializeField] private Image radialFill;
    [SerializeField] private Image pip1;
    [SerializeField] private Image pip2;

    [Header("Follow (World Space UI)")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.45f, 0f);
    [SerializeField] private bool lockRotation = true;

    [Header("Behavior")]
    [SerializeField] private bool hideWhenCooldown = false;
    [SerializeField] private GameObject visualRoot;

    FieldInfo isChargingField, chargeTimerField, maxChargeTimeField;
    FieldInfo isCooldownField, dashesUsedField, comboDashesField;

    void Awake()
    {
        if (followTarget == null && dash != null) followTarget = dash.transform;
        if (visualRoot == null) visualRoot = gameObject;
        CacheFields();
    }

    void CacheFields()
    {
        var t = typeof(PlayerDashController2D);

        isChargingField = t.GetField("isCharging", BindingFlags.Instance | BindingFlags.NonPublic);
        chargeTimerField = t.GetField("chargeTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        maxChargeTimeField = t.GetField("maxChargeTime", BindingFlags.Instance | BindingFlags.NonPublic);

        isCooldownField = t.GetField("isCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
        dashesUsedField = t.GetField("dashesUsed", BindingFlags.Instance | BindingFlags.NonPublic);
        comboDashesField = t.GetField("comboDashes", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    void LateUpdate()
    {
        // 1) Follow player
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
            if (lockRotation) transform.rotation = Quaternion.identity;
        }

        if (dash == null || radialFill == null || pip1 == null || pip2 == null) return;

        // 2) Leer estado del dash (sin tocar scripts)
        bool isCharging = (bool)isChargingField.GetValue(dash);
        float timer = (float)chargeTimerField.GetValue(dash);
        float maxTime = (float)maxChargeTimeField.GetValue(dash);

        bool isCooldown = (bool)isCooldownField.GetValue(dash);
        int used = (int)dashesUsedField.GetValue(dash);
        int maxDashes = (int)comboDashesField.GetValue(dash);

        int remaining = Mathf.Clamp(maxDashes - used, 0, maxDashes);

        // 3) Pips (2 / 1 / 0)
        pip1.enabled = remaining >= 1;
        pip2.enabled = remaining >= 2;

        // 4) Círculo: solo cuando cargas
        float pct = (maxTime <= 0f) ? 0f : Mathf.Clamp01(timer / maxTime);
        radialFill.fillAmount = isCharging ? pct : 0f;

        // 5) Opcional ocultar en cooldown
        if (hideWhenCooldown && visualRoot != null)
            visualRoot.SetActive(!isCooldown);
    }
}
