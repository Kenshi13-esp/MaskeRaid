using UnityEngine;

public class VFXAutoDestroy : MonoBehaviour
{
    public void KillSelf()
    {
        Destroy(gameObject);
    }
}
