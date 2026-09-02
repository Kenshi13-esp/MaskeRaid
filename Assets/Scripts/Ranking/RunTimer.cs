using TMPro;
using UnityEngine;

/// <summary>
/// Cronometro de la partida. Aparece y arranca en cuanto sale el primer boss, y se detiene al
/// llegar a la pantalla de victoria o de derrota. Si la partida se completa, registra el tiempo
/// en el ranking con el nombre que se escribio en el menu.
///
/// El arranque se engancha a <see cref="BossHealth.ActiveBossChanged"/> en lugar de pedirselo al
/// gestor del boss rush: el evento ya existe y se dispara exactamente cuando el boss entra en
/// escena, asi que el cronometro no necesita conocer a nadie.
///
/// Cuenta en tiempo real (<see cref="Time.unscaledDeltaTime"/>) y se detiene en pausa. Con
/// tiempo escalado, cargar el especial en camara lenta habria ralentizado el cronometro y
/// cargarlo sin parar seria la forma optima de batir el record.
/// </summary>
public class RunTimer : MonoBehaviour
{
    private const float SecondsPerMinute = 60f;

    [Header("UI")]
    [Tooltip("Objeto que se muestra al arrancar el cronometro. Vacio = este GameObject")]
    [SerializeField] private GameObject visualRoot;

    [SerializeField] private TextMeshProUGUI timerLabel;

    [Header("Comportamiento")]
    [Tooltip("Mantiene el cronometro oculto hasta que aparece el primer boss")]
    [SerializeField] private bool hideUntilFirstBoss = true;

    private string lastDisplayedTime;
    private bool wasRecorded;

    /// <summary>Cronometro activo en la escena, o null si no hay ninguno.</summary>
    public static RunTimer Active { get; private set; }

    /// <summary>Tiempo acumulado de la partida en segundos.</summary>
    public float ElapsedSeconds { get; private set; }

    /// <summary>True mientras el cronometro esta contando.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>True desde que arranco por primera vez, aunque ya se haya detenido.</summary>
    public bool HasStarted { get; private set; }

    /// <summary>Tiempo actual ya formateado para mostrar.</summary>
    public string ElapsedText => FormatTime(ElapsedSeconds);

    private void OnEnable()
    {
        Active = this;

        if (visualRoot == null) visualRoot = gameObject;

        BossHealth.ActiveBossChanged += OnActiveBossChanged;

        ResetTimer();

        // Si el boss ya estaba en escena antes de habilitarse el cronometro, arranca ya.
        if (BossHealth.ActiveBoss != null) StartTimer();
    }

    private void OnDisable()
    {
        BossHealth.ActiveBossChanged -= OnActiveBossChanged;

        if (Active == this) Active = null;
    }

    private void Update()
    {
        if (!IsRunning) return;

        // La pausa no debe contar: el reloj del menu de pausa no es tiempo de juego.
        if (GamePause.IsPaused) return;

        ElapsedSeconds += Time.unscaledDeltaTime;

        RefreshLabel();
    }

    /// <summary>Arranca el cronometro y muestra el marcador. Repetirlo no lo reinicia.</summary>
    public void StartTimer()
    {
        if (HasStarted) return;

        HasStarted = true;
        IsRunning = true;

        SetVisible(true);
        RefreshLabel();
    }

    /// <summary>Detiene el cronometro sin registrar nada. Es el caso de la derrota.</summary>
    public void Stop()
    {
        IsRunning = false;
        RefreshLabel();
    }

    /// <summary>
    /// Detiene el cronometro y guarda la marca en el ranking. Devuelve el puesto conseguido
    /// empezando en 1, o -1 si no ha entrado en la tabla, la partida nunca arranco o ya se
    /// habia registrado antes.
    /// </summary>
    public int StopAndRecord()
    {
        bool wasStarted = HasStarted;

        Stop();

        if (!wasStarted || wasRecorded) return -1;

        wasRecorded = true;

        int position = RankingStore.AddRun(PlayerSession.PlayerName, ElapsedSeconds);

        Debug.Log($"[RunTimer] Partida completada por {PlayerSession.PlayerName} en {ElapsedText}. " +
                  (position > 0 ? $"Puesto {position} del ranking." : "No ha entrado en el ranking."));

        return position;
    }

    /// <summary>Devuelve el cronometro a cero y lo vuelve a ocultar.</summary>
    public void ResetTimer()
    {
        ElapsedSeconds = 0f;
        IsRunning = false;
        HasStarted = false;
        wasRecorded = false;
        lastDisplayedTime = null;

        SetVisible(!hideUntilFirstBoss);
        RefreshLabel();
    }

    /// <summary>Formatea un tiempo como MM:SS.cc, el formato de marcador de arcade.</summary>
    public static string FormatTime(float seconds)
    {
        float clamped = Mathf.Max(0f, seconds);

        int minutes = (int)(clamped / SecondsPerMinute);
        int wholeSeconds = (int)(clamped % SecondsPerMinute);
        int hundredths = (int)((clamped - Mathf.Floor(clamped)) * 100f);

        return $"{minutes:00}:{wholeSeconds:00}.{hundredths:00}";
    }

    private void OnActiveBossChanged(BossHealth boss)
    {
        if (boss == null) return;

        StartTimer();
    }

    /// <summary>Escribe en el marcador solo cuando el texto cambia, para no ensuciar el canvas.</summary>
    private void RefreshLabel()
    {
        if (timerLabel == null) return;

        string formatted = FormatTime(ElapsedSeconds);
        if (formatted == lastDisplayedTime) return;

        lastDisplayedTime = formatted;
        timerLabel.text = formatted;
    }

    private void SetVisible(bool visible)
    {
        // Si el raiz visual es este mismo objeto no se puede desactivar, porque Update dejaria de
        // correr y el cronometro no avanzaria: en ese caso se apaga solo el texto.
        if (visualRoot != null && visualRoot != gameObject)
        {
            if (visualRoot.activeSelf != visible) visualRoot.SetActive(visible);
            return;
        }

        if (timerLabel != null && timerLabel.enabled != visible) timerLabel.enabled = visible;
    }
}
