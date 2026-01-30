using System.Collections.Generic;
using UnityEngine;

public class DashHitbox2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int bossDamage = 1;
    [SerializeField] private int enemyDamage = 999;

    private bool active;

    // Guardamos IDs de targets golpeados DURANTE ESTE DASH
    private readonly HashSet<int> hitThisDash = new HashSet<int>();

    public void BeginDash(int dashSerial)
    {
        active = true;
        hitThisDash.Clear();   //  clave: se resetea cada dash
    }

    public void EndDash()
    {
        active = false;
        hitThisDash.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

        // ===== BOSS =====
        BossHealth boss = other.GetComponentInParent<BossHealth>();
        if (boss != null && !boss.IsDead)
        {
            int bossId = boss.gameObject.GetInstanceID();   //  por boss, no por collider
            if (hitThisDash.Contains(bossId)) return;        // 1 golpe por dash

            hitThisDash.Add(bossId);
            boss.TakeDamage(bossDamage);
            Debug.Log($"DASH HIT BOSS (hp now?) dmg={bossDamage}");
            return;
        }
    }
}
