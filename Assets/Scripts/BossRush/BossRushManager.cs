using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRushManager : MonoBehaviour
{
    [Header("Boss Pool")]
    [SerializeField] private List<GameObject> bossPrefabs = new List<GameObject>();

    [Header("Rush Settings")]
    [SerializeField] private float introDelay = 1f;
    [SerializeField] private float afterWinDelay = 1f;

    [Header("Spawn")]
    [SerializeField] private Transform bossSpawnPoint;

    [Header("Refs")]
    [SerializeField] private Transform player;

    private int currentRoundIndex = 0;
    private int totalBossesDefeated = 0;
    private BossHealth currentBossHealth;
    private GameObject currentBossInstance;
    private List<int> availableBossIndices = new List<int>();

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

        if (bossPrefabs == null || bossPrefabs.Count == 0)
        {
            Debug.LogError("[BossRushManager] bossPrefabs está vacío. Agrega prefabs de bosses a la lista.");
            return;
        }

        InitializeBossPool();
        StartCoroutine(RunBossRush());
    }

    private void InitializeBossPool()
    {
        availableBossIndices.Clear();
        for (int i = 0; i < bossPrefabs.Count; i++)
        {
            if (bossPrefabs[i] != null)
            {
                availableBossIndices.Add(i);
            }
        }
    }

    private IEnumerator RunBossRush()
    {
        while (true)
        {
            currentRoundIndex++;

            if (availableBossIndices.Count == 0)
            {
                Debug.Log($"=== CICLO COMPLETADO: Todos los {bossPrefabs.Count} bosses derrotados. Reiniciando pool... ===");
                InitializeBossPool();
            }

            int randomIndex = Random.Range(0, availableBossIndices.Count);
            int bossIndex = availableBossIndices[randomIndex];
            availableBossIndices.RemoveAt(randomIndex);

            GameObject bossPrefab = bossPrefabs[bossIndex];

            if (bossPrefab == null)
            {
                Debug.LogError($"[BossRushManager] Boss en índice {bossIndex} es null.");
                continue;
            }

            string bossName = bossPrefab.name;
            Debug.Log($"=== RONDA {currentRoundIndex}: {bossName} ({totalBossesDefeated + 1} total) ===");

            yield return new WaitForSeconds(introDelay);

            SpawnBoss(bossPrefab);

            while (currentBossHealth != null && !currentBossHealth.IsDead)
            {
                yield return null;
            }

            totalBossesDefeated++;
            Debug.Log($"+++ Boss derrotado: {bossName} (Total: {totalBossesDefeated}) +++");

            if (currentBossInstance != null)
            {
                Destroy(currentBossInstance);
            }

            yield return new WaitForSeconds(afterWinDelay);
        }
    }

    private void SpawnBoss(GameObject bossPrefab)
    {
        if (currentBossInstance != null)
        {
            Destroy(currentBossInstance);
        }

        currentBossInstance = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

        currentBossHealth = currentBossInstance.GetComponent<BossHealth>();
        if (currentBossHealth == null)
        {
            Debug.LogError("[BossRushManager] El bossPrefab NO tiene BossHealth. Añádelo al prefab.");
            return;
        }

        currentBossHealth.ResetHealth();

        ActivateBoss(currentBossInstance);
    }

    private void ActivateBoss(GameObject bossInstance)
    {
        var bossController = bossInstance.GetComponent<IBossController>();
        if (bossController != null)
        {
            bossController.ActivateBoss();
        }
        else
        {
            Debug.LogWarning($"[BossRushManager] El boss '{bossInstance.name}' no implementa IBossController.");
        }
    }
}

