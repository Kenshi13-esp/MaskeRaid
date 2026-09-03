using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Orquesta el boss rush: instancia los bosses en orden, entrega su mascara al jugador al
/// derrotarlos y gestiona la victoria y el game over.
/// </summary>
public class BossRushManager : MonoBehaviour
{
    private const string FinalBossNameToken = "Qetza";
    private const string GameplayUiRootName = "UI";
    private const string MainMenuSceneName = "MainMenu";

    private static readonly string[] GameplayUiElements =
    {
        "HPBoss_BG",
        "PlayerHealthBar",
        "RunTimerPanel",
        "PauseButton",
        "Pause",
        "SoundButton"
    };

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

    [Header("UI Panels (Pre-colocados en escena)")]
    [Tooltip("Panel que se activa al perder")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Panel que se activa al ganar")]
    [SerializeField] private GameObject victoryPanel;

    [Tooltip("Etiqueta de tiempo dentro del panel de victoria")]
    [SerializeField] private TextMeshProUGUI victoryTimerLabel;

    [Header("Redireccion")]
    [Tooltip("Segundos antes de volver al menu principal tras mostrar el panel de fin")]
    [SerializeField] private float delayBeforeRedirect = 4f;

    [Header("Desvanecimiento")]
    [Tooltip("CanvasGroup del panel negro a pantalla completa usado para el fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("Opacidad maxima del desvanecimiento (0 = transparente, 1 = negro absoluto)")]
    [SerializeField] private float fadeMaxAlpha = 0.85f;

    [Tooltip("Duracion del desvanecimiento en segundos")]
    [SerializeField] private float fadeDuration = 1f;

    private PlayerMaskController playerMaskController;
    private PlayerHealth playerHealth;
    private readonly List<GameObject> hiddenUiElements = new List<GameObject>();

    private int currentRoundIndex;
    private int totalBossesDefeated;
    private int currentBossSequenceIndex;
    private bool isGameOver;

    private BossHealth currentBossHealth;
    private GameObject currentBossInstance;

    private void Awake()
    {
        ResolvePlayerReferences();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
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
        RestoreGameplayUi();
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

    /// <summary>Detiene el cronometro, desvanece a negro, muestra el panel de victoria y redirige.</summary>
    private void TriggerVictory()
    {
        if (currentBossInstance != null) Destroy(currentBossInstance);

        string elapsedText = string.Empty;
        int position = -1;

        if (RunTimer.Active != null)
        {
            elapsedText = RunTimer.Active.ElapsedText;
            position = RunTimer.Active.StopAndRecord();
            Debug.Log($"[BossRushManager] Victoria: {PlayerSession.PlayerName} - {elapsedText}. Puesto {position}.");
        }

        HideGameplayUi();
        GamePause.SetGameFinished(true);

        StartCoroutine(FadeToBlackThenShowPanel(victoryPanel, victoryTimerLabel, elapsedText, position));
    }

    /// <summary>Detiene el cronometro, borra el nombre, desvanece a negro, muestra el panel y redirige.</summary>
    private void OnPlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;

        Debug.Log($"[BossRushManager] GAME OVER. Rondas: {currentRoundIndex - 1}. Bosses derrotados: {totalBossesDefeated}.");

        if (RunTimer.Active != null) RunTimer.Active.Stop();

        PlayerSession.Clear();

        if (player != null) player.gameObject.SetActive(false);

        DisableCurrentBoss();
        HideGameplayUi();
        GamePause.SetGameFinished(true);

        StartCoroutine(FadeToBlackThenShowPanel(gameOverPanel, null, string.Empty, -1));
    }

    /// <summary>
    /// Desvanece la pantalla a negro, activa el panel de fin encima del fade y, tras un tiempo,
    /// redirige al menu principal. Usa tiempo real porque Time.timeScale esta a cero.
    /// </summary>
    private IEnumerator FadeToBlackThenShowPanel(GameObject panel, TextMeshProUGUI timerLabel, string elapsedText, int position)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration) * fadeMaxAlpha;
                yield return null;
            }

            fadeCanvasGroup.alpha = fadeMaxAlpha;
        }

        if (panel != null)
        {
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);

            if (timerLabel != null)
            {
                timerLabel.text = position > 0
                    ? $"{elapsedText}\nPUESTO {position}"
                    : elapsedText;
            }
        }
        else
        {
            Debug.LogWarning("[BossRushManager] Panel de fin no asignado.", this);
        }

        float remainingDelay = delayBeforeRedirect;

        if (fadeCanvasGroup != null) remainingDelay = Mathf.Max(0f, delayBeforeRedirect - fadeDuration);

        yield return new WaitForSecondsRealtime(remainingDelay);

        GamePause.ResetState();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void DisableCurrentBoss()
    {
        if (currentBossInstance == null) return;

        MonoBehaviour bossController = currentBossInstance.GetComponent<IBossController>() as MonoBehaviour;
        if (bossController != null) bossController.enabled = false;

        currentBossInstance.SetActive(false);
    }

    /// <summary>
    /// Oculta los elementos de HUD de jugabilidad sin apagar el raiz /UI, para que los paneles
    /// de victoria y derrota (que son hijos de ese raiz) sigan visibles al activarse.
    /// </summary>
    private void HideGameplayUi()
    {
        GameObject uiRoot = GameObject.Find(GameplayUiRootName);
        if (uiRoot == null) return;

        hiddenUiElements.Clear();

        foreach (string elementName in GameplayUiElements)
        {
            Transform child = uiRoot.transform.Find(elementName);
            if (child == null || !child.gameObject.activeSelf) continue;

            child.gameObject.SetActive(false);
            hiddenUiElements.Add(child.gameObject);
        }
    }

    /// <summary>Reactiva los elementos de UI que se ocultaron al terminar la partida.</summary>
    private void RestoreGameplayUi()
    {
        foreach (GameObject element in hiddenUiElements)
        {
            if (element != null) element.SetActive(true);
        }

        hiddenUiElements.Clear();
    }

}
