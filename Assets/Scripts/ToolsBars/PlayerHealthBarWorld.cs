using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class PlayerHealthBarWorld_NoTouch : MonoBehaviour
{
    [Header("Refs (Inspector)")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image fillImage;

    [Header("Follow")]
    [SerializeField] private Transform followTarget; // si lo dejas vacío, usa el PlayerHealth
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, -0.35f, 0f);
    [SerializeField] private bool lockRotation = true;

    [Header("Hide")]
    [SerializeField] private bool hideWhenFull = false;
    [Tooltip("Arrastra aquí el objeto visual (por ejemplo Hp_Bg) para ocultarlo, NO este GameObject.")]
    [SerializeField] private GameObject visualRoot;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private FieldInfo hpField;
    private FieldInfo maxHpField;

    private void Awake()
    {
        if (playerHealth == null)
            Debug.LogError("HealthBar: falta asignar PlayerHealth en el inspector.");

        if (fillImage == null)
            Debug.LogError("HealthBar: falta asignar Fill Image en el inspector.");

        if (followTarget == null && playerHealth != null)
            followTarget = playerHealth.transform;

        // Campos privados: hp y maxHP
        var t = typeof(PlayerHealth);
        hpField = t.GetField("hp", BindingFlags.Instance | BindingFlags.NonPublic);
        maxHpField = t.GetField("maxHP", BindingFlags.Instance | BindingFlags.NonPublic);

        if (hpField == null || maxHpField == null)
            Debug.LogError("HealthBar: no encuentro los campos privados 'hp' o 'maxHP' en PlayerHealth.");

        if (visualRoot == null)
            visualRoot = gameObject; // si no asignas nada, al menos no peta (pero mejor asignar Hp_Bg)
    }

    private void LateUpdate()
    {
        // Seguir al jugador (si estás usando World Space)
        if (followTarget != null)
        {
            transform.position = followTarget.position + worldOffset;
            if (lockRotation) transform.rotation = Quaternion.identity;
        }

        if (playerHealth == null || fillImage == null || hpField == null || maxHpField == null)
            return;

        int hp = (int)hpField.GetValue(playerHealth);
        int maxHP = (int)maxHpField.GetValue(playerHealth);

        float pct = (maxHP <= 0) ? 0f : Mathf.Clamp01((float)hp / maxHP);
        fillImage.fillAmount = pct;

        if (debugLogs) Debug.Log($"[HealthBar] hp={hp} max={maxHP} pct={pct}");

        if (hideWhenFull && visualRoot != null)
            visualRoot.SetActive(pct < 0.999f);
    }
}
