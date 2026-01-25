using UnityEngine;

public class DashVFXTravel2D : MonoBehaviour
{
    private Vector3 startPos;
    private Vector2 direction;
    private float distance;
    private float speed;
    private float traveled;

    public void Init(Vector3 startPos, Vector2 direction, float distance, float speed)
    {
        this.startPos = startPos;
        this.direction = direction.normalized;
        this.distance = distance;
        this.speed = speed;

        traveled = 0f;
        transform.position = startPos;
    }

    private void Update()
    {
        float step = speed * Time.unscaledDeltaTime; // tiempo real
        transform.position += (Vector3)(direction * step);
        traveled += step;

        if (traveled >= distance)
            Destroy(gameObject);
    }
}




