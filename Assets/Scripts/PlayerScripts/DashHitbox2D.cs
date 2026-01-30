using System.Collections.Generic;
using UnityEngine;

public class DashHitbox2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int bossDamage = 1;
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

        BossHealth boss = other.GetComponentInParent<BossHealth>();
        if (boss != null && !boss.IsDead)
        {
            int bossId = boss.gameObject.GetInstanceID();
            if (hitThisDash.Contains(bossId)) return;

            hitThisDash.Add(bossId);
            boss.TakeDamage(bossDamage);
            Debug.Log($"DASH HIT BOSS (hp now?) dmg={bossDamage}");
            return;
        }
    }
}
