using UnityEngine;

public class BossPhaseCollisions2D : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private Collider2D playerBlockerCollider; //el del hijo PlayerBlocker

    // Llamar cuando EMPIEZA la animación de ataque
    public void AttackStart()
    {
        if (playerBlockerCollider != null)
            playerBlockerCollider.enabled = false; //el player lo atraviesa
    }

    // Llamar cuando TERMINA la animación de ataque
    public void AttackEnd()
    {
        if (playerBlockerCollider != null)
            playerBlockerCollider.enabled = true; //vuelve a bloquear
    }
}
