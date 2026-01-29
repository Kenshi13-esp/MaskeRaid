using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Chase")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 0.6f;

    [Header("Organic Motion")]
    [SerializeField] private float wobbleFrequency = 1.6f;
    [SerializeField] private float wobbleAmplitude = 0.9f;
    [SerializeField] private float steeringSharpness = 6f;

    [Header("Walls")]
    [SerializeField] private LayerMask wallsMask;
    [SerializeField] private float wallAvoidDistance = 0.6f;   // “nariz” del enemigo
    [SerializeField] private float wallAvoidTurn = 0.9f;       // cuánto gira para bordear

    private Rigidbody2D rb;
    private Vector2 currentDir;
    private float seed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        seed = Random.Range(0f, 9999f);

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    //Para tu WaveSpawner
    public void SetTarget(Transform target) => player = target;

    private void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (player.position - transform.position);
        float dist = toPlayer.magnitude;

        if (dist <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 baseDir = toPlayer.normalized;

        // Movimiento orgánico (serpenteo lateral)
        Vector2 sideDir = new Vector2(-baseDir.y, baseDir.x);
        float t = Time.time * wobbleFrequency + seed;
        float noise = (Mathf.PerlinNoise(t, seed) * 2f) - 1f;

        Vector2 desiredDir = (baseDir + sideDir * noise * wobbleAmplitude).normalized;

        //Evitación simple de paredes:
        // Si hay pared justo delante, desviamos un poco hacia un lado
        if (wallsMask.value != 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(rb.position, desiredDir, wallAvoidDistance, wallsMask);
            if (hit.collider != null)
            {
                // probamos girar a izquierda o derecha según cuál esté más libre
                Vector2 left = Rotate90(desiredDir);
                Vector2 right = -left;

                bool leftBlocked = Physics2D.Raycast(rb.position, left, wallAvoidDistance, wallsMask).collider != null;
                bool rightBlocked = Physics2D.Raycast(rb.position, right, wallAvoidDistance, wallsMask).collider != null;

                Vector2 avoidDir;
                if (!leftBlocked && rightBlocked) avoidDir = left;
                else if (leftBlocked && !rightBlocked) avoidDir = right;
                else avoidDir = left; // si ambos iguales, elige uno

                desiredDir = Vector2.Lerp(desiredDir, avoidDir, wallAvoidTurn).normalized;
            }
        }

        // Suavizamos giro
        currentDir = Vector2.Lerp(currentDir, desiredDir, steeringSharpness * Time.fixedDeltaTime);

        // Velocidad (esto respeta colisiones si el collider NO es trigger)
        rb.linearVelocity = currentDir * moveSpeed;
    }

    private static Vector2 Rotate90(Vector2 v) => new Vector2(-v.y, v.x);
}
