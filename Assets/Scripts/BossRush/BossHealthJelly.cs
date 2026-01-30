using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 1;
    private int hp;

    public bool IsDead => hp <= 0;
    public int CurrentHP => hp;
    public int MaxHP => maxHP;

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

        Debug.Log("BOSS HP: " + hp + "/" + maxHP);

        if (hp <= 0)
        {
            Debug.Log("BOSS DEAD");
            gameObject.SetActive(false);
        }
    }
}


