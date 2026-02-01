using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;
    private Transform cameraTransform;
    private Vector3 originalPosition;
    private Coroutine currentShake;

    public static CameraShake Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CameraShake>();
            }
            return instance;
        }
    }

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
        if (Instance != null)
        {
            Instance.StartShake(duration, magnitude);
        }
    }

    private void StartShake(float duration, float magnitude)
    {
        if (currentShake != null)
        {
            StopCoroutine(currentShake);
        }
        currentShake = StartCoroutine(ShakeRoutine(duration, magnitude));
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
            cameraTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        cameraTransform.localPosition = originalPosition;
        currentShake = null;
    }
}
