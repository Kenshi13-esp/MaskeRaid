using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;
    
    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;
    
    public static CameraShake Instance => instance;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        
        instance = this;
        cameraTransform = transform;
        originalPosition = cameraTransform.localPosition;
    }
    
    public static void Shake(float duration, float magnitude)
    {
        if (instance != null)
        {
            instance.TriggerShake(duration, magnitude);
        }
    }
    
    private void TriggerShake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }
    
    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float percentComplete = elapsed / duration;
            float dampening = 1f - Mathf.Clamp01(percentComplete);
            
            float offsetX = Random.Range(-1f, 1f) * magnitude * dampening;
            float offsetY = Random.Range(-1f, 1f) * magnitude * dampening;
            
            cameraTransform.localPosition = new Vector3(
                originalPosition.x + offsetX,
                originalPosition.y + offsetY,
                originalPosition.z
            );
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
