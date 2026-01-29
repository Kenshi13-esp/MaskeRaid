using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class DashChargeRadial_NoTouch : MonoBehaviour
{
    [Header("Assign")]
    [SerializeField] private PlayerDashController2D dash; // arrastra el componente del Player
    [SerializeField] private Image radialImage;           // arrastra DashChargeCircle (Image)
    [SerializeField] private Transform followTarget;      // Player (opcional)

    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.45f, 0f);
    [SerializeField] private bool lockRotation = true;

    [Header("Behavior")]
    [SerializeField] private bool hideWhenNotCharging = true;
    [SerializeField] private GameObject visualRoot; // para ocultar solo el circulo (recomendado)

    // reflection fields (privados)
    FieldInfo isChargingField;
    FieldInfo chargeTimerField;
    FieldInfo maxChargeTimeField;
    FieldInfo isCooldownField;

    void Awake()
    {
        if (dash == null) Debug.LogError("DashChargeRadial: asigna PlayerDashController2D en el inspector.");
        if (radialImage == null) Debug.LogError("DashChargeRadial: asigna la Image radial en el inspector.");

        if (followTarget == null && dash != null)
            followTarget = dash.transform;

        if (visualRoot == null) visualRoot = gameObject;

        CacheFields();
    }

    void CacheFields()
    {
        var t = typeof(PlayerDashController2D);

        isChargingField = t.GetField("isCharging", BindingFlags.Instance | BindingFlags.NonPublic);
        chargeTimerField = t.GetField("chargeTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        maxChargeTimeField = t.GetField("maxChargeTime", BindingFlags.Instance | BindingFlags.NonPublic);
        isCooldownField = t.GetField("isCooldown", BindingFlags.Instance | BindingFlags.NonPublic); // opcional

        if (isChargingField == null || chargeTimerField == null || maxChargeTimeField == null)
            Debug.LogError("DashChargeRadial: no encuentra isCharging/chargeTimer/maxChargeTime en PlayerDashController2D. ¿Has cambiado nombres?");
    }

    void LateUpdate()
    {
        // seguir
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
            if (lockRotation) transform.rotation = Quaternion.identity;
        }

        if (dash == null || radialImage == null || isChargingField == null) return;

        bool isCharging = (bool)isChargingField.GetValue(dash);
        float chargeTimer = (float)chargeTimerField.GetValue(dash);
        float maxChargeTime = (float)maxChargeTimeField.GetValue(dash);

        bool isCooldown = false;
        if (isCooldownField != null)
            isCooldown = (bool)isCooldownField.GetValue(dash);

        float pct = (maxChargeTime <= 0f) ? 0f : Mathf.Clamp01(chargeTimer / maxChargeTime);

        // Mostrar solo mientras cargas (y opcional: ocultar en cooldown)
        if (hideWhenNotCharging && visualRoot != null)
            visualRoot.SetActive(isCharging && !isCooldown);

        radialImage.fillAmount = isCharging ? pct : 0f;
    }
}

