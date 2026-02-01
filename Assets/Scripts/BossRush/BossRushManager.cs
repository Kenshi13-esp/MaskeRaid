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
    
    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPrefab;
    [SerializeField] private Vector2 gameOverImageSize = new Vector2(1080f, 870f);
    
    [Header("Victory")]
    [SerializeField] private Sprite exitSprite;
    
    private PlayerDashController2D playerDashController;
    private PlayerHealth playerHealth;
    private GameObject gameOverInstance;
    private VictoryHandler victoryHandler;
    private GameObject gameSceneUI;

    private int currentRoundIndex = 0;
    private int totalBossesDefeated = 0;
    private BossHealth currentBossHealth;
    private GameObject currentBossInstance;
    private int currentBossSequenceIndex = 0;
    private bool isGameOver = false;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerDashController = p.GetComponent<PlayerDashController2D>();
                playerHealth = p.GetComponent<PlayerHealth>();
            }
        }
        else
        {
            playerDashController = player.GetComponent<PlayerDashController2D>();
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        
        if (playerDashController == null)
        {
            Debug.LogError("[BossRushManager] No se encontró PlayerDashController2D en el Player.");
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("[BossRushManager] No se encontró PlayerHealth en el Player.");
        }
        else
        {
            playerHealth.OnPlayerDeath.AddListener(OnPlayerDeath);
        }
        
        victoryHandler = gameObject.AddComponent<VictoryHandler>();
        
        if (exitSprite != null)
        {
            victoryHandler.SetExitSprite(exitSprite);
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
        while (!isGameOver)
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

            while (currentBossHealth != null && !currentBossHealth.IsDead && !isGameOver)
            {
                yield return null;
            }

            if (isGameOver)
            {
                Debug.Log("[BossRushManager] Game Over detectado, deteniendo Boss Rush.");
                yield break;
            }

            totalBossesDefeated++;
            Debug.Log($"+++ Boss derrotado: {bossName} (Total: {totalBossesDefeated}) +++");
            
            if (bossName.Contains("Qetza"))
            {
                Debug.Log($"[BossRushManager] ¡VICTORIA! Boss Qetza derrotado. ¡Has ganado!");
                
                if (currentBossInstance != null)
                {
                    Destroy(currentBossInstance);
                }
                
                if (victoryHandler != null)
                {
                    victoryHandler.TriggerVictory();
                }
                
                yield break;
            }

            if (currentBossInstance != null)
            {
                Destroy(currentBossInstance);
            }

            currentBossSequenceIndex++;

            if (playerHealth != null)
            {
                playerHealth.HealToFull();
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

        PlaySpawnSound(bossPrefab.name);

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
    
    private void PlaySpawnSound(string prefabName)
    {
        if (prefabName.Contains("Glorbo"))
        {
            SoundManager.PlaySound(SoundType.GLORBO_SPAWN);
        }
        else if (prefabName.Contains("Oniki") || prefabName.Contains("Oni"))
        {
            SoundManager.PlaySound(SoundType.ONI_SPAWN);
        }
        else if (prefabName.Contains("Qetza"))
        {
            SoundManager.PlaySound(SoundType.QETZA_SPAWN);
        }
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

    private void OnPlayerDeath()
    {
        isGameOver = true;
        Debug.Log($"[BossRushManager] ========== GAME OVER ==========");
        Debug.Log($"[BossRushManager] Rondas completadas: {currentRoundIndex - 1}");
        Debug.Log($"[BossRushManager] Bosses derrotados: {totalBossesDefeated}");
        Debug.Log($"[BossRushManager] ================================");
        
        if (player != null)
        {
            player.gameObject.SetActive(false);
        }
        
        if (currentBossInstance != null)
        {
            var bossController = currentBossInstance.GetComponent<IBossController>();
            if (bossController != null)
            {
                MonoBehaviour bossMono = bossController as MonoBehaviour;
                if (bossMono != null)
                {
                    bossMono.enabled = false;
                }
            }
            
            currentBossInstance.SetActive(false);
        }
        
        GameObject uiObject = GameObject.Find("UI");
        if (uiObject != null)
        {
            gameSceneUI = uiObject;
            gameSceneUI.SetActive(false);
            Debug.Log("[BossRushManager] UI de GameScene desactivado.");
        }
        
        if (gameOverPrefab != null)
        {
            gameOverInstance = Instantiate(gameOverPrefab);
            
            Transform imageTransform = gameOverInstance.transform.Find("GameOverImage");
            if (imageTransform != null)
            {
                RectTransform rectTransform = imageTransform.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = gameOverImageSize;
                }
            }
            
            Debug.Log("[BossRushManager] Game Over UI mostrado.");
        }
        else
        {
            Debug.LogWarning("[BossRushManager] gameOverPrefab no está asignado en el Inspector.");
        }
        
        Time.timeScale = 0f;
    }
    
    private void OnDestroy()
    {
        if (gameSceneUI != null)
        {
            gameSceneUI.SetActive(true);
        }
    }
}

