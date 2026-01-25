using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public event Action<EnemyHealth> OnDeath;

    [SerializeField] private int maxHP = 1;
    private int hp;
    private bool dead;

    private void Awake()
    {
        hp = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (dead) return;

        hp -= amount;
        if (hp <= 0)
            Die();
    }

    private void Die()
    {
        if (dead) return;
        dead = true;

        //HIT STOP al morir por dash
        if (GameFeel.I != null)
            GameFeel.I.HitStop(0.05f, 0f);

        //Notifica al spawner (WaveSpawner10Waves)
        OnDeath?.Invoke(this);

        Destroy(gameObject);
    }
}

