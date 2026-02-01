using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    private static HitStop instance;
    
    private Coroutine freezeCoroutine;
    private float pendingTimeScale = 1f;
    
    public static HitStop Instance => instance;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        
        instance = this;
    }
    
    public static void Stop(float duration)
    {
        if (instance != null)
        {
            instance.TriggerStop(duration);
        }
    }
    
    private void TriggerStop(float duration)
    {
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }
        
        freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }
    
    private IEnumerator FreezeRoutine(float duration)
    {
        pendingTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        yield return new WaitForSecondsRealtime(duration);
        
        Time.timeScale = pendingTimeScale;
        freezeCoroutine = null;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            Time.timeScale = 1f;
            instance = null;
        }
    }
}
