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
    
    private PlayerDashController2D playerDashController;

    private int currentRoundIndex = 0;
    private int totalBossesDefeated = 0;
    private BossHealth currentBossHealth;
    private GameObject currentBossInstance;
    private int currentBossSequenceIndex = 0;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerDashController = p.GetComponent<PlayerDashController2D>();
            }
        }
        else
        {
            playerDashController = player.GetComponent<PlayerDashController2D>();
        }
        
        if (playerDashController == null)
        {
            Debug.LogError("[BossRushManager] No se encontró PlayerDashController2D en el Player.");
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

        StartCoroutine(RunBossRush());
    }

    private IEnumerator RunBossRush()
    {
        while (true)
        {
            currentRoundIndex++;

            if (currentBossSequenceIndex >= bossPrefabs.Count)
            {
                Debug.Log($"=== CICLO COMPLETADO: Todos los {bossPrefabs.Count} bosses derrotados. Reiniciando secuencia... ===");
                currentBossSequenceIndex = 0;
            }

            GameObject bossPrefab = bossPrefabs[currentBossSequenceIndex];

            if (bossPrefab == null)
            {
                Debug.LogError($"[BossRushManager] Boss en índice {currentBossSequenceIndex} es null.");
                currentBossSequenceIndex++;
                continue;
            }

            string bossName = bossPrefab.name;
            Debug.Log($"=== RONDA {currentRoundIndex}: {bossName} (Boss #{currentBossSequenceIndex + 1}/{bossPrefabs.Count}) ===");

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

            currentBossSequenceIndex++;

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
        
        currentBossHealth.OnBossDeath.RemoveListener(OnBossDefeated);
        currentBossHealth.OnBossDeath.AddListener(OnBossDefeated);

        ActivateBoss(currentBossInstance);
    }
    
    private void OnBossDefeated(DashAbility dashAbility)
    {
        Debug.Log($"[BossRushManager] ¡Boss derrotado! Otorgando poder: {dashAbility?.AbilityName ?? "null"}");
        
        if (dashAbility != null && playerDashController != null)
        {
            playerDashController.SetDashAbility(dashAbility);
        }
        else if (playerDashController == null)
        {
            Debug.LogWarning("[BossRushManager] No se puede otorgar poder: PlayerDashController es null.");
        }
        
        totalBossesDefeated++;
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

