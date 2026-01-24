using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DashHitbox2D : MonoBehaviour
{
    private Collider2D hitbox;

    // Cada enemigo solo puede recibir 1 golpe por dash
    private readonly HashSet<int> hitThisDash = new HashSet<int>();

    private int currentDashSerial = 0;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;
    }

    public void BeginDash(int dashSerial)
    {
        currentDashSerial = dashSerial;
        hitThisDash.Clear();
        SetActive(true);
    }

    public void EndDash()
    {
        SetActive(false);
    }

    public void SetActive(bool active)
    {
        if (hitbox != null)
            hitbox.enabled = active;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        // Si el enemy tiene varios colliders, intentamos coger el root
        GameObject enemyGO = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        int id = enemyGO.GetInstanceID();
        if (!hitThisDash.Add(id))
            return; // ya golpeado en este dash

        EnemyHealth hp = enemyGO.GetComponent<EnemyHealth>();
        if (hp != null)
            hp.TakeDamage(1);
    }
}
