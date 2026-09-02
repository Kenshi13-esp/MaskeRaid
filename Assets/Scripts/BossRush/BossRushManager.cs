using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquesta el boss rush: instancia los bosses en orden, entrega su mascara al jugador al
/// derrotarlos y gestiona la victoria y el game over.
/// </summary>
public class BossRushManager : MonoBehaviour
{
    private const string FinalBossNameToken = "Qetza";

    [Header("Boss Pool")]
    [Tooltip("Bosses en el orden en el que apareceran")]
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

    private PlayerMaskController playerMaskController;
    private PlayerHealth playerHealth;
    private GameObject gameOverInstance;
    private VictoryHandler victoryHandler;
    private GameObject gameSceneUI;

    private int currentRoundIndex;
    private int totalBossesDefeated;
    private int currentBossSequenceIndex;
    private bool isGameOver;

    private BossHealth currentBossHealth;
    private GameObject currentBossInstance;

    private void Awake()
    {
        ResolvePlayerReferences();

        victoryHandler = gameObject.AddComponent<VictoryHandler>();

        if (exitSprite != null) victoryHandler.SetExitSprite(exitSprite);
    }

    private void Start()
    {
        if (bossSpawnPoint == null)
        {
            Debug.LogError("[BossRushManager] Falta bossSpawnPoint.", this);
            return;
        }

        if (bossPrefabs == null || bossPrefabs.Count == 0)
        {
            Debug.LogError("[BossRushManager] bossPrefabs esta vacio.", this);
            return;
        }

        StartCoroutine(RunBossRush());
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnPlayerDeath.RemoveListener(OnPlayerDeath);
        if (gameSceneUI != null) gameSceneUI.SetActive(true);
    }

    private void ResolvePlayerReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (player == null)
        {
            Debug.LogError("[BossRushManager] No se encontro el Player en la escena.", this);
            return;
        }

        playerMaskController = player.GetComponent<PlayerMaskController>();
        playerHealth = player.GetComponent<PlayerHealth>();

        if (playerMaskController == null)
        {
            Debug.LogError("[BossRushManager] El Player no tiene PlayerMaskController.", this);
        }

        if (playerHealth == null)
        {
            Debug.LogError("[BossRushManager] El Player no tiene PlayerHealth.", this);
            return;
        }

        playerHealth.OnPlayerDeath.AddListener(OnPlayerDeath);
    }

    private IEnumerator RunBossRush()
    {
        while (!isGameOver)
        {
            currentRoundIndex++;

            if (currentBossSequenceIndex >= bossPrefabs.Count) currentBossSequenceIndex = 0;

            GameObject bossPrefab = bossPrefabs[currentBossSequenceIndex];

            if (bossPrefab == null)
            {
                Debug.LogError($"[BossRushManager] El boss del indice {currentBossSequenceIndex} es null.", this);
                currentBossSequenceIndex++;
                continue;
            }

            string bossName = bossPrefab.name;

            yield return new WaitForSeconds(introDelay);

            SpawnBoss(bossPrefab);

            while (currentBossHealth != null && !currentBossHealth.IsDead && !isGameOver)
            {
                yield return null;
            }

            if (isGameOver) yield break;

            totalBossesDefeated++;

            if (bossName.Contains(FinalBossNameToken))
            {
                TriggerVictory();
                yield break;
            }

            if (currentBossInstance != null) Destroy(currentBossInstance);

            currentBossSequenceIndex++;

            if (playerHealth != null) playerHealth.HealToFull();

            yield return new WaitForSeconds(afterWinDelay);
        }
    }

    private void SpawnBoss(GameObject bossPrefab)
    {
        if (currentBossInstance != null) Destroy(currentBossInstance);

        currentBossInstance = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

        PlaySpawnSound(bossPrefab.name);

        currentBossHealth = currentBossInstance.GetComponent<BossHealth>();

        if (currentBossHealth == null)
        {
            Debug.LogError($"[BossRushManager] El prefab '{bossPrefab.name}' no tiene BossHealth.", this);
            return;
        }

        currentBossHealth.ResetHealth();

        currentBossHealth.OnBossDeath.RemoveListener(OnBossDefeated);
        currentBossHealth.OnBossDeath.AddListener(OnBossDefeated);

        ActivateBoss(currentBossInstance);
    }

    private void PlaySpawnSound(string prefabName)
    {
        if (prefabName.Contains("Glorbo")) SoundManager.PlaySound(SoundType.GLORBO_SPAWN);
        else if (prefabName.Contains("Oni")) SoundManager.PlaySound(SoundType.ONI_SPAWN);
        else if (prefabName.Contains(FinalBossNameToken)) SoundManager.PlaySound(SoundType.QETZA_SPAWN);
    }

    private void OnBossDefeated(MaskDefinition mask)
    {
        if (mask == null || playerMaskController == null) return;

        playerMaskController.EquipMask(mask);
    }

    private void ActivateBoss(GameObject bossInstance)
    {
        IBossController bossController = bossInstance.GetComponent<IBossController>();

        if (bossController == null)
        {
            Debug.LogWarning($"[BossRushManager] '{bossInstance.name}' no implementa IBossController.", this);
            return;
        }

        bossController.ActivateBoss();
    }

    private void TriggerVictory()
    {
        if (currentBossInstance != null) Destroy(currentBossInstance);

        if (victoryHandler != null) victoryHandler.TriggerVictory();
    }

    private void OnPlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;

        Debug.Log($"[BossRushManager] GAME OVER. Rondas: {currentRoundIndex - 1}. Bosses derrotados: {totalBossesDefeated}.");

        // El cronometro se para antes de tocar la UI, que es lo que lo aloja. La derrota no
        // registra marca: al ranking solo entran las partidas completadas.
        if (RunTimer.Active != null) RunTimer.Active.Stop();

        if (player != null) player.gameObject.SetActive(false);

        DisableCurrentBoss();
        HideGameplayUI();
        ShowGameOverScreen();

        GamePause.SetGameFinished(true);
    }

    private void DisableCurrentBoss()
    {
        if (currentBossInstance == null) return;

        MonoBehaviour bossController = currentBossInstance.GetComponent<IBossController>() as MonoBehaviour;
        if (bossController != null) bossController.enabled = false;

        currentBossInstance.SetActive(false);
    }

    private void HideGameplayUI()
    {
        GameObject uiObject = GameObject.Find("UI");
        if (uiObject == null) return;

        gameSceneUI = uiObject;
        gameSceneUI.SetActive(false);
    }

    private void ShowGameOverScreen()
    {
        if (gameOverPrefab == null)
        {
            Debug.LogWarning("[BossRushManager] gameOverPrefab no esta asignado.", this);
            return;
        }

        gameOverInstance = Instantiate(gameOverPrefab);

        Transform imageTransform = gameOverInstance.transform.Find("GameOverImage");
        if (imageTransform == null) return;

        RectTransform imageRect = imageTransform.GetComponent<RectTransform>();
        if (imageRect != null) imageRect.sizeDelta = gameOverImageSize;
    }
}
