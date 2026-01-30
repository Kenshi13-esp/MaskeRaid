using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 5;
    private int hp;

    private void Awake()
    {
        hp = maxHP;
    }

    public void TakeDamage(int amount)
    {
        hp -= amount;
        hp = Mathf.Max(0, hp);

        Debug.Log($"HP: {hp}/{maxHP}");

        if (hp <= 0)
        {
            Debug.Log("PLAYER DEAD");
            // Aquí luego reinicias escena o UI.
        }
    }
}



