using System.Collections;
using UnityEngine;

public class BossRushManager : MonoBehaviour
{
    [System.Serializable]
    public class Round
    {
        public string roundName = "Round";
        public GameObject bossPrefab;
        public float introDelay = 1f;   // espera antes de spawnear
        public float afterWinDelay = 1f; // espera tras matar boss
    }

    [Header("Boss Rush (5 rounds)")]
    [SerializeField] private Round[] rounds = new Round[5];

    [Header("Spawn")]
    [SerializeField] private Transform bossSpawnPoint;

    [Header("Refs")]
    [SerializeField] private Transform player;

    private int currentRoundIndex = -1;
    private BossHealth currentBossHealth;
    private GameObject currentBossInstance;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Start()
    {
        if (bossSpawnPoint == null)
        {
            Debug.LogError("[BossRushManager] Falta bossSpawnPoint.");
            return;
        }

        if (rounds == null || rounds.Length != 5)
        {
            Debug.LogError("[BossRushManager] rounds debe tener EXACTAMENTE 5 elementos.");
            return;
        }

        StartCoroutine(RunBossRush());
    }

    private IEnumerator RunBossRush()
    {
        for (int i = 0; i < rounds.Length; i++)
        {
            currentRoundIndex = i;

            Round r = rounds[i];
            if (r.bossPrefab == null)
            {
                Debug.LogError($"[BossRushManager] Round {i + 1} no tiene bossPrefab asignado.");
                yield break;
            }

            Debug.Log($"=== RONDA {i + 1}/5: {r.roundName} ===");

            yield return new WaitForSeconds(r.introDelay);

            SpawnBoss(r.bossPrefab);

            // Esperar a que el boss muera
            while (currentBossHealth != null && !currentBossHealth.IsDead)
                yield return null;

            Debug.Log($"+++ Boss derrotado: {r.roundName} +++");

            // limpiar instancia por si acaso
            if (currentBossInstance != null)
                Destroy(currentBossInstance);

            yield return new WaitForSeconds(r.afterWinDelay);
        }

        Debug.Log("VICTORIA: Boss Rush completado (5/5).");
    }

    private void SpawnBoss(GameObject bossPrefab)
    {
        if (currentBossInstance != null)
            Destroy(currentBossInstance);

        currentBossInstance = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

        currentBossHealth = currentBossInstance.GetComponent<BossHealth>();
        if (currentBossHealth == null)
        {
            Debug.LogError("[BossRushManager] El bossPrefab NO tiene BossHealth. Añádelo al prefab.");
            return;
        }

        // Conectar target al player si el boss lo necesita
        var dingle = currentBossInstance.GetComponent<BossJellyDingle2D>();
        if (dingle != null && player != null)
        {
            // el boss ya busca player por tag, pero si quieres forzarlo:
            // dingle.SetTarget(player);  (si añadimos SetTarget en ese script)
        }

        currentBossHealth.ResetHealth(); // por si es un prefab reutilizado
    }
}

