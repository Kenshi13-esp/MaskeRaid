using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Abre y cierra el menu de pausa. El estado real vive en <see cref="GamePause"/> para que la
/// jugabilidad y la entrada lo consulten sin depender de la UI.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject pauseButton;

    private MenuPanel pausePanel;

    /// <summary>True si el menu de pausa esta abierto.</summary>
    public bool IsPaused => GamePause.IsPaused;

    private void Awake()
    {
        if (pauseMenu != null) pausePanel = pauseMenu.GetComponent<MenuPanel>();
    }

    private void Start()
    {
        GamePause.ResetState();
        ApplyMenuState(false);
    }

    private void OnEnable()
    {
        GamePause.PauseChanged += ApplyMenuState;

        // El boton de atras del mando cierra el menu por aqui para que se restaure el timeScale.
        if (pausePanel != null) pausePanel.BackRequested += Resume;
    }

    private void OnDisable()
    {
        GamePause.PauseChanged -= ApplyMenuState;

        if (pausePanel != null) pausePanel.BackRequested -= Resume;
    }

    /// <summary>Pausa la partida y abre el menu.</summary>
    public void Pause()
    {
        GamePause.SetPaused(true);
    }

    /// <summary>Reanuda la partida y cierra el menu.</summary>
    public void Resume()
    {
        GamePause.SetPaused(false);
    }

    /// <summary>Alterna entre pausa y juego. La usan el boton del HUD y la entrada de mando.</summary>
    public void TogglePause()
    {
        GamePause.SetPaused(!GamePause.IsPaused);
    }

    /// <summary>Reinicia la escena actual.</summary>
    public void Restart()
    {
        GamePause.ResetState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Vuelve al menu principal.</summary>
    public void ReturnToMainMenu()
    {
        GamePause.ResetState();
        SceneManager.LoadScene("MainMenu");
    }

    private void ApplyMenuState(bool paused)
    {
        if (pauseMenu != null) pauseMenu.SetActive(paused);
        if (pauseButton != null) pauseButton.SetActive(!paused);
    }
}
