using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 3;
    private int hp;

    private bool isDead = false;

    private void Awake()
    {
        hp = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        hp -= amount;
        Debug.Log($"PLAYER HP: {hp}/{maxHP}");

        if (hp <= 0)
        {
            isDead = true;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("PLAYER DEAD");
        // Aquí luego puedes:
        // - reiniciar escena
        // - mostrar UI de Game Over
        // - desactivar controles, etc.
    }
}

