using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox del dash del jugador. Solo hace dano mientras el dash esta activo y como maximo
/// una vez por boss y por dash.
/// </summary>
public class DashHitbox2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int baseDamage = 1;

    private readonly HashSet<int> bossesHitThisDash = new HashSet<int>();

    private bool isActive;
    private float currentDamageMultiplier = 1f;

    /// <summary>Abre la ventana de dano del dash con el multiplicador del perfil activo.</summary>
    public void BeginDash(float damageMultiplier = 1f)
    {
        isActive = true;
        currentDamageMultiplier = damageMultiplier;
        bossesHitThisDash.Clear();
    }

    /// <summary>Cierra la ventana de dano del dash.</summary>
    public void EndDash()
    {
        isActive = false;
        currentDamageMultiplier = 1f;
        bossesHitThisDash.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        BossHealth boss = other.GetComponentInParent<BossHealth>();
        if (boss == null || boss.IsDead) return;

        int bossId = boss.gameObject.GetInstanceID();
        if (!bossesHitThisDash.Add(bossId)) return;

        int finalDamage = Mathf.RoundToInt(baseDamage * currentDamageMultiplier);
        boss.TakeDamage(finalDamage);
    }
}
