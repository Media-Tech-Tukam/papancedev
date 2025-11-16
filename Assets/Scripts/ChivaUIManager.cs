using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChivaUIManager : MonoBehaviour
{
    [Header("Main Game UI")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject defeatScreen;
    
    [Header("Progress UI")]
    [SerializeField] private Slider routeProgressBar;
    [SerializeField] private TextMeshProUGUI routeProgressText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI remainingDistanceText;
    
    [Header("Drunkenness UI")]
    [SerializeField] private Slider drunkennessBar;
    [SerializeField] private TextMeshProUGUI drunkennessText;
    [SerializeField] private Image drunkennessBarFill;
    [SerializeField] private Color soberColor = Color.green;
    [SerializeField] private Color drunkColor = Color.red;
    [SerializeField] private GameObject drunkWarning;
    [SerializeField] private float warningThreshold = 0.8f;
    
    [Header("Vehicle UI")]
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI speedUnitsText;
    [SerializeField] private Slider speedBar;
    [SerializeField] private float maxSpeedForUI = 30f; // km/h para la barra
    [SerializeField] private GameObject brakingIndicator;
    
    [Header("Passengers UI")]
    [SerializeField] private TextMeshProUGUI passengerCountText;
    [SerializeField] private TextMeshProUGUI passengerPickupText; // Para mostrar "+3 passengers!"
    [SerializeField] private GameObject electrolitNotification;
    [SerializeField] private float notificationDuration = 2f;
    
    [Header("Time & Stats")]
    [SerializeField] private TextMeshProUGUI gameTimeText;
    [SerializeField] private TextMeshProUGUI checkpointText;
    [SerializeField] private GameObject newCheckpointNotification;
    
    [Header("Loading Screen")]
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private TextMeshProUGUI loadingStatusText;
    [SerializeField] private TextMeshProUGUI loadingTipsText;
    [SerializeField] private string[] loadingTips;
    
    [Header("Victory Screen")]
    [SerializeField] private TextMeshProUGUI finalTimeText;
    [SerializeField] private TextMeshProUGUI finalPassengersText;
    [SerializeField] private TextMeshProUGUI finalStatsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    
    [Header("Defeat Screen")]
    [SerializeField] private TextMeshProUGUI defeatReasonText;
    [SerializeField] private TextMeshProUGUI defeatStatsText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;
    
    [Header("Pause Menu")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseQuitButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    
    [Header("Controls Info")]
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private TextMeshProUGUI controlsText;
    [SerializeField] private bool showControlsAtStart = true;
    [SerializeField] private float controlsDisplayTime = 5f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip notificationSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;
    [SerializeField] private float uiVolume = 0.5f;
    
    [Header("Animation")]
    [SerializeField] private bool animateUIElements = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugInfoText;
    
    // Referencias del sistema
    private ChivaGameManager gameManager;
    private ChivaController chivaController;
    private DrunkennessSystem drunkennessSystem;
    private PassengerManager passengerManager;
    
    // Estado de UI
    private bool isGameUIVisible = false;
    private bool isPaused = false;
    private Coroutine notificationCoroutine;
    private Coroutine drunkWarningCoroutine;
    
    // Cache de textos para performance
    private string cachedDistanceFormat = "0.0";
    private string cachedSpeedFormat = "0";
    private string cachedTimeFormat = "mm\\:ss";
    
    void Start()
    {
        Debug.Log("=== CHIVA UI MANAGER STARTING ===");
        
        InitializeComponents();
        SetupEventListeners();
        SetupUIElements();
        InitializeUI();
        
        Debug.Log("UI Manager initialized");
    }
    
    void InitializeComponents()
    {
        // Buscar referencias del sistema
        gameManager = FindObjectOfType<ChivaGameManager>();
        chivaController = FindObjectOfType<ChivaController>();
        drunkennessSystem = FindObjectOfType<DrunkennessSystem>();
        passengerManager = FindObjectOfType<PassengerManager>();
        
        // Audio source
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
                uiAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Validar componentes críticos
        if (gameManager == null)
            Debug.LogError("ChivaUIManager: GameManager not found!");
    }
    
    void SetupEventListeners()
    {
        // Game Manager events
        if (gameManager != null)
        {
            gameManager.OnGameStart += OnGameStart;
            gameManager.OnGameEnd += OnGameEnd;
            gameManager.OnCheckpointReached += OnCheckpointReached;
            gameManager.OnGamePause += OnGamePause;
        }
        
        // Chiva Controller events
        if (chivaController != null)
        {
            chivaController.OnSpeedChanged += OnSpeedChanged;
            chivaController.OnProgressChanged += OnProgressChanged;
            chivaController.OnBrakingStateChanged += OnBrakingStateChanged;
        }
        
        // Drunkenness System events
        if (drunkennessSystem != null)
        {
            drunkennessSystem.OnDrunkennessChanged += OnDrunkennessChanged;
            drunkennessSystem.OnElectroliting += OnElectrolitEffect;
        }
        
        // Passenger Manager events
        if (passengerManager != null)
        {
            passengerManager.OnPassengerPickup += OnPassengerPickup;
            passengerManager.OnElectrolitPickup += OnElectrolitPickup;
        }
    }
    
    void SetupUIElements()
    {
        // Configurar botones
        if (restartButton != null)
            restartButton.onClick.AddListener(() => { PlayUISound(buttonClickSound); gameManager?.RestartGame(); });
        
        if (quitButton != null)
            quitButton.onClick.AddListener(() => { PlayUISound(buttonClickSound); gameManager?.QuitGame(); });
        
        if (retryButton != null)
            retryButton.onClick.AddListener(() => { PlayUISound(buttonClickSound); gameManager?.RestartGame(); });
        
        if (menuButton != null)
            menuButton.onClick.AddListener(() => { PlayUISound(buttonClickSound); gameManager?.QuitGame(); });
        
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => { PlayUISound(buttonClickSound); gameManager?.ResumeGame(); });
        
        if (pauseRestartButton != null)
            pauseRestartButton.onClick.AddListener(() => { PlayUISound(buttonClickSound); gameManager?.RestartGame(); });
        
        if (pauseQuitButton != null)
            pauseQuitButton.onClick.AddListener(() => { PlayUISound(buttonClickSound); gameManager?.QuitGame(); });
        
        // Configurar sliders
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
        }
        
        // Configurar barras de progreso
        if (routeProgressBar != null)
        {
            routeProgressBar.minValue = 0f;
            routeProgressBar.maxValue = 1f;
            routeProgressBar.value = 0f;
        }
        
        if (drunkennessBar != null)
        {
            drunkennessBar.minValue = 0f;
            drunkennessBar.maxValue = 1f;
            drunkennessBar.value = 0f;
        }
        
        if (speedBar != null)
        {
            speedBar.minValue = 0f;
            speedBar.maxValue = maxSpeedForUI;
            speedBar.value = 0f;
        }
        
        // Configurar textos de controles
        if (controlsText != null)
        {
            SetupControlsText();
        }
    }
    
    void InitializeUI()
    {
        // Estado inicial de paneles
        ShowLoadingUI();
        HideGameUI();
        HidePauseMenu();
        HideVictoryScreen();
        HideDefeatScreen();
        
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
        
        if (debugPanel != null)
            debugPanel.SetActive(debugMode);
        
        // Mostrar tips de loading
        if (showControlsAtStart)
            StartCoroutine(ShowControlsAtStart());
        
        UpdateLoadingTips();
    }
    
    void Update()
    {
        if (isGameUIVisible)
        {
            UpdateRealTimeUI();
            UpdateDebugInfo();
        }
    }
    
    void UpdateRealTimeUI()
    {
        // Actualizar tiempo de juego
        if (gameTimeText != null && gameManager != null)
        {
            float gameTime = gameManager.GetCurrentStats().playTime;
            gameTimeText.text = FormatTime(gameTime);
        }
        
        // Actualizar debug info
        if (debugMode && debugInfoText != null)
        {
            UpdateDebugText();
        }
    }
    
    void UpdateDebugInfo()
    {
        if (!debugMode || debugInfoText == null || gameManager == null) return;
        
        var stats = gameManager.GetCurrentStats();
        string debugText = $"STATE: {gameManager.CurrentState}\n";
        debugText += $"FPS: {1f/Time.unscaledDeltaTime:F0}\n";
        debugText += $"DISTANCE: {stats.distanceTraveled:F0}m\n";
        debugText += $"SPEED: {stats.currentSpeed * 3.6f:F0} km/h\n";
        debugText += $"DRUNK: {stats.currentDrunkenness:F1}%\n";
        debugText += $"PASSENGERS: {stats.passengersTotal}\n";
        debugText += $"TIME: {stats.playTime:F0}s";
        
        debugInfoText.text = debugText;
    }
    
    void UpdateDebugText()
    {
        if (gameManager == null) return;
        
        var stats = gameManager.GetCurrentStats();
        string debug = $"Distance: {stats.distanceTraveled:F0}m | Speed: {stats.currentSpeed * 3.6f:F0} km/h | Drunk: {stats.currentDrunkenness:F1}%";
        debugInfoText.text = debug;
    }
    
    // ========== UI STATE MANAGEMENT ==========
    
    public void ShowLoadingUI()
    {
        if (loadingUI != null)
        {
            loadingUI.SetActive(true);
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(loadingUI, true));
        }
        
        if (loadingStatusText != null)
            loadingStatusText.text = "Generando carretera...";
    }
    
    public void HideLoadingUI()
    {
        if (loadingUI != null)
        {
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(loadingUI, false));
            else
                loadingUI.SetActive(false);
        }
    }
    
    public void ShowGameUI()
    {
        if (gameUI != null)
        {
            gameUI.SetActive(true);
            isGameUIVisible = true;
            
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(gameUI, true));
        }
    }
    
    public void HideGameUI()
    {
        if (gameUI != null)
        {
            isGameUIVisible = false;
            
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(gameUI, false));
            else
                gameUI.SetActive(false);
        }
    }
    
    public void ShowPauseMenu()
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
            isPaused = true;
            
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(pauseMenu, true));
            
            PlayUISound(notificationSound);
        }
    }
    
    public void HidePauseMenu()
    {
        if (pauseMenu != null)
        {
            isPaused = false;
            
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(pauseMenu, false));
            else
                pauseMenu.SetActive(false);
        }
    }
    
    public void ShowVictoryScreen(ChivaGameManager.GameStats stats)
    {
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
            
            // Actualizar textos de victoria
            if (finalTimeText != null)
                finalTimeText.text = $"Tiempo: {FormatTime(stats.playTime)}";
            
            if (finalPassengersText != null)
                finalPassengersText.text = $"Pasajeros: {stats.passengersTotal}";
            
            if (finalStatsText != null)
            {
                string statsText = $"Distancia: {stats.distanceTraveled/1000f:F1} km\n";
                statsText += $"Velocidad promedio: {stats.averageSpeed * 3.6f:F0} km/h\n";
                statsText += $"Borrachera máxima: {stats.currentDrunkenness:F0}%\n";
                statsText += $"Tragos dados: {stats.drinksCount}";
                finalStatsText.text = statsText;
            }
            
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(victoryScreen, true));
            
            PlayUISound(victorySound);
        }
    }
    
    public void ShowDefeatScreen(ChivaGameManager.GameStats stats)
    {
        if (defeatScreen != null)
        {
            defeatScreen.SetActive(true);
            
            // Determinar razón de derrota
            string reason = stats.currentDrunkenness >= 100f ? 
                "¡Demasiado borracho para continuar!" : 
                "¡El viaje ha terminado!";
            
            if (defeatReasonText != null)
                defeatReasonText.text = reason;
            
            if (defeatStatsText != null)
            {
                string statsText = $"Distancia recorrida: {stats.distanceTraveled/1000f:F1} km\n";
                statsText += $"Tiempo de viaje: {FormatTime(stats.playTime)}\n";
                statsText += $"Pasajeros recogidos: {stats.passengersTotal}\n";
                statsText += $"Nivel final de borrachera: {stats.currentDrunkenness:F0}%";
                defeatStatsText.text = statsText;
            }
            
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(defeatScreen, true));
            
            PlayUISound(defeatSound);
        }
    }
    
    public void HideVictoryScreen()
    {
        if (victoryScreen != null)
            victoryScreen.SetActive(false);
    }
    
    public void HideDefeatScreen()
    {
        if (defeatScreen != null)
            defeatScreen.SetActive(false);
    }
    
    // ========== EVENT HANDLERS ==========
    
    void OnGameStart()
    {
        Debug.Log("UI: Game started");
        HideLoadingUI();
        ShowGameUI();
    }
    
    void OnGameEnd(ChivaGameManager.GameEndReason reason)
    {
        Debug.Log($"UI: Game ended - {reason}");
        HideGameUI();
        
        if (gameManager != null)
        {
            var stats = gameManager.GetCurrentStats();
            
            if (reason == ChivaGameManager.GameEndReason.Victory)
                ShowVictoryScreen(stats);
            else
                ShowDefeatScreen(stats);
        }
    }
    
    void OnGamePause(bool paused)
    {
        if (paused)
            ShowPauseMenu();
        else
            HidePauseMenu();
    }
    
    void OnSpeedChanged(float newSpeed)
    {
        // Actualizar UI de velocidad
        float speedKmh = newSpeed * 3.6f;
        
        if (speedText != null)
            speedText.text = speedKmh.ToString(cachedSpeedFormat);
        
        if (speedBar != null)
            speedBar.value = speedKmh;
    }
    
    void OnProgressChanged(float progress)
    {
        // Actualizar barra de progreso
        if (routeProgressBar != null)
            routeProgressBar.value = progress;
        
        if (routeProgressText != null)
            routeProgressText.text = $"{progress * 100f:F0}%";
        
        // Actualizar distancia
        if (chivaController != null)
        {
            float currentKm = chivaController.GetDistanceKm();
            float totalKm = 25f; // 25km total
            
            if (distanceText != null)
                distanceText.text = $"{currentKm:F1} km";
            
            if (remainingDistanceText != null)
                remainingDistanceText.text = $"{totalKm - currentKm:F1} km restantes";
        }
    }
    
    void OnBrakingStateChanged(bool isBraking)
    {
        if (brakingIndicator != null)
            brakingIndicator.SetActive(isBraking);
    }
    
    void OnDrunkennessChanged(float newDrunkenness)
    {
        float drunkPercentage = newDrunkenness / 100f;
        
        // Actualizar barra de borrachera
        if (drunkennessBar != null)
            drunkennessBar.value = drunkPercentage;
        
        if (drunkennessText != null)
            drunkennessText.text = $"{newDrunkenness:F0}%";
        
        // Cambiar color de la barra
        if (drunkennessBarFill != null)
            drunkennessBarFill.color = Color.Lerp(soberColor, drunkColor, drunkPercentage);
        
        // Mostrar advertencia si está muy borracho
        if (drunkWarning != null)
        {
            bool shouldShowWarning = drunkPercentage >= warningThreshold;
            if (shouldShowWarning && !drunkWarning.activeInHierarchy)
            {
                StartDrunkWarningAnimation();
            }
            else if (!shouldShowWarning && drunkWarning.activeInHierarchy)
            {
                StopDrunkWarningAnimation();
            }
        }
    }
    
    void OnPassengerPickup(int count)
    {
        UpdatePassengerCount(passengerManager?.CurrentPassengerCount ?? 0);
        ShowPassengerPickupNotification(count);
    }
    
    void OnElectrolitPickup(float reduction)
    {
        ShowElectrolitNotification();
        PlayUISound(notificationSound);
    }
    
    void OnElectrolitEffect(float amount)
    {
        ShowElectrolitNotification();
    }
    
    void OnCheckpointReached(float distance)
    {
        ShowCheckpointNotification(distance / 1000f);
    }
    
    // ========== UI UPDATE METHODS ==========
    
    public void UpdateLoadingProgress(float progress)
    {
        if (loadingProgressBar != null)
            loadingProgressBar.value = progress;
        
        if (loadingStatusText != null)
        {
            if (progress < 0.5f)
                loadingStatusText.text = "Generando carretera...";
            else if (progress < 0.8f)
                loadingStatusText.text = "Preparando pasajeros...";
            else
                loadingStatusText.text = "¡Casi listo!";
        }
    }
    
    public void UpdateRouteProgress(float progress)
    {
        OnProgressChanged(progress);
    }
    
    public void UpdateDrunkennessBar(float percentage)
    {
        OnDrunkennessChanged(percentage * 100f);
    }
    
    public void UpdatePassengerCount(int count)
    {
        if (passengerCountText != null)
            passengerCountText.text = $"Pasajeros: {count}";
    }
    
    // ========== NOTIFICATION SYSTEM ==========
    
    void ShowPassengerPickupNotification(int count)
    {
        if (passengerPickupText != null)
        {
            passengerPickupText.text = $"+{count} pasajero{(count > 1 ? "s" : "")}!";
            passengerPickupText.gameObject.SetActive(true);
            
            if (notificationCoroutine != null)
                StopCoroutine(notificationCoroutine);
            
            notificationCoroutine = StartCoroutine(HideNotificationAfterDelay(passengerPickupText.gameObject));
        }
        
        PlayUISound(notificationSound);
    }
    
    void ShowElectrolitNotification()
    {
        if (electrolitNotification != null)
        {
            electrolitNotification.SetActive(true);
            
            if (notificationCoroutine != null)
                StopCoroutine(notificationCoroutine);
            
            notificationCoroutine = StartCoroutine(HideNotificationAfterDelay(electrolitNotification));
        }
    }
    
    void ShowCheckpointNotification(float kilometersMark)
    {
        if (checkpointText != null)
        {
            checkpointText.text = $"Kilómetro {kilometersMark:F0}";
            
            if (newCheckpointNotification != null)
            {
                newCheckpointNotification.SetActive(true);
                StartCoroutine(HideNotificationAfterDelay(newCheckpointNotification));
            }
        }
        
        PlayUISound(notificationSound);
    }
    
    private IEnumerator HideNotificationAfterDelay(GameObject notification)
    {
        yield return new WaitForSeconds(notificationDuration);
        
        if (notification != null)
            notification.SetActive(false);
    }
    
    // ========== ANIMATIONS ==========
    
    private IEnumerator AnimateUIElement(GameObject element, bool show)
    {
        if (element == null) yield break;
        
        CanvasGroup canvasGroup = element.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = element.AddComponent<CanvasGroup>();
        
        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;
        
        if (show)
            element.SetActive(true);
        
        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
        
        if (!show)
            element.SetActive(false);
    }
    
    void StartDrunkWarningAnimation()
    {
        if (drunkWarning != null)
        {
            drunkWarning.SetActive(true);
            
            if (drunkWarningCoroutine != null)
                StopCoroutine(drunkWarningCoroutine);
            
            drunkWarningCoroutine = StartCoroutine(DrunkWarningBlinkEffect());
        }
    }
    
    void StopDrunkWarningAnimation()
    {
        if (drunkWarningCoroutine != null)
        {
            StopCoroutine(drunkWarningCoroutine);
            drunkWarningCoroutine = null;
        }
        
        if (drunkWarning != null)
            drunkWarning.SetActive(false);
    }
    
    private IEnumerator DrunkWarningBlinkEffect()
    {
        CanvasGroup warningGroup = drunkWarning.GetComponent<CanvasGroup>();
        if (warningGroup == null)
            warningGroup = drunkWarning.AddComponent<CanvasGroup>();
        
        while (drunkWarning.activeInHierarchy)
        {
            // Fade out
            float alpha = 1f;
            while (alpha > 0f)
            {
                alpha -= Time.unscaledDeltaTime * 3f;
                warningGroup.alpha = alpha;
                yield return null;
            }
            
            // Fade in
            while (alpha < 1f)
            {
                alpha += Time.unscaledDeltaTime * 3f;
                warningGroup.alpha = alpha;
                yield return null;
            }
        }
    }
    
    // ========== SETUP METHODS ==========
    
    void SetupControlsText()
    {
        string controlsInfo = "CONTROLES:\n\n";
        controlsInfo += "A / D - Mover izquierda/derecha\n";
        controlsInfo += "ESPACIO - Frenar\n";
        controlsInfo += "P - Pausar\n\n";
        controlsInfo += "OBJETIVO:\n";
        controlsInfo += "• Llega a los 25 km\n";
        controlsInfo += "• Recoge pasajeros para reducir borrachera\n";
        controlsInfo += "• No dejes que la borrachera llegue a 100%";
        
        controlsText.text = controlsInfo;
    }
    
    void UpdateLoadingTips()
    {
        if (loadingTipsText != null && loadingTips.Length > 0)
        {
            string randomTip = loadingTips[Random.Range(0, loadingTips.Length)];
            loadingTipsText.text = $"Consejo: {randomTip}";
        }
    }
    
    private IEnumerator ShowControlsAtStart()
    {
        yield return new WaitForSeconds(1f);
        
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(true);
            yield return new WaitForSeconds(controlsDisplayTime);
            
            if (animateUIElements)
                StartCoroutine(AnimateUIElement(controlsPanel, false));
            else
                controlsPanel.SetActive(false);
        }
    }
    
    // ========== SETTINGS ==========
    
    void OnVolumeChanged(float volume)
    {
        AudioListener.volume = volume;
    }
    
    void OnFullscreenToggle(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    
    // ========== UTILITY ==========
    
    void PlayUISound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip, uiVolume);
        }
    }
    
    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:D2}:{seconds:D2}";
    }
    
    // ========== DEBUG ==========
    
    [System.Serializable]
    private class LoadingTips
    {
        public string[] tips = new string[]
        {
            "Frena antes de recoger pasajeros para ir más despacio",
            "Más pasajeros = menos borrachera del tío borracho",
            "Los pasajeros electrolit reducen la borrachera instantáneamente",
            "Usa A y D para esquivar y acercarte a los pasajeros",
            "La chiva acelera sola, solo controlas dirección y freno",
            "Cada trago del tío aumenta la borrachera",
            "La borrachera afecta tus controles gradualmente",
            "Son 25 kilómetros hasta la victoria"
        };
    }
}
