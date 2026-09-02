using UnityEngine;

/// <summary>
/// Contexto compartido que necesita cualquier <see cref="DashMoveBase"/> para ejecutarse:
/// cuerpo fisico, animator, sprite, mascara de paredes y como orientarse. Las subclases
/// aportan lo especifico de cada bando (invulnerabilidad, hitbox de dano, sonido), de modo
/// que un mismo dash sirve tanto para un boss como para el jugador.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class DashActor : MonoBehaviour
{
    private const float FacingThreshold = 0.01f;

    [Header("Dash Actor - Referencias")]
    [Tooltip("Vacio = se busca en este GameObject y sus hijos")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Vacio = se busca en este GameObject y sus hijos")]
    [SerializeField] private Animator animator;

    [Tooltip("Punto donde nace el VFX del dash. Vacio = el propio transform")]
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("Dash Actor - Ajustes")]
    [Tooltip("Capas consideradas pared para detener o rebotar el dash")]
    [SerializeField] private LayerMask wallMask;

    [Tooltip("Como se orienta este actor hacia la direccion del dash")]
    [SerializeField] private DashFacingMode facingMode = DashFacingMode.SpriteFlipX;

    private Rigidbody2D body;
    private Collider2D physicalCollider;
    private bool physicalColliderResolved;

    /// <summary>Bando del actor. Determina a quien dana su dash.</summary>
    public abstract DashFaction Faction { get; }

    public Rigidbody2D Body
    {
        get
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            return body;
        }
    }

    public Animator Animator
    {
        get
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            return animator;
        }
    }

    public SpriteRenderer SpriteRenderer
    {
        get
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            return spriteRenderer;
        }
    }

    /// <summary>Primer collider no trigger del actor. Se usa para ignorar colisiones puntuales.</summary>
    public Collider2D PhysicalCollider
    {
        get
        {
            if (!physicalColliderResolved)
            {
                physicalColliderResolved = true;
                physicalCollider = ResolvePhysicalCollider();
            }
            return physicalCollider;
        }
    }

    public Transform VfxSpawnPoint => vfxSpawnPoint != null ? vfxSpawnPoint : transform;

    public LayerMask WallMask => wallMask;

    /// <summary>
    /// Orienta el actor hacia la direccion indicada segun su modo de giro. Solo escribe si el
    /// valor cambia, porque los dashes con rebote la llaman en cada paso de fisica.
    /// </summary>
    public void FaceDirection(Vector2 direction)
    {
        if (facingMode == DashFacingMode.None) return;
        if (Mathf.Abs(direction.x) <= FacingThreshold) return;

        bool facingLeft = direction.x < 0f;

        switch (facingMode)
        {
            case DashFacingMode.SpriteFlipX:
                SpriteRenderer renderer = SpriteRenderer;
                if (renderer != null && renderer.flipX != facingLeft) renderer.flipX = facingLeft;
                break;

            case DashFacingMode.RootScaleX:
                ApplyScaleFacing(transform, facingLeft);
                break;

            case DashFacingMode.SpriteScaleX:
                if (SpriteRenderer != null) ApplyScaleFacing(SpriteRenderer.transform, facingLeft);
                break;
        }
    }

    /// <summary>True si el GameObject indicado pertenece a las capas de pared del actor.</summary>
    public bool IsWall(GameObject other)
    {
        return other != null && ((1 << other.layer) & wallMask.value) != 0;
    }

    /// <summary>Dispara un trigger del Animator si el nombre no esta vacio.</summary>
    public void SetAnimatorTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName) || Animator == null) return;
        Animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Activa o desactiva la ventana de dano del dash. El perfil llega para que el dano y el
    /// feedback de impacto salgan de los mismos datos; al desactivar puede ser nulo.
    /// </summary>
    public virtual void SetDashDamageActive(bool active, DashProfile profile) { }

    /// <summary>Activa o desactiva la invulnerabilidad mientras dura el dash.</summary>
    public virtual void SetInvulnerable(bool invulnerable) { }

    /// <summary>Reproduce el sonido de dash del actor.</summary>
    public virtual void PlayDashSound(SoundType soundType) { }

    /// <summary>Detiene el sonido de dash del actor.</summary>
    public virtual void StopDashSound() { }

    private Collider2D ResolvePhysicalCollider()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();

        foreach (Collider2D candidate in colliders)
        {
            if (!candidate.isTrigger) return candidate;
        }

        return colliders.Length > 0 ? colliders[0] : null;
    }

    private static void ApplyScaleFacing(Transform target, bool facingLeft)
    {
        Vector3 scale = target.localScale;
        float signedX = facingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);

        if (Mathf.Approximately(scale.x, signedX)) return;

        scale.x = signedX;
        target.localScale = scale;
    }
}
