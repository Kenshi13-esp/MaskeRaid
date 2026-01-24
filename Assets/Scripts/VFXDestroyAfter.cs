using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class VFXDestroyAfterAnim : MonoBehaviour
{
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        StartCoroutine(KillAfterAnim());
    }

    private IEnumerator KillAfterAnim()
    {
        // Espera 1 frame para que el Animator entre en el estado correcto
        yield return null;

        // Espera a que la animación termine
        while (true)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);

            // normalizedTime >= 1 significa "ya terminó" (si no está en loop)
            if (!anim.IsInTransition(0) && st.normalizedTime >= 1f)
                break;

            yield return null;
        }

        Destroy(gameObject);
    }
}

