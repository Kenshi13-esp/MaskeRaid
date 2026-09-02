using UnityEngine;

/// <summary>
/// Destruye el GameObject cuando su animacion llega al final. Se desactiva a si mismo en
/// cuanto programa la destruccion en lugar de seguir ejecutando un Update que ya no hace nada.
/// </summary>
public class AnimationLoopController : MonoBehaviour
{
    private const int BaseAnimatorLayer = 0;

    [SerializeField] private float destroyDelay = 0.5f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null) enabled = false;
    }

    private void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(BaseAnimatorLayer).normalizedTime < 1f) return;

        animator.enabled = false;
        enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}
