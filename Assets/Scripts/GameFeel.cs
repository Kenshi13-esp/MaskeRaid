using System.Collections;
using UnityEngine;

public class GameFeel : MonoBehaviour
{
    public static GameFeel I { get; private set; }

    [Header("Hit Stop")]
    [SerializeField] private float hitStopCooldownRealtime = 0.10f; // evita spam cuando mueren muchos

    private float defaultFixedDeltaTime;
    private float lastHitStopTimeRealtime = -999f;
    private Coroutine hitStopCo;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    /// <summary>
    /// Duración en TIEMPO REAL (no afectada por timeScale).
    /// Cooldown evita que se acumule si mueren varios enemigos a la vez.
    /// </summary>
    public void HitStop(float durationRealtime, float timeScaleDuringStop = 0f)
    {
        // Anti-spam: si ya hicimos uno hace nada, ignoramos
        float now = Time.unscaledTime;
        if (now - lastHitStopTimeRealtime < hitStopCooldownRealtime)
            return;

        lastHitStopTimeRealtime = now;

        if (hitStopCo != null)
            StopCoroutine(hitStopCo);

        hitStopCo = StartCoroutine(HitStopRoutine(durationRealtime, timeScaleDuringStop));
    }

    private IEnumerator HitStopRoutine(float durationRealtime, float scale)
    {
        float prevScale = Time.timeScale;

        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;

        yield return new WaitForSecondsRealtime(durationRealtime);

        Time.timeScale = prevScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;

        hitStopCo = null;
    }
}


