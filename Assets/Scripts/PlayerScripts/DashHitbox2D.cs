using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox del dash del jugador. Solo hace dano mientras el dash esta activo y como maximo
/// una vez por boss y por dash.
///
/// Es tambien el punto donde se concentra el peso del golpe: al conectar dispara la
/// congelacion breve (<see cref="HitStop"/>) y el temblor de camara, en el instante exacto
/// del contacto y no al terminar el recorrido.
///
/// El ataque especial (R2) es un caso aparte a proposito: su dano y su golpeo son fijos y no
/// dependen del perfil de la mascara equipada, para que se sienta igual de contundente con
/// cualquier poder robado. El dash normal si escala con el multiplicador de su perfil.
/// </summary>
public class DashHitbox2D : MonoBehaviour
{
    [Header("Dash normal")]
    [SerializeField] private int baseDamage = 1;

    [Header("Especial (R2)")]
    [Tooltip("Dano fijo del especial, sea cual sea la mascara equipada")]
    [SerializeField] private int specialDamage = 300;

    [Tooltip("Congelacion del golpe del especial, en segundos reales")]
    [SerializeField] private float specialHitStopDuration = 0.14f;

    [Tooltip("Escala de tiempo durante la congelacion del especial. 0 = el mundo se para del todo")]
    [Range(0f, 1f)]
    [SerializeField] private float specialHitStopTimeScale = 0f;

    [Tooltip("Duracion del temblor de camara del especial")]
    [SerializeField] private float specialCameraShakeDuration = 0.24f;

    [Tooltip("Intensidad del temblor de camara del especial")]
    [SerializeField] private float specialCameraShakeMagnitude = 0.34f;

    private readonly HashSet<int> bossesHitThisDash = new HashSet<int>();

    private bool isActive;
    private bool isSpecialDash;
    private DashProfile activeProfile;

    /// <summary>
    /// Marca el ataque que va a lanzarse como especial o como dash normal. Lo llama el
    /// controlador del jugador antes de ejecutar el movimiento, porque es el unico que sabe
    /// de que boton viene el ataque.
    /// </summary>
    public void SetSpecialDash(bool isSpecial)
    {
        isSpecialDash = isSpecial;
    }

    /// <summary>Abre la ventana de dano del dash con los ajustes del perfil activo.</summary>
    public void BeginDash(DashProfile profile)
    {
        isActive = true;
        activeProfile = profile;
        bossesHitThisDash.Clear();
    }

    /// <summary>Cierra la ventana de dano del dash.</summary>
    public void EndDash()
    {
        isActive = false;
        isSpecialDash = false;
        activeProfile = null;
        bossesHitThisDash.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        BossHealth boss = other.GetComponentInParent<BossHealth>();
        if (boss == null || boss.IsDead) return;

        int bossId = boss.gameObject.GetInstanceID();
        if (!bossesHitThisDash.Add(bossId)) return;

        boss.TakeDamage(ResolveDamage());
        PlayImpactFeedback();
    }

    /// <summary>Dano del golpe: fijo en el especial y escalado por el perfil en el dash normal.</summary>
    private int ResolveDamage()
    {
        if (isSpecialDash) return specialDamage;

        float damageMultiplier = activeProfile != null ? activeProfile.DamageMultiplier : 1f;
        return Mathf.RoundToInt(baseDamage * damageMultiplier);
    }

    /// <summary>Congelacion y temblor de camara en el fotograma del contacto.</summary>
    private void PlayImpactFeedback()
    {
        if (isSpecialDash)
        {
            HitStop.Freeze(specialHitStopDuration, specialHitStopTimeScale);

            if (specialCameraShakeDuration > 0f)
            {
                CameraShake.Shake(specialCameraShakeDuration, specialCameraShakeMagnitude);
            }

            return;
        }

        if (activeProfile == null) return;

        HitStop.Freeze(activeProfile.HitStopDuration, activeProfile.HitStopTimeScale);

        if (activeProfile.CameraShakeDuration > 0f)
        {
            CameraShake.Shake(activeProfile.CameraShakeDuration, activeProfile.CameraShakeMagnitude);
        }
    }
}
