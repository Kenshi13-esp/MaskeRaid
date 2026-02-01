using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [Header("Floating Settings")]
    [Tooltip("Altura máxima del movimiento de flotación")]
    [SerializeField] private float floatAmplitude = 0.5f;
    
    [Tooltip("Velocidad del movimiento de flotación")]
    [SerializeField] private float floatSpeed = 1f;
    
    [Tooltip("Desfase inicial de la animación (para desincronizar múltiples objetos)")]
    [SerializeField] private float offset = 0f;
    
    [Header("Scale Settings")]
    [Tooltip("Activar animación de escala tipo respiración")]
    [SerializeField] private bool enableScaling = true;
    
    [Tooltip("Multiplicador de escala (1.0 = sin cambio)")]
    [SerializeField] private float scaleAmplitude = 0.1f;
    
    [Tooltip("Velocidad de la animación de escala")]
    [SerializeField] private float scaleSpeed = 1.5f;
    
    private Vector3 startPosition;
    private Vector3 startScale;
    private float timeOffset;
    
    private void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;
        timeOffset = offset;
    }
    
    private void Update()
    {
        float time = Time.time * floatSpeed + timeOffset;
        
        float yOffset = Mathf.Sin(time) * floatAmplitude;
        transform.localPosition = startPosition + new Vector3(0f, yOffset, 0f);
        
        if (enableScaling)
        {
            float scaleTime = Time.time * scaleSpeed + timeOffset;
            float scaleFactor = 1f + Mathf.Sin(scaleTime) * scaleAmplitude;
            transform.localScale = startScale * scaleFactor;
        }
    }
}
