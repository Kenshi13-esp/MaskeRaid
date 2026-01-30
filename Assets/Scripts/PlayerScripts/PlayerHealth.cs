using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private float knockbackDuration = 0.5f;
    [SerializeField] private float sideForce = 18f;
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float postLandingInvincibility = 0.5f;
    [SerializeField] private float postDashInvincibilityFrames = 1f;

    private int hp;
    private Rigidbody2D rb;
    private PlayerDashController2D dashController;
    private SpriteRenderer spriteRenderer;
    private Transform spriteTransform;

    private bool isLaunched = false;
    private bool isInvincible = false;

    public bool IsLaunched => isLaunched;

    private void Awake()
    {
        hp = maxHP;
        rb = GetComponent<Rigidbody2D>();
        dashController = GetComponent<PlayerDashController2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteTransform = spriteRenderer.transform;
    }

    public void TakeDamage(int amount, Vector2 knockbackDir, float forceMultiplier)
    {
        if (isLaunched || isInvincible) return;

        hp -= amount;
        hp = Mathf.Max(0, hp);

        StopAllCoroutines();
        StartCoroutine(ParabolicLaunch(knockbackDir));

        if (hp <= 0) Debug.Log("PLAYER DEAD");
    }

    private IEnumerator ParabolicLaunch(Vector2 dir)
    {
        isLaunched = true;
        if (dashController != null) dashController.EndDashState();

        Vector3 startLocalPos = spriteTransform.localPosition;

        rb.linearVelocity = Vector2.zero;
        float horizontalDir = Mathf.Sign(dir.x);
        rb.AddForce(new Vector2(horizontalDir * sideForce, 0f), ForceMode2D.Impulse);

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / knockbackDuration;

            float heightOffset = 4f * t * (1f - t) * jumpHeight;
            if (spriteTransform != null)
            {
                spriteTransform.localPosition = new Vector3(
                    startLocalPos.x,
                    startLocalPos.y + heightOffset,
                    startLocalPos.z
                );
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.96f, rb.linearVelocity.y);

            yield return null;
        }

        if (spriteTransform != null) spriteTransform.localPosition = startLocalPos;
        rb.linearVelocity = Vector2.zero;
        isLaunched = false;

        StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float timer = 0f;
        const float blinkSpeed = 0.1f;
        while (timer < postLandingInvincibility)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(blinkSpeed);
            timer += blinkSpeed;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        isInvincible = false;
    }

    public void GrantPostDashInvincibility()
    {
        if (isLaunched) return;
        StartCoroutine(PostDashInvincibilityRoutine());
    }

    private IEnumerator PostDashInvincibilityRoutine()
    {
        isInvincible = true;
        
        for (int i = 0; i < postDashInvincibilityFrames; i++)
        {
            yield return null;
        }
        
        isInvincible = false;
    }
}



