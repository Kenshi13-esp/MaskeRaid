using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vida de un boss y recompensa que otorga al morir. Publica el boss activo de forma
/// estatica para que el HUD no tenga que buscarlo cada frame.
/// </summary>
public class BossHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 1;

    [Header("Recompensa")]
    [Tooltip("Mascara que el jugador obtiene al derrotar a este boss")]
    [SerializeField] private MaskDefinition maskReward;

    /// <summary>Se dispara al morir el boss con la mascara que otorga.</summary>
    public UnityEvent<MaskDefinition> OnBossDeath;

    private int hp;
    private DamageFlashEffect damageFlash;

    /// <summary>Boss activo en la escena, o null si no hay ninguno.</summary>
    public static BossHealth ActiveBoss { get; private set; }

    /// <summary>
    /// Se dispara cuando aparece o desaparece el boss activo. Permite al HUD engancharse por
    /// evento en lugar de comprobar <see cref="ActiveBoss"/> en cada fotograma.
    /// </summary>
    public static event Action<BossHealth> ActiveBossChanged;

    /// <summary>Se dispara cuando cambia la vida del boss (vida actual, vida maxima).</summary>
    public event Action<int, int> HealthChanged;

    public bool IsDead => hp <= 0;
    public int CurrentHP => hp;
    public int MaxHP => maxHP;
    public MaskDefinition MaskReward => maskReward;

    private void Awake()
    {
        hp = maxHP;
        damageFlash = GetComponent<DamageFlashEffect>();

        if (damageFlash == null) damageFlash = gameObject.AddComponent<DamageFlashEffect>();
    }

    private void OnEnable()
    {
        ActiveBoss = this;
        ActiveBossChanged?.Invoke(this);
        HealthChanged?.Invoke(hp, maxHP);
    }

    private void OnDisable()
    {
        if (ActiveBoss != this) return;

        ActiveBoss = null;
        ActiveBossChanged?.Invoke(null);
    }

    /// <summary>Devuelve la vida al maximo. Se usa al reaparecer el boss en el rush.</summary>
    public void ResetHealth()
    {
        hp = maxHP;
        HealthChanged?.Invoke(hp, maxHP);
    }

    /// <summary>Aplica dano al boss y lo desactiva si su vida llega a cero.</summary>
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        hp = Mathf.Max(0, hp - amount);

        SoundManager.PlaySound(SoundType.BOSS_HIT);
        HealthChanged?.Invoke(hp, maxHP);

        if (!IsDead)
        {
            if (damageFlash != null) damageFlash.Flash();
            return;
        }

        if (maskReward != null) OnBossDeath?.Invoke(maskReward);

        gameObject.SetActive(false);
    }
}
