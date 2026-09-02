using UnityEngine;

/// <summary>
/// Estela de siluetas que deja el jugador mientras el dash esta en curso. Vende la velocidad
/// del desplazamiento sin necesidad de assets nuevos: copia el sprite del fotograma actual en
/// siluetas que se quedan atras y se desvanecen.
///
/// Las siluetas salen de una reserva fija creada al arrancar, asi que un dash no instancia ni
/// destruye GameObjects en cada fotograma.
/// </summary>
public class DashAfterimage : MonoBehaviour
{
    private const string ContainerSuffix = " Afterimages";
    private const int MinGhostCount = 2;

    [Header("Referencias")]
    [Tooltip("Vacio = se busca en este GameObject")]
    [SerializeField] private PlayerDashController2D dash;

    [Tooltip("Sprite que se clona. Vacio = se busca en este GameObject y sus hijos")]
    [SerializeField] private SpriteRenderer sourceRenderer;

    [Header("Estela")]
    [Tooltip("Segundos entre siluetas")]
    [SerializeField] private float spawnInterval = 0.035f;

    [Tooltip("Segundos que tarda una silueta en desaparecer")]
    [SerializeField] private float fadeDuration = 0.18f;

    [Tooltip("Opacidad inicial de cada silueta")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0.5f;

    [Tooltip("Color de las siluetas")]
    [SerializeField] private Color tint = new Color(0.65f, 0.9f, 1f, 1f);

    [Tooltip("Maximo de siluetas simultaneas")]
    [SerializeField] private int maxGhosts = 10;

    [Tooltip("Cuantas capas por detras del sprite original se dibujan las siluetas")]
    [SerializeField] private int sortingOrderOffset = 1;

    private Transform container;
    private SpriteRenderer[] ghosts;
    private float[] remainingLife;
    private float spawnTimer;
    private int nextGhostIndex;

    private void Awake()
    {
        if (dash == null) dash = GetComponent<PlayerDashController2D>();
        if (sourceRenderer == null) sourceRenderer = GetComponentInChildren<SpriteRenderer>();

        if (dash == null) Debug.LogError("[DashAfterimage] Falta la referencia al PlayerDashController2D.", this);
        if (sourceRenderer == null) Debug.LogError("[DashAfterimage] No se ha encontrado ningun SpriteRenderer.", this);

        BuildGhostPool();
    }

    private void OnDestroy()
    {
        if (container != null) Destroy(container.gameObject);
    }

    private void Update()
    {
        FadeGhosts();

        if (dash == null || sourceRenderer == null) return;

        if (!dash.IsDashing || GamePause.IsGameplayBlocked)
        {
            spawnTimer = 0f;
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = Mathf.Max(spawnInterval, Time.deltaTime);
        SpawnGhost();
    }

    /// <summary>
    /// Crea las siluetas apagadas dentro de un contenedor en la raiz de la escena: no pueden
    /// ser hijas del jugador porque deben quedarse atras mientras el dash avanza.
    /// </summary>
    private void BuildGhostPool()
    {
        int count = Mathf.Max(MinGhostCount, maxGhosts);

        GameObject containerObject = new GameObject(name + ContainerSuffix);
        container = containerObject.transform;

        ghosts = new SpriteRenderer[count];
        remainingLife = new float[count];

        for (int i = 0; i < count; i++)
        {
            GameObject ghostObject = new GameObject("Afterimage");
            ghostObject.transform.SetParent(container, false);
            ghostObject.SetActive(false);

            ghosts[i] = ghostObject.AddComponent<SpriteRenderer>();
        }
    }

    private void SpawnGhost()
    {
        int index = nextGhostIndex;
        nextGhostIndex = (nextGhostIndex + 1) % ghosts.Length;

        SpriteRenderer ghost = ghosts[index];
        Transform sourceTransform = sourceRenderer.transform;
        Transform ghostTransform = ghost.transform;

        ghostTransform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
        ghostTransform.localScale = sourceTransform.lossyScale;

        ghost.sprite = sourceRenderer.sprite;
        ghost.flipX = sourceRenderer.flipX;
        ghost.flipY = sourceRenderer.flipY;
        ghost.sharedMaterial = sourceRenderer.sharedMaterial;
        ghost.sortingLayerID = sourceRenderer.sortingLayerID;
        ghost.sortingOrder = sourceRenderer.sortingOrder - Mathf.Max(0, sortingOrderOffset);
        ghost.color = new Color(tint.r, tint.g, tint.b, startAlpha);

        remainingLife[index] = fadeDuration;
        ghost.gameObject.SetActive(true);
    }

    private void FadeGhosts()
    {
        if (ghosts == null) return;

        for (int i = 0; i < ghosts.Length; i++)
        {
            if (remainingLife[i] <= 0f) continue;

            remainingLife[i] -= Time.deltaTime;

            SpriteRenderer ghost = ghosts[i];
            if (ghost == null) continue;

            if (remainingLife[i] <= 0f)
            {
                remainingLife[i] = 0f;
                ghost.gameObject.SetActive(false);
                continue;
            }

            float life = fadeDuration > 0f ? remainingLife[i] / fadeDuration : 0f;
            Color color = ghost.color;
            color.a = startAlpha * life;
            ghost.color = color;
        }
    }
}
