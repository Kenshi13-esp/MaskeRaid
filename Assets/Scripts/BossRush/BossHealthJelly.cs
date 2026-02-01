using UnityEngine;
using UnityEngine.Events;

public class BossHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 1;
    
    [Header("Dash Ability Reward")]
    [Tooltip("Habilidad de dash que otorga este boss al morir")]
    [SerializeField] private DashAbility dashAbilityReward;
    
    public UnityEvent<DashAbility> OnBossDeath;
    
    private int hp;
    private DamageFlashEffect damageFlash;

    public bool IsDead => hp <= 0;
    public int CurrentHP => hp;
    public int MaxHP => maxHP;
    public DashAbility DashAbilityReward => dashAbilityReward;

    private void Awake()
    {
        hp = maxHP;
        damageFlash = GetComponent<DamageFlashEffect>();
        
        if (damageFlash == null)
        {
            damageFlash = gameObject.AddComponent<DamageFlashEffect>();
        }
    }

    public void ResetHealth()
    {
        hp = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (hp <= 0) return;
        
        hp -= amount;
        hp = Mathf.Max(0, hp);

        Debug.Log("BOSS HP: " + hp + "/" + maxHP);
        
        SoundManager.PlaySound(SoundType.BOSS_HIT);
        
        if (damageFlash != null && hp > 0)
        {
            damageFlash.Flash();
        }

        if (hp <= 0)
        {
            Debug.Log("BOSS DEAD");
            
            if (dashAbilityReward != null)
            {
                OnBossDeath?.Invoke(dashAbilityReward);
            }
            
            gameObject.SetActive(false);
        }
    }

    public int GetCurrentHealth()
    {
        return hp;
    }

    public int GetMaxHealth()
    {
        return maxHP;
    }
}


