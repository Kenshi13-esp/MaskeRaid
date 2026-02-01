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

    public bool IsDead => hp <= 0;
    public int CurrentHP => hp;
    public int MaxHP => maxHP;
    public DashAbility DashAbilityReward => dashAbilityReward;

    private void Awake()
    {
        hp = maxHP;
    }

    public void ResetHealth()
    {
        hp = maxHP;
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        hp = Mathf.Max(0, hp);

        GameFeel.Boss.TakeDamage();

        if (hp <= 0)
        {
            GameFeel.Boss.Death();
            
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


