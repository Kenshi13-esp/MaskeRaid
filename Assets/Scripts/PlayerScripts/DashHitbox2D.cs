using System.Collections.Generic;
using UnityEngine;

public class DashHitbox2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int baseDamage = 1;
    private bool active;
    private float currentDamageMultiplier = 1f;

    private readonly HashSet<int> hitThisDash = new HashSet<int>();

    public void BeginDash(int dashSerial, float damageMultiplier = 1f)
    {
        active = true;
        currentDamageMultiplier = damageMultiplier;
        hitThisDash.Clear();
    }

    public void EndDash()
    {
        active = false;
        currentDamageMultiplier = 1f;
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
            int finalDamage = Mathf.RoundToInt(baseDamage * currentDamageMultiplier);
            boss.TakeDamage(finalDamage);
            Debug.Log($"DASH HIT BOSS! BaseDmg={baseDamage}, Multiplier={currentDamageMultiplier:F1}x, FinalDmg={finalDamage}");
            return;
        }
    }
}
