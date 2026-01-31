using UnityEngine;

public class AnimationLoopController : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 0.5f;
    
    private Animator animator;
    private bool hasPlayed;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (animator == null || hasPlayed) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.normalizedTime >= 1.0f)
        {
            hasPlayed = true;
            animator.enabled = false;
            Destroy(gameObject, destroyDelay);
        }
    }
}
