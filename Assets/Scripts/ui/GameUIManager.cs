using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

// ============================================
// GAME UI MANAGER - Sistema de UI principal CON FIREBASE RANKING
// ============================================
public class GameUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gameplayPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject playerRegistrationPanel;
    public GameObject rankingPanel;
    public GameObject loginOrRegisterPanel;
    public GameObject preparationPanel;
    
    [Header("Gameplay UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI multiplierText;
    
    [Header("Collectible Counters")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI powerCoinsText;
    
    [Header("Power-Up UI")]
    public GameObject powerUpContainer;
    public PowerUpUIElement speedBoostUI;
    public PowerUpUIElement magnetUI;
    public PowerUpUIElement doublePointsUI;
    public PowerUpUIElement shieldUI;
    
    [Header("Game Over UI")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI newHighScoreText;
    public TextMeshProUGUI finalDistanceText;
    public TextMeshProUGUI totalCoinsText;
    public TextMeshProUGUI gameOverReasonText;
    public TextMeshProUGUI playerRankText;
    public TextMeshProUGUI personalBestText;
    
    [Header("Player Registration UI")]
    public TMP_InputField playerNameInput;
    public TMP_InputField playerEmailInput;
    public TMP_InputField playerPhoneInput;
    public Button registerButton;
    public Button skipRegistrationButton;
    public TextMeshProUGUI registrationErrorText;
    public TextMeshProUGUI welcomePlayerText;
    public Toggle termsAndConditionsToggle;
    
    [Header("Login UI")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginNameInput;
    public Button loginButton;
    public Button goToRegisterButton;
    public TextMeshProUGUI loginErrorText;
    
    [Header("Ranking UI")]
    public Transform rankingContentParent;
    public GameObject rankingEntryPrefab;
    public TextMeshProUGUI currentPlayerRankText;
    public TextMeshProUGUI totalPlayersText;
    public Button showFullRankingButton;
    public Button logoutButton;
    
    [Header("Firebase UI")] // ⭐ NUEVA SECCIÓN FIREBASE
    public TextMeshProUGUI firebaseSyncStatusText;
    public GameObject firebaseSyncIndicator;
    public Image firebaseConnectionIndicator;
    public Color connectedColor = Color.green;
    public Color disconnectedColor = Color.red;
    public Color syncingColor = Color.yellow;
    public Color connectingColor = Color.yellow; // ✅ AMARILLO (que sí existe)    private bool isWaitingForFirebaseConnection = false;
    private bool isWaitingForFirebaseConnection = false;
    private Coroutine firebaseConnectionWaitCoroutine;
    
    [Header("Settings")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle vibrationToggle;
    public Button controlsButton;
    
    [Header("Animation Settings")]
    public float uiAnimationSpeed = 0.5f;
    public AnimationCurve uiCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Effects")]
    public ParticleSystem scoreParticles;
    public AudioClip uiClickSound;
    public AudioClip gameOverSound;
    public AudioClip newHighScoreSound;
    public AudioClip fatalHitSound;
    public AudioClip newPersonalBestSound;
    public AudioClip rankUpSound;
    
    [Header("Fatal Obstacle Effects")]
    public GameObject fatalHitEffect;
    public float screenShakeIntensity = 0.5f;
    public float screenShakeDuration = 0.3f;
    public Image screenFlashOverlay;
    
    // Referencias del sistema
    private CollectibleManager collectibleManager;
    private ImprovedSplineFollower player;
    private SplineMathGenerator splineGenerator;
    private AudioSource audioSource;
    private Camera mainCamera;
    private FirebaseRankingManager rankingManager; // ⭐ CAMBIO: FirebaseRankingManager
    
    // Estado del juego
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Settings,
        PlayerRegistration,
        Ranking,
        LoginOrRegister,
        Preparation 
    }
    
    // Razones de game over
    public enum GameOverReason
    {
        Unknown,
        FatalObstacle,
        OutOfBounds,
        TimeUp,
        PlayerChoice
    }
    
    private GameState currentState = GameState.MainMenu;
    private bool isTransitioning = false;
    private GameOverReason lastGameOverReason = GameOverReason.Unknown;
    
    // Variables de UI
    private int displayScore = 0;
    private float displayDistance = 0f;
    private Coroutine scoreAnimationCoroutine;
    private Vector3 originalCameraPosition;
    
    // Variables para ranking
    private float gameStartTime;
    private int previousRank = -1;
    private bool isPersonalBest = false;
    
    // ⭐ NUEVAS VARIABLES FIREBASE
    private bool isFirebaseSyncing = false;
    private Coroutine syncIndicatorCoroutine;
    
    void Start()
    {
        Debug.Log("=== GAME UI MANAGER STARTING ===");
        
        // Encontrar referencias
        collectibleManager = FindObjectOfType<CollectibleManager>();
        player = FindObjectOfType<ImprovedSplineFollower>();
        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        audioSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;
        rankingManager = FirebaseRankingManager.Instance as FirebaseRankingManager;
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // ⭐ VERIFICAR FirebaseRankingManager
        if (rankingManager == null)
        {
            Debug.LogError("❌ FirebaseRankingManager not found! Please add it to the scene.");
        }
        
        // Guardar posición original de la cámara
        if (mainCamera != null)
            originalCameraPosition = mainCamera.transform.localPosition;
        
        // Configurar eventos
        SetupEventListeners();
        
        // Configurar UI inicial
        SetupInitialUI();
        
        // ⭐ NUEVA INICIALIZACIÓN DE FIREBASE
        InitializeFirebaseConnection();
        
        Debug.Log("GameUIManager initialized successfully!");
    }
    
    void Update()
    {
        // Actualizar UI de gameplay si estamos jugando
        if (currentState == GameState.Playing)
        {
            UpdateGameplayUI();
        }
        
        // Manejar input
        HandleInput();
    }
    
    void SetupEventListeners()
    {
        if (collectibleManager != null)
        {
            // Suscribirse a eventos del CollectibleManager
            collectibleManager.OnScoreChanged.AddListener(OnScoreChanged);
            collectibleManager.OnPowerUpActivated.AddListener(OnPowerUpActivated);
            collectibleManager.OnPowerUpExpired.AddListener(OnPowerUpExpired);
        }
        
        // ⭐ EVENTOS DEL FIREBASE RANKING MANAGER
        if (rankingManager != null)
        {
            rankingManager.OnPlayerRegistered += OnPlayerRegistered;
            rankingManager.OnPlayerRankChanged += OnPlayerRankChanged;
            rankingManager.OnRankingUpdated += OnRankingUpdated;
            
            // ⭐ NUEVOS EVENTOS FIREBASE
            rankingManager.OnFirebaseSyncCompleted += OnFirebaseSyncCompleted;
            rankingManager.OnFirebaseSyncError += OnFirebaseSyncError;
        }
        
        // Eventos de UI
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterButtonPressed);
        
        if (skipRegistrationButton != null)
            skipRegistrationButton.onClick.AddListener(OnSkipRegistrationPressed);
        
        if (showFullRankingButton != null)
            showFullRankingButton.onClick.AddListener(ShowRanking);
        
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutButtonPressed);
        
        // Eventos de login
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonPressed);
        
        if (goToRegisterButton != null)
            goToRegisterButton.onClick.AddListener(ShowPlayerRegistration);
            // ⭐ NUEVO: Suscribirse a eventos de conexión Firebase
        if (rankingManager?.firebaseSync != null)
        {
            rankingManager.firebaseSync.OnConnectionTested += OnFirebaseConnectionTested;
            rankingManager.firebaseSync.OnFirebaseReady += OnFirebaseReady;
        }
    }
    
    // ⭐ NUEVO EVENT HANDLER: Conexión Firebase testada
    void OnFirebaseConnectionTested(bool isConnected)
    {
        Debug.Log($"🔥 Firebase connection tested: {isConnected}");
        
        if (isWaitingForFirebaseConnection)
        {
            isWaitingForFirebaseConnection = false;
            ShowMainMenuAfterFirebaseCheck();
        }
    }

    // ⭐ NUEVO EVENT HANDLER: Firebase listo
    void OnFirebaseReady()
    {
        Debug.Log("🔥 Firebase is ready!");
        
        if (isWaitingForFirebaseConnection)
        {
            isWaitingForFirebaseConnection = false;
            ShowMainMenuAfterFirebaseCheck();
        }
    }

    void SetupInitialUI()
    {
        // Configurar sliders de volumen
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        if (vibrationToggle != null)
        {
            vibrationToggle.isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
        }
        
        // Configurar screen flash overlay
        if (screenFlashOverlay != null)
        {
            screenFlashOverlay.color = new Color(1, 0, 0, 0);
            screenFlashOverlay.gameObject.SetActive(true);
        }
        
        // Configurar UI de registro
        SetupRegistrationUI();
        
        // Configurar high score inicial
        UpdateHighScoreDisplay();
        
        // Actualizar UI del jugador actual
        UpdateCurrentPlayerUI();
    }
    
    // ⭐ NUEVO MÉTODO: Configurar UI de Firebase
    void SetupFirebaseUI()
    {
        // Configurar indicador de sincronización
        if (firebaseSyncIndicator != null)
            firebaseSyncIndicator.SetActive(false);
        
        // Configurar indicador de conexión
        UpdateFirebaseConnectionStatus();
        
        // Configurar texto de estado
        if (firebaseSyncStatusText != null)
        {
            if (rankingManager != null && rankingManager.IsFirebaseEnabled())
            {
                firebaseSyncStatusText.text = "Firebase: Ready";
            }
            else
            {
                firebaseSyncStatusText.text = "Firebase: Disabled";
            }
        }
    }
    
    void InitializeFirebaseConnection()
{
    if (rankingManager?.firebaseSync == null)
    {
        Debug.LogWarning("⚠️ Firebase not available - showing main menu directly");
        ShowMainMenuAfterFirebaseCheck();
        return;
    }
    
    // Configurar Firebase UI inicial
    SetupFirebaseUI();
    
    // Verificar si ya está conectado
    if (rankingManager.firebaseSync.IsConnected())
    {
        Debug.Log("✅ Firebase already connected!");
        ShowMainMenuAfterFirebaseCheck();
        return;
    }
    
    // Si ya está intentando conectar, esperar
    if (rankingManager.firebaseSync.IsConnecting())
    {
        Debug.Log("🔄 Firebase connection in progress, waiting...");
        StartWaitingForConnection();
        return;
    }
    
    // Iniciar conexión
    Debug.Log("🔥 Starting Firebase connection...");
    StartWaitingForConnection();
    rankingManager.firebaseSync.TestConnection();
}

// 4. AGREGAR ESTE NUEVO MÉTODO
// ⭐ NUEVO MÉTODO: Esperar conexión Firebase
void StartWaitingForConnection()
{
    isWaitingForFirebaseConnection = true;
    
    // Mostrar estado de conexión
    if (firebaseSyncStatusText != null)
        firebaseSyncStatusText.text = "Firebase: Connecting...";
    
    if (firebaseConnectionIndicator != null)
        firebaseConnectionIndicator.color = connectingColor;
    
    // Iniciar coroutine de espera
    if (firebaseConnectionWaitCoroutine != null)
        StopCoroutine(firebaseConnectionWaitCoroutine);
    
    firebaseConnectionWaitCoroutine = StartCoroutine(WaitForFirebaseConnection());
}

// 5. AGREGAR ESTE NUEVO MÉTODO
// ⭐ NUEVO COROUTINE: Esperar conexión Firebase
IEnumerator WaitForFirebaseConnection()
{
    float maxWaitTime = 10f; // Máximo 10 segundos
    float elapsedTime = 0f;
    
    while (elapsedTime < maxWaitTime && isWaitingForFirebaseConnection)
    {
        if (rankingManager?.firebaseSync != null)
        {
            if (rankingManager.firebaseSync.IsConnected())
            {
                Debug.Log("✅ Firebase connected successfully!");
                isWaitingForFirebaseConnection = false;
                ShowMainMenuAfterFirebaseCheck();
                yield break;
            }
            
            if (!rankingManager.firebaseSync.IsConnecting())
            {
                Debug.LogWarning("⚠️ Firebase connection failed or stopped");
                isWaitingForFirebaseConnection = false;
                ShowMainMenuAfterFirebaseCheck();
                yield break;
            }
        }
        
        elapsedTime += Time.unscaledDeltaTime;
        yield return null;
    }
    
    // Timeout
    if (isWaitingForFirebaseConnection)
    {
        Debug.LogWarning("⏰ Firebase connection timeout - proceeding anyway");
        isWaitingForFirebaseConnection = false;
        ShowMainMenuAfterFirebaseCheck();
    }
}

// 6. AGREGAR ESTE NUEVO MÉTODO
// ⭐ NUEVO MÉTODO: Mostrar menú principal después de verificar Firebase
void ShowMainMenuAfterFirebaseCheck()
{
    // Actualizar UI de Firebase
    UpdateFirebaseConnectionStatus();
    
    // Actualizar UI del jugador actual
    UpdateCurrentPlayerUI();
    
    // Mostrar menú principal
    ShowMainMenu();
}
    // ⭐ NUEVO MÉTODO: Actualizar estado de conexión Firebase
    void UpdateFirebaseConnectionStatus()
    {
        if (firebaseConnectionIndicator == null) return;
        
        if (isWaitingForFirebaseConnection)
        {
            firebaseConnectionIndicator.color = connectingColor;
            if (firebaseSyncStatusText != null)
                firebaseSyncStatusText.text = "Firebase: Connecting...";
            return;
        }
        
        if (rankingManager?.firebaseSync != null)
        {
            if (rankingManager.firebaseSync.IsConnected())
            {
                firebaseConnectionIndicator.color = isFirebaseSyncing ? syncingColor : connectedColor;
                if (firebaseSyncStatusText != null && !isFirebaseSyncing)
                    firebaseSyncStatusText.text = "Firebase: Connected";
            }
            else
            {
                firebaseConnectionIndicator.color = disconnectedColor;
                if (firebaseSyncStatusText != null)
                    firebaseSyncStatusText.text = "Firebase: Disconnected";
            }
        }
        else
        {
            firebaseConnectionIndicator.color = disconnectedColor;
            if (firebaseSyncStatusText != null)
                firebaseSyncStatusText.text = "Firebase: Disabled";
        }
    }
    // ⭐ MÉTODO: Configurar UI de registro
    void SetupRegistrationUI()
    {
        if (registrationErrorText != null)
            registrationErrorText.gameObject.SetActive(false);
        
        if (loginErrorText != null)
            loginErrorText.gameObject.SetActive(false);
        
        // Validación en tiempo real
        if (playerNameInput != null)
            playerNameInput.onValueChanged.AddListener(ValidateRegistrationFields);
        
        if (playerEmailInput != null)
            playerEmailInput.onValueChanged.AddListener(ValidateRegistrationFields);
        
        if (playerPhoneInput != null)
            playerPhoneInput.onValueChanged.AddListener(ValidateRegistrationFields);

        if (termsAndConditionsToggle != null)
        {
            termsAndConditionsToggle.isOn = false;
            termsAndConditionsToggle.onValueChanged.AddListener(ValidateRegistrationFields);
        }
        
        ValidateRegistrationFields("");
    }
    
    // ⭐ MÉTODO: Validar campos de registro
    void ValidateRegistrationFields(string value)
    {
        if (registerButton == null) return;
        
        bool isValid = !string.IsNullOrWhiteSpace(playerNameInput?.text) &&
                   !string.IsNullOrWhiteSpace(playerEmailInput?.text) &&
                   !string.IsNullOrWhiteSpace(playerPhoneInput?.text) &&
                   (termsAndConditionsToggle?.isOn ?? false);
        
        registerButton.interactable = isValid;
    }
    
    void ValidateRegistrationFields(bool value)
    {
        ValidateRegistrationFields("");
    }
    // ⭐ MÉTODO: Actualizar UI del jugador actual
    void UpdateCurrentPlayerUI()
    {
        if (rankingManager == null) return;
        
        var currentPlayer = rankingManager.GetCurrentPlayer();
        
        if (currentPlayer != null)
        {
            // Mostrar nombre del jugador
            if (welcomePlayerText != null)
            {
                welcomePlayerText.text = $"Welcome back, {currentPlayer.name}!";
                welcomePlayerText.gameObject.SetActive(true);
            }
            
            // Mostrar ranking actual
            UpdatePlayerRankDisplay();
            
            // Mostrar/ocultar botones según corresponda
            if (logoutButton != null)
                logoutButton.gameObject.SetActive(true);
        }
        else
        {
            // No hay jugador registrado
            if (welcomePlayerText != null)
                welcomePlayerText.gameObject.SetActive(false);
            
            if (logoutButton != null)
                logoutButton.gameObject.SetActive(false);
        }
    }
    
    // ⭐ MÉTODO: Actualizar display del ranking del jugador
    void UpdatePlayerRankDisplay()
    {
        if (rankingManager == null) return;
        
        int currentRank = rankingManager.GetCurrentPlayerRank();
        int totalPlayers = rankingManager.GetTotalPlayers();
        
        if (currentPlayerRankText != null)
        {
            if (currentRank > 0)
                currentPlayerRankText.text = $"Tú posición: #{currentRank}";
            else
                currentPlayerRankText.text = "Sin posición aun";
        }
        
        if (totalPlayersText != null)
        {
            totalPlayersText.text = $"Jugadores: {totalPlayers}";
        }
    }
    
    // ============================================
    // MANEJO DE ESTADOS
    // ============================================
    
    public void ShowMainMenu()
    {
        if (isTransitioning) return;
        
        StartCoroutine(ChangeState(GameState.MainMenu));
        Time.timeScale = 0f;
        
        AudioManager.Instance?.PlayMenuMusic();

        // Actualizar UI del jugador
        UpdateCurrentPlayerUI();
    }
    
    public void StartGame()
    {
        if (isTransitioning) return;  

    // Verificación de jugador registrado (existente)  
    if (rankingManager != null && !rankingManager.HasCurrentPlayer())  
    {  
        ShowLoginOrRegister();  
        return;  
    }  

    PlayUISound(uiClickSound);  
    StartCoroutine(ChangeState(GameState.Preparation)); // Cambio clave  
    StartCoroutine(StartCountdown()); // Inicia la cuenta regresiva  
    }

    IEnumerator StartCountdown()  
{  
    TextMeshProUGUI countdownText = preparationPanel.GetComponentInChildren<TextMeshProUGUI>();  
    int count = 3; // 3, 2, 1, ¡Ruummm!  

    while (count > 0)  
    {  
        if (countdownText != null)  
            countdownText.text = count.ToString();  

        PlayUISound(uiClickSound); // Opcional: sonido por número  
        yield return new WaitForSecondsRealtime(1f); // Ignora Time.timeScale  
        count--;  
    }  

    // "¡GO!"  
    if (countdownText != null)  
        countdownText.text = "¡Ruummm!";  

    PlayUISound(newHighScoreSound); // Opcional: sonido de inicio  
    yield return new WaitForSecondsRealtime(0.5f);  

    // Cambiar al estado Playing  
    StartCoroutine(ChangeState(GameState.Playing));  
    Time.timeScale = 1f;  
    AudioManager.Instance?.PlayGameplayMusic();  

    // Resetear estadísticas (código existente)  
    if (collectibleManager != null)  
        collectibleManager.ResetSession();  
}

    
    // ⭐ MÉTODO: Mostrar panel de login o registro
    public void ShowLoginOrRegister()
    {
        if (isTransitioning) return;
        
        PlayUISound(uiClickSound);
        StartCoroutine(ChangeState(GameState.LoginOrRegister));
    }
    
    // ⭐ MÉTODO: Mostrar panel de registro
    public void ShowPlayerRegistration()
    {
        if (isTransitioning) return;
        
        PlayUISound(uiClickSound);
        StartCoroutine(ChangeState(GameState.PlayerRegistration));
        
        // Limpiar campos
        if (playerNameInput != null) playerNameInput.text = "";
        if (playerEmailInput != null) playerEmailInput.text = "";
        if (playerPhoneInput != null) playerPhoneInput.text = "";
        if (registrationErrorText != null) registrationErrorText.gameObject.SetActive(false);
        if (termsAndConditionsToggle != null) 
        termsAndConditionsToggle.isOn = false;
        
        ValidateRegistrationFields("");
    }
    
    // ⭐ MÉTODO: Mostrar ranking
    public void ShowRanking()
    {
        if (isTransitioning) return;
        
        PlayUISound(uiClickSound);
        StartCoroutine(ChangeState(GameState.Ranking));
        
        // Actualizar contenido del ranking
        UpdateRankingDisplay();
    }
    
    public void PauseGame()
    {
        if (currentState != GameState.Playing || isTransitioning) return;
        
        PlayUISound(uiClickSound);
        StartCoroutine(ChangeState(GameState.Paused));
        Time.timeScale = 0f;
    }
    
    public void ResumeGame()
    {
        if (currentState != GameState.Paused || isTransitioning) return;
        
        PlayUISound(uiClickSound);
        StartCoroutine(ChangeState(GameState.Playing));
        Time.timeScale = 1f;
    }
    
    public void ShowSettings()
    {
        if (isTransitioning) return;
        
        PlayUISound(uiClickSound);
        StartCoroutine(ChangeState(GameState.Settings));
    }
    
    public void RestartGame()
    {
        PlayUISound(uiClickSound);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    // ============================================
    // EVENT HANDLERS DE FIREBASE
    // ============================================
    
    // ⭐ NUEVO EVENT HANDLER: Firebase sync completado
    void OnFirebaseSyncCompleted(bool success)
    {
        Debug.Log($"🔥 Firebase sync completed: {success}");
        
        isFirebaseSyncing = false;
        
        // Actualizar UI
        UpdateFirebaseConnectionStatus();
        
        if (firebaseSyncStatusText != null)
        {
            firebaseSyncStatusText.text = success ? "Firebase: Synced" : "Firebase: Sync Failed";
        }
        
        // Ocultar indicador de sincronización
        if (firebaseSyncIndicator != null)
            firebaseSyncIndicator.SetActive(false);
        
        // Detener animación del indicador
        if (syncIndicatorCoroutine != null)
        {
            StopCoroutine(syncIndicatorCoroutine);
            syncIndicatorCoroutine = null;
        }
    }
    
    // ⭐ NUEVO EVENT HANDLER: Error de Firebase
    void OnFirebaseSyncError(string error)
    {
        Debug.LogError($"🔥 Firebase sync error: {error}");
        
        isFirebaseSyncing = false;
        
        // Actualizar UI
        UpdateFirebaseConnectionStatus();
        
        if (firebaseSyncStatusText != null)
        {
            firebaseSyncStatusText.text = "Firebase: Error";
        }
        
        // Ocultar indicador de sincronización
        if (firebaseSyncIndicator != null)
            firebaseSyncIndicator.SetActive(false);
        
        // Detener animación del indicador
        if (syncIndicatorCoroutine != null)
        {
            StopCoroutine(syncIndicatorCoroutine);
            syncIndicatorCoroutine = null;
        }
        
        // Opcional: Mostrar mensaje de error al usuario
        // Por ahora solo logueamos, después podemos agregar un popup
    }
    
    // ⭐ MÉTODO: Mostrar indicador de sincronización
    void ShowFirebaseSyncIndicator()
    {
        isFirebaseSyncing = true;
        UpdateFirebaseConnectionStatus();
        
        if (firebaseSyncStatusText != null)
        {
            firebaseSyncStatusText.text = "Firebase: Syncing...";
        }
        
        if (firebaseSyncIndicator != null)
        {
            firebaseSyncIndicator.SetActive(true);
            
            // Iniciar animación de rotación
            if (syncIndicatorCoroutine != null)
                StopCoroutine(syncIndicatorCoroutine);
            
            syncIndicatorCoroutine = StartCoroutine(AnimateSyncIndicator());
        }
    }
    
    // ⭐ COROUTINE: Animar indicador de sincronización
    IEnumerator AnimateSyncIndicator()
    {
        if (firebaseSyncIndicator == null) yield break;
        
        while (isFirebaseSyncing)
        {
            firebaseSyncIndicator.transform.Rotate(0, 0, -90f * Time.unscaledDeltaTime);
            yield return null;
        }
    }
    
    // ============================================
    // EVENT HANDLERS DE BOTONES DEL RANKING
    // ============================================
    
    // ⭐ BOTÓN DE LOGIN
    void OnLoginButtonPressed()
    {
        if (rankingManager == null) return;
        
        // ⭐ VERIFICAR CONEXIÓN FIREBASE ANTES DE INTENTAR LOGIN
        if (rankingManager.firebaseSync != null && !rankingManager.firebaseSync.IsConnected())
        {
            ShowLoginError("Firebase not connected. Please wait and try again.");
            return;
        }
        
        string email = loginEmailInput?.text?.Trim() ?? "";
        string name = loginNameInput?.text?.Trim() ?? "";
        
        if (string.IsNullOrEmpty(email))
        {
            ShowLoginError("Please enter your email");
            return;
        }
        
        // ⭐ MOSTRAR INDICADOR DE FIREBASE MIENTRAS BUSCA
        ShowFirebaseSyncIndicator();
        
        // ⭐ NUEVO: Buscar primero localmente, luego en Firebase
        StartCoroutine(LoginPlayerCoroutine(email, name));
    }
    
    IEnumerator LoginPlayerCoroutine(string email, string name)
{
    Debug.Log($"🔍 Searching for player with email: {email}");
    
    // 1. Buscar primero en cache local
    PlayerData existingPlayer = rankingManager.GetPlayerByEmail(email);
    
    if (existingPlayer != null)
    {
        Debug.Log($"✅ Player found in local cache: {existingPlayer.name}");
        CompleteLogin(existingPlayer, name);
        yield break;
    }
    
    // 2. Si no está en cache local, buscar en Firebase
    Debug.Log("🔍 Player not found locally, searching in Firebase...");
    
    bool searchCompleted = false;
    bool searchSuccess = false;
    PlayerData firebasePlayer = null;
    
    // Configurar listener temporal para el resultado de la búsqueda
    System.Action<FirebasePlayerData> onPlayerFound = (fbPlayer) =>
    {
        if (fbPlayer != null && fbPlayer.email.Equals(email, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"✅ Player found in Firebase: {fbPlayer.name}");
            firebasePlayer = ConvertFromFirebaseData(fbPlayer);
            searchSuccess = true;
        }
        searchCompleted = true;
    };
    
    System.Action<string> onSearchError = (error) =>
    {
        Debug.LogWarning($"⚠️ Firebase search error: {error}");
        searchCompleted = true;
        searchSuccess = false;
    };
    
    // Suscribirse temporalmente a eventos
    rankingManager.firebaseSync.OnPlayerSaved += onPlayerFound;
    rankingManager.firebaseSync.OnError += onSearchError;
    
    // Realizar búsqueda en Firebase
    rankingManager.firebaseSync.LoadPlayerByEmail(email);
    
    // Esperar resultado (máximo 10 segundos)
    float timeoutTime = 10f;
    float elapsedTime = 0f;
    
    while (!searchCompleted && elapsedTime < timeoutTime)
    {
        elapsedTime += Time.unscaledDeltaTime;
        yield return null;
    }
    
    // Desuscribirse de eventos temporales
    rankingManager.firebaseSync.OnPlayerSaved -= onPlayerFound;
    rankingManager.firebaseSync.OnError -= onSearchError;
    
    // Procesar resultado
    if (searchSuccess && firebasePlayer != null)
    {
        // Agregar al cache local para futuras consultas
        rankingManager.AddPlayerToCache(firebasePlayer);
        CompleteLogin(firebasePlayer, name);
    }
    else if (elapsedTime >= timeoutTime)
    {
        OnFirebaseSyncCompleted(false);
        ShowLoginError("Search timeout. Please try again.");
    }
    else
    {
        OnFirebaseSyncCompleted(false);
        ShowLoginError("No encontramos este e-mail, para jugar primero te debes registrar.");
    }
}

// ⭐ NUEVO MÉTODO: Completar proceso de login
void CompleteLogin(PlayerData player, string nameVerification)
{
    // Verificación simple por nombre (opcional)
    if (!string.IsNullOrEmpty(nameVerification) && 
        !player.name.Equals(nameVerification, System.StringComparison.OrdinalIgnoreCase))
    {
        OnFirebaseSyncCompleted(false);
        ShowLoginError("Name doesn't match. Please verify your information.");
        return;
    }
    
    // Login exitoso - establecer como jugador actual
    if (rankingManager.LoginPlayer(player.id))
    {
        PlayUISound(uiClickSound);
        if (loginErrorText != null)
            loginErrorText.gameObject.SetActive(false);
        
        // Limpiar campos
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginNameInput != null) loginNameInput.text = "";
        
        OnFirebaseSyncCompleted(true);
        
        Debug.Log($"🎉 Login successful for: {player.name}");
    }
    else
    {
        OnFirebaseSyncCompleted(false);
        ShowLoginError("Login failed. Please try again.");
    }
}

// ⭐ NUEVO MÉTODO: Convertir FirebasePlayerData a PlayerData
PlayerData ConvertFromFirebaseData(FirebasePlayerData fbData)
{
    PlayerData playerData = new PlayerData(fbData.name, fbData.email, fbData.phone);
    playerData.id = fbData.id;
    playerData.bestScore = fbData.bestScore;
    playerData.bestDistance = fbData.bestDistance;
    playerData.totalGames = fbData.totalGames;
    playerData.totalCoins = fbData.totalCoins;
    playerData.totalGems = fbData.totalGems;
    playerData.firstPlayDate = fbData.firstPlayDate;
    playerData.lastPlayDate = fbData.lastPlayDate;
    
    if (fbData.scores != null)
    {
        playerData.scores = new List<PlayerScore>(fbData.scores);
    }
    
    return playerData;
}
    // ⭐ MOSTRAR ERROR DE LOGIN
    void ShowLoginError(string message)
    {
        if (loginErrorText != null)
        {
            loginErrorText.text = message;
            loginErrorText.gameObject.SetActive(true);
            
            // Ocultar mensaje después de 3 segundos
            StartCoroutine(HideLoginError());
        }
    }
    
    IEnumerator HideLoginError()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (loginErrorText != null)
            loginErrorText.gameObject.SetActive(false);
    }
    
    // ⭐ BOTÓN DE REGISTRO
void OnRegisterButtonPressed()
{
    if (rankingManager == null) return;
    
    // Verificar conexión Firebase
    if (rankingManager.firebaseSync != null && !rankingManager.firebaseSync.IsConnected())
    {
        ShowRegistrationError("Firebase not connected. Please wait and try again.");
        return;
    }
    
    string name = playerNameInput?.text?.Trim() ?? "";
    string email = playerEmailInput?.text?.Trim() ?? "";
    string phone = playerPhoneInput?.text?.Trim() ?? "";
    
    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone))
    {
        ShowRegistrationError("Please fill all fields");
        return;
    }
    
    ShowFirebaseSyncIndicator();
    
    // Si el email ya existe, hacer login automático e iniciar partida
    if (rankingManager.IsEmailRegistered(email))
    {
        // Buscar al jugador existente
        PlayerData existingPlayer = rankingManager.GetPlayerByEmail(email);
        
        if (existingPlayer != null)
        {
            Debug.Log($"✅ Email exists, logging in as: {existingPlayer.name}");
            
            // Hacer login automático
            if (rankingManager.LoginPlayer(existingPlayer.id))
            {
                // Limpiar mensajes de error
                if (registrationErrorText != null)
                    registrationErrorText.gameObject.SetActive(false);
                
                // Mostrar mensaje positivo
                ShowRegistrationError($"Welcome back {existingPlayer.name}! Starting game...", isError: false);
                
                // Iniciar partida después de breve delay
                StartCoroutine(StartGameAfterLogin(1.5f));
                return;
            }
        }
    }
    
    // Si el email no existe, proceder con registro normal
    bool success = rankingManager.RegisterPlayer(name, email, phone);
    
    if (success)
    {
        PlayUISound(uiClickSound);
        if (registrationErrorText != null)
            registrationErrorText.gameObject.SetActive(false);
    }
    else
    {
        OnFirebaseSyncCompleted(false);
        ShowRegistrationError("Registration failed. Please check your data.");
    }
}
IEnumerator StartGameAfterLogin(float delay)
{
    yield return new WaitForSecondsRealtime(delay);
    
    // Limpiar UI
    if (playerNameInput != null) playerNameInput.text = "";
    if (playerEmailInput != null) playerEmailInput.text = "";
    if (playerPhoneInput != null) playerPhoneInput.text = "";
    
    // Iniciar partida
    StartGame();
}
    
    // ⭐ CAMBIO AUTOMÁTICO A LOGIN CUANDO EMAIL EXISTE
    IEnumerator DelayedSwitchToLogin()
    {
        yield return new WaitForSecondsRealtime(2f);
        ShowLoginOrRegister();
    }
    
    // ⭐ MOSTRAR ERROR DE REGISTRO
  void ShowRegistrationError(string message, bool isError = true)
{
    if (registrationErrorText != null)
    {
        registrationErrorText.text = message;
        registrationErrorText.color = isError ? Color.red : Color.green;
        registrationErrorText.gameObject.SetActive(true);
        
        StartCoroutine(HideRegistrationError());
    }
}
    
    IEnumerator HideRegistrationError()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (registrationErrorText != null)
            registrationErrorText.gameObject.SetActive(false);
    }
    
    // ⭐ SALTAR REGISTRO
    void OnSkipRegistrationPressed()
    {
        PlayUISound(uiClickSound);
        ShowMainMenu();
    }
    
    // ⭐ LOGOUT CON RECARGA COMPLETA
    void OnLogoutButtonPressed()
    {
        if (rankingManager == null) return;
        
        PlayUISound(uiClickSound);
        rankingManager.LogoutCurrentPlayer();
        
        // Recargar escena completamente
        Debug.Log("🔄 Logging out - Reloading scene for complete reset");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    // ============================================
    // GAME OVER Y SINCRONIZACIÓN
    // ============================================
    
    public void GameOver()
    {
        GameOver(GameOverReason.Unknown);
    }
    
    public void GameOver(GameOverReason reason)
    {
        if (currentState == GameState.GameOver || isTransitioning) return;
        
        lastGameOverReason = reason;
        
        // ⭐ GUARDAR PUNTUACIÓN CON FIREBASE SYNC
        SaveScoreToRanking();

        AudioManager.Instance?.PlayGameOverMusic();
        
        if (reason == GameOverReason.FatalObstacle)
        {
            StartCoroutine(HandleFatalObstacleGameOver());
        }
        else
        {
            PlayUISound(gameOverSound);
            StartCoroutine(ChangeState(GameState.GameOver));
            Time.timeScale = 0f;
            UpdateGameOverUI();
        }
    }
    
    // ⭐ MÉTODO: Guardar puntuación con sincronización Firebase
    void SaveScoreToRanking()
    {
        if (rankingManager == null || !rankingManager.HasCurrentPlayer()) return;
        if (collectibleManager == null) return;
        
        // ⭐ MOSTRAR INDICADOR DE SINCRONIZACIÓN
        ShowFirebaseSyncIndicator();
        
        // Calcular tiempo de juego
        float playTime = Time.time - gameStartTime;
        
        // Obtener estadísticas finales
        int finalScore = collectibleManager.GetTotalScore();
        float finalDistance = splineGenerator != null ? splineGenerator.GetPlayerDistance() : 0f;
        int coins = collectibleManager.coinsCollected;
        int gems = collectibleManager.gemsCollected;
        int powerCoins = collectibleManager.powerCoinsCollected;
        string reason = GetGameOverReasonText(lastGameOverReason);
        
        // Verificar si es record personal
        var currentPlayer = rankingManager.GetCurrentPlayer();
        if (currentPlayer != null && finalScore > currentPlayer.bestScore)
        {
            isPersonalBest = true;
        }
        
        // Guardar en ranking (esto disparará la sincronización con Firebase automáticamente)
        rankingManager.AddScore(finalScore, finalDistance, coins, gems, powerCoins, reason, playTime);
        
        Debug.Log($"🎯 Score saved to ranking: {finalScore} points");
    }
    
    IEnumerator HandleFatalObstacleGameOver()
    {
        Debug.Log("💀 Handling fatal obstacle game over with special effects!");
        
        PlayUISound(fatalHitSound != null ? fatalHitSound : gameOverSound);
        StartCoroutine(ScreenFlashEffect());
        StartCoroutine(ScreenShakeEffect());
        
        if (fatalHitEffect != null && player != null)
        {
            GameObject effect = Instantiate(fatalHitEffect, player.transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        Time.timeScale = 0f;
        yield return StartCoroutine(ChangeState(GameState.GameOver));
        UpdateGameOverUI();
    }
    
    IEnumerator ScreenFlashEffect()
    {
        if (screenFlashOverlay == null) yield break;
        
        screenFlashOverlay.color = new Color(1, 0, 0, 0.7f);
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0.7f, 0f, elapsed / duration);
            screenFlashOverlay.color = new Color(1, 0, 0, alpha);
            yield return null;
        }
        
        screenFlashOverlay.color = new Color(1, 0, 0, 0);
    }
    
    IEnumerator ScreenShakeEffect()
    {
        if (mainCamera == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < screenShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            float intensity = Mathf.Lerp(screenShakeIntensity, 0f, elapsed / screenShakeDuration);
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * intensity;
            randomOffset.z = 0;
            
            mainCamera.transform.localPosition = originalCameraPosition + randomOffset;
            
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalCameraPosition;
    }
    
    void UpdateGameOverUI()
    {
        if (collectibleManager == null) return;
        
        int finalScore = collectibleManager.GetTotalScore();
        int highScore = collectibleManager.GetHighScore();
        bool newRecord = finalScore > highScore;
        
        if (finalScoreText != null)
            finalScoreText.text = finalScore.ToString();
        
        if (newHighScoreText != null)
        {
            newHighScoreText.gameObject.SetActive(newRecord);
            if (newRecord)
            {
                PlayUISound(newHighScoreSound);
                StartCoroutine(AnimateNewHighScore());
            }
        }
        
        if (finalDistanceText != null && splineGenerator != null)
        {
            float finalDistance = splineGenerator.GetPlayerDistance();
            finalDistanceText.text = $"{finalDistance:F0}m";
        }
        
        if (totalCoinsText != null)
            totalCoinsText.text = collectibleManager.coinsCollected.ToString();
        
        if (gameOverReasonText != null)
        {
            gameOverReasonText.text = GetGameOverReasonText(lastGameOverReason);
        }
        
        // ⭐ MOSTRAR INFORMACIÓN DE RANKING
        UpdateGameOverRankingInfo();
        
        collectibleManager.SaveHighScore();
        UpdateHighScoreDisplay();
    }
    
    // ⭐ MÉTODO: Actualizar información de ranking en game over
    void UpdateGameOverRankingInfo()
    {
        if (rankingManager == null || !rankingManager.HasCurrentPlayer()) return;
        
        // Mostrar posición en ranking
        if (playerRankText != null)
        {
            int currentRank = rankingManager.GetCurrentPlayerRank();
            if (currentRank > 0)
            {
                playerRankText.text = $"Tú posición: #{currentRank}";
                
                // Verificar si subió de posición
                if (previousRank > 0 && currentRank < previousRank)
                {
                    int rankImprovement = previousRank - currentRank;
                    playerRankText.text += $" (↑{rankImprovement})";
                    PlayUISound(rankUpSound);
                }
            }
            else
            {
                playerRankText.text = "Sin posición aun";
            }
            
            playerRankText.gameObject.SetActive(true);
        }
        
        // Mostrar si es record personal
        if (personalBestText != null)
        {
            if (isPersonalBest)
            {
                personalBestText.text = "🏆 Superaste tú puntaje";
                personalBestText.gameObject.SetActive(true);
                PlayUISound(newPersonalBestSound);
                StartCoroutine(AnimatePersonalBest());
            }
            else
            {
                personalBestText.gameObject.SetActive(false);
            }
        }
    }
    
    // ⭐ ANIMAR TEXTO DE RECORD PERSONAL
    IEnumerator AnimatePersonalBest()
    {
        if (personalBestText == null) yield break;
        
        Vector3 originalScale = personalBestText.transform.localScale;
        Color originalColor = personalBestText.color;
        
        for (int i = 0; i < 5; i++)
        {
            // Pulso de escala
            personalBestText.transform.localScale = originalScale * 1.1f;
            personalBestText.color = Color.yellow;
            yield return new WaitForSecondsRealtime(0.15f);
            
            personalBestText.transform.localScale = originalScale;
            personalBestText.color = originalColor;
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }
    
    string GetGameOverReasonText(GameOverReason reason)
    {
        switch (reason)
        {
            case GameOverReason.FatalObstacle:
                return "💀 Crushed by Fatal Obstacle!";
            case GameOverReason.OutOfBounds:
                return "🌊 Fell Off the Track!";
            case GameOverReason.TimeUp:
                return "⏰ Time's Up!";
            case GameOverReason.PlayerChoice:
                return "🏳️ You Gave Up!";
            default:
                return "💥 Game Over!";
        }
    }
    
    IEnumerator AnimateNewHighScore()
    {
        if (newHighScoreText == null) yield break;
        
        Vector3 originalScale = newHighScoreText.transform.localScale;
        
        for (int i = 0; i < 3; i++)
        {
            newHighScoreText.transform.localScale = originalScale * 1.2f;
            yield return new WaitForSecondsRealtime(0.1f);
            newHighScoreText.transform.localScale = originalScale;
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }
    
    // ============================================
    // EVENT HANDLERS DEL RANKING (ORIGINALES)
    // ============================================
    
    // ⭐ EVENT HANDLER: Jugador registrado
void OnPlayerRegistered(PlayerData player)
{
    Debug.Log($"🎉 Player registered: {player.name}");
    UpdateCurrentPlayerUI();
    
    // Mostrar mensaje de bienvenida
    ShowRegistrationError($"Welcome {player.name}! Starting game...", isError: false);
    
    // Iniciar juego después de breve delay
    StartCoroutine(StartGameAfterLogin(1.5f));
}
    
    IEnumerator DelayedStartGame()
    {
        yield return new WaitForSecondsRealtime(1f);
        StartGame();
    }
    
    // ⭐ EVENT HANDLER: Ranking del jugador cambió
    void OnPlayerRankChanged(int newRank)
    {
        Debug.Log($"📈 Player rank changed to: #{newRank}");
        // El ranking ya se actualiza en UpdateGameOverRankingInfo()
    }
    
    // ⭐ EVENT HANDLER: Ranking actualizado
    void OnRankingUpdated(List<PlayerData> topPlayers)
    {
        Debug.Log($"🏆 Ranking updated. Top player: {topPlayers[0]?.name}");
        // Si estamos en el panel de ranking, actualizarlo
        if (currentState == GameState.Ranking)
        {
            UpdateRankingDisplay();
        }
    }
    // ============================================
    // CAMBIOS DE ESTADO Y ANIMACIONES
    // ============================================
    
    IEnumerator ChangeState(GameState newState)
    {
        isTransitioning = true;
        
        yield return StartCoroutine(AnimatePanel(GetCurrentPanel(), false));
        
        currentState = newState;
        
        SetActivePanel(newState);
        
        yield return StartCoroutine(AnimatePanel(GetCurrentPanel(), true));
        
        isTransitioning = false;
    }
    
    GameObject GetCurrentPanel()
    {
        switch (currentState)
        {
            case GameState.MainMenu: return mainMenuPanel;
            case GameState.Playing: return gameplayPanel;
            case GameState.Paused: return pausePanel;
            case GameState.GameOver: return gameOverPanel;
            case GameState.Settings: return settingsPanel;
            case GameState.PlayerRegistration: return playerRegistrationPanel;
            case GameState.Ranking: return rankingPanel;
            case GameState.LoginOrRegister: return loginOrRegisterPanel;
            default: return gameplayPanel;
        }
    }
    
    void SetActivePanel(GameState state)
    {
        // Desactivar todos los paneles  
    if (mainMenuPanel != null) mainMenuPanel.SetActive(false);  
    if (gameplayPanel != null) gameplayPanel.SetActive(false);  
    if (pausePanel != null) pausePanel.SetActive(false);  
    if (gameOverPanel != null) gameOverPanel.SetActive(false);  
    if (settingsPanel != null) settingsPanel.SetActive(false);  
    if (playerRegistrationPanel != null) playerRegistrationPanel.SetActive(false);  
    if (rankingPanel != null) rankingPanel.SetActive(false);  
    if (loginOrRegisterPanel != null) loginOrRegisterPanel.SetActive(false);  
    if (preparationPanel != null) preparationPanel.SetActive(false); // Nuevo panel  

    // Activar solo el panel correspondiente al estado actual  
    switch (state)  
    {  
        case GameState.MainMenu:  
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);  
            break;  
        case GameState.Playing:  
            if (gameplayPanel != null) gameplayPanel.SetActive(true);  
            break;  
        case GameState.Paused:  
            if (pausePanel != null) pausePanel.SetActive(true);  
            break;  
        case GameState.GameOver:  
            if (gameOverPanel != null) gameOverPanel.SetActive(true);  
            break;  
        case GameState.Settings:  
            if (settingsPanel != null) settingsPanel.SetActive(true);  
            break;  
        case GameState.PlayerRegistration:  
            if (playerRegistrationPanel != null) playerRegistrationPanel.SetActive(true);  
            break;  
        case GameState.Ranking:  
            if (rankingPanel != null) rankingPanel.SetActive(true);  
            break;  
        case GameState.LoginOrRegister:  
            if (loginOrRegisterPanel != null) loginOrRegisterPanel.SetActive(true);  
            break;  
        case GameState.Preparation:  
            if (preparationPanel != null) preparationPanel.SetActive(true); // Nuevo estado  
            break;  
    }  
    }
    
    IEnumerator AnimatePanel(GameObject panel, bool animateIn)
    {
        if (panel == null) yield break;
        
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();
        
        float startAlpha = animateIn ? 0f : 1f;
        float endAlpha = animateIn ? 1f : 0f;
        
        Vector3 startScale = animateIn ? Vector3.zero : Vector3.one;
        Vector3 endScale = animateIn ? Vector3.one : Vector3.zero;
        
        float elapsed = 0f;
        
        while (elapsed < uiAnimationSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = uiCurve.Evaluate(elapsed / uiAnimationSpeed);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            panel.transform.localScale = Vector3.Lerp(startScale, endScale, progress);
            
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
        panel.transform.localScale = endScale;
    }
    
    // ============================================
    // GAMEPLAY UI
    // ============================================
    
    void UpdateGameplayUI()
    {
        // Actualizar distancia
        if (player != null && distanceText != null)
        {
            float currentDistance = 0f;
            
            if (splineGenerator != null)
            {
                currentDistance = splineGenerator.GetPlayerDistance();
            }
            
            distanceText.text = $"{currentDistance:F0}m";
        }
        
        // Actualizar multiplicador
        if (collectibleManager != null && multiplierText != null)
        {
            float multiplier = collectibleManager.pointMultiplier;
            multiplierText.text = multiplier > 1f ? $"x{multiplier:F1}" : "";
            multiplierText.gameObject.SetActive(multiplier > 1f);
        }
        
        UpdateCollectibleCounters();
        UpdatePowerUpUI();
    }
    
    void UpdateCollectibleCounters()
    {
        if (collectibleManager == null) return;
        
        if (coinsText != null)
            coinsText.text = collectibleManager.coinsCollected.ToString();
        
        if (gemsText != null)
            gemsText.text = collectibleManager.gemsCollected.ToString();
        
        if (powerCoinsText != null)
            powerCoinsText.text = collectibleManager.powerCoinsCollected.ToString();
    }
    
    void ResetGameplayUI()
    {
        displayScore = 0;
        displayDistance = 0f;
        
        if (scoreText != null) scoreText.text = "0";
        if (distanceText != null) distanceText.text = "0m";
        if (multiplierText != null) multiplierText.gameObject.SetActive(false);
        if (preparationPanel != null)  
    {  
        TextMeshProUGUI countdownText = preparationPanel.GetComponentInChildren<TextMeshProUGUI>();  
        if (countdownText != null)  
            countdownText.text = "3"; // Valor inicial  
    }  
        
        UpdateCollectibleCounters();
        ResetPowerUpUI();
    }
    
    // ============================================
    // POWER-UP UI
    // ============================================
    
    void UpdatePowerUpUI()
    {
        if (collectibleManager == null) return;
        
        if (speedBoostUI != null)
        {
            speedBoostUI.UpdatePowerUp(
                collectibleManager.hasActiveSpeedBoost,
                collectibleManager.speedBoostTimeLeft,
                5f
            );
        }
        
        if (magnetUI != null)
        {
            magnetUI.UpdatePowerUp(
                collectibleManager.hasActiveMagnet,
                collectibleManager.magnetTimeLeft,
                10f
            );
        }
        
        if (doublePointsUI != null)
        {
            doublePointsUI.UpdatePowerUp(
                collectibleManager.hasActiveDoublePoints,
                collectibleManager.doublePointsTimeLeft,
                15f
            );
        }
        
        if (shieldUI != null)
        {
            shieldUI.UpdatePowerUp(
                collectibleManager.hasActiveShield,
                collectibleManager.shieldTimeLeft,
                8f
            );
        }
    }
    
    void ResetPowerUpUI()
    {
        if (speedBoostUI != null) speedBoostUI.ResetPowerUp();
        if (magnetUI != null) magnetUI.ResetPowerUp();
        if (doublePointsUI != null) doublePointsUI.ResetPowerUp();
        if (shieldUI != null) shieldUI.ResetPowerUp();
    }
    
    // ============================================
    // EVENT HANDLERS ORIGINALES
    // ============================================
    
    public void OnScoreChanged(int newScore)
    {
        if (scoreAnimationCoroutine != null)
            StopCoroutine(scoreAnimationCoroutine);
        
        scoreAnimationCoroutine = StartCoroutine(AnimateScore(displayScore, newScore));
        
        if (scoreParticles != null && newScore > displayScore)
        {
            scoreParticles.Play();
        }
    }
    
    IEnumerator AnimateScore(int startScore, int endScore)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, progress));
            
            if (scoreText != null)
                scoreText.text = displayScore.ToString();
            
            yield return null;
        }
        
        displayScore = endScore;
        if (scoreText != null)
            scoreText.text = displayScore.ToString();
    }
    
    public void OnPowerUpActivated(CollectibleCollision.PowerUpType powerUpType)
    {
        Debug.Log($"🎉 Power-up activated in UI: {powerUpType}");
        
        switch (powerUpType)
        {
            case CollectibleCollision.PowerUpType.SpeedBoost:
                if (speedBoostUI != null) speedBoostUI.PlayActivationEffect();
                break;
            case CollectibleCollision.PowerUpType.Magnet:
                if (magnetUI != null) magnetUI.PlayActivationEffect();
                break;
            case CollectibleCollision.PowerUpType.DoublePoints:
                if (doublePointsUI != null) doublePointsUI.PlayActivationEffect();
                break;
            case CollectibleCollision.PowerUpType.Shield:
                if (shieldUI != null) shieldUI.PlayActivationEffect();
                break;
        }
    }
    
    public void OnPowerUpExpired(CollectibleCollision.PowerUpType powerUpType)
    {
        Debug.Log($"⏰ Power-up expired in UI: {powerUpType}");
    }
    
    // ============================================
    // RANKING UI
    // ============================================
    
    // ⭐ MÉTODO: Actualizar display del ranking
    void UpdateRankingDisplay()
    {
        if (rankingManager == null || rankingContentParent == null) return;
        
        // Limpiar contenido anterior
        foreach (Transform child in rankingContentParent)
        {
            Destroy(child.gameObject);
        }
        
        // Obtener top jugadores
        var topPlayers = rankingManager.GetTopPlayers(20);
        
        // Crear entradas de ranking
        for (int i = 0; i < topPlayers.Count; i++)
        {
            CreateRankingEntry(topPlayers[i], i + 1);
        }
        
        // Actualizar información del jugador actual
        UpdatePlayerRankDisplay();
    }
    
    // ⭐ MÉTODO: Crear entrada de ranking
    void CreateRankingEntry(PlayerData player, int rank)
    {
        if (rankingEntryPrefab == null || rankingContentParent == null) return;
        
        GameObject entry = Instantiate(rankingEntryPrefab, rankingContentParent);
        RankingEntry rankingEntry = entry.GetComponent<RankingEntry>();
        
        if (rankingEntry != null)
        {
            // Verificar si es el jugador actual
            bool isCurrentPlayer = rankingManager.GetCurrentPlayer()?.id == player.id;
            
            // Configurar la entrada
            rankingEntry.SetupEntry(player, rank, isCurrentPlayer);
            
            // Configurar delay de animación para efecto escalonado
            rankingEntry.SetAnimationDelay(rank * 0.05f);
        }
        else
        {
            // Método manual si no hay componente RankingEntry
            SetupRankingEntryManual(entry, player, rank);
        }
    }
    
    // Método de respaldo para configurar entradas manualmente
    void SetupRankingEntryManual(GameObject entry, PlayerData player, int rank)
    {
        // Buscar componentes del prefab
        var rankText = entry.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
        var nameText = entry.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        var scoreText = entry.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        var gamesText = entry.transform.Find("GamesText")?.GetComponent<TextMeshProUGUI>();
        var background = entry.GetComponent<Image>();
        
        // Configurar textos
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
            
            // Colores especiales para top 3
            switch (rank)
            {
                case 1:
                    rankText.color = Color.yellow; // Oro
                    break;
                case 2:
                    rankText.color = new Color(0.8f, 0.8f, 0.8f); // Plata
                    break;
                case 3:
                    rankText.color = new Color(0.8f, 0.5f, 0.2f); // Bronce
                    break;
                default:
                    rankText.color = Color.white;
                    break;
            }
        }
        
        if (nameText != null)
            nameText.text = player.name;
        
        if (scoreText != null)
            scoreText.text = player.bestScore.ToString();
        
        if (gamesText != null)
            gamesText.text = $"{player.totalGames} games";
        
        // Resaltar jugador actual
        bool isCurrentPlayer = rankingManager.GetCurrentPlayer()?.id == player.id;
        if (isCurrentPlayer && background != null)
        {
            background.color = new Color(1f, 1f, 0f, 0.3f); // Amarillo semitransparente
        }
    }
    // ============================================
    // SETTINGS (SIN CAMBIOS)
    // ============================================
    
    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        AudioManager.Instance?.SetMusicVolume(value);
    }
    
    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        AudioManager.Instance?.SetSFXVolume(value);
    }
    
    public void OnVibrationToggled(bool enabled)
    {
        PlayerPrefs.SetInt("Vibration", enabled ? 1 : 0);
    }
    
    void UpdateHighScoreDisplay()
    {
        if (highScoreText != null && collectibleManager != null)
        {
            highScoreText.text = $"Best: {collectibleManager.GetHighScore()}";
        }
    }
    
    // ============================================
    // UTILIDADES
    // ============================================
    
    void PlayUISound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }
    
    // Métodos públicos para obstáculos letales (sin cambios)
    public void TriggerFatalObstacleGameOver()
    {
        GameOver(GameOverReason.FatalObstacle);
    }
    
    public void TriggerOutOfBoundsGameOver()
    {
        GameOver(GameOverReason.OutOfBounds);
    }
    
    public void TriggerTimeUpGameOver()
    {
        GameOver(GameOverReason.TimeUp);
    }
    
    public GameOverReason GetLastGameOverReason()
    {
        return lastGameOverReason;
    }
    
    // ⭐ MÉTODOS PÚBLICOS PARA RANKING
    public bool HasRegisteredPlayer()
    {
        return rankingManager != null && rankingManager.HasCurrentPlayer();
    }
    
    public string GetCurrentPlayerName()
    {
        if (rankingManager == null) return "";
        var player = rankingManager.GetCurrentPlayer();
        return player?.name ?? "";
    }
    
    public int GetCurrentPlayerRank()
    {
        if (rankingManager == null) return -1;
        return rankingManager.GetCurrentPlayerRank();
    }
    
    // ⭐ MÉTODOS PÚBLICOS PARA FIREBASE
    public bool IsFirebaseEnabled()
    {
        return rankingManager != null && rankingManager.IsFirebaseEnabled();
    }
    
    public void ForceFirebaseSync()
    {
        if (rankingManager != null)
        {
            ShowFirebaseSyncIndicator();
            rankingManager.ForceFirebaseSync();
        }
    }
    
    public void LoadFirebaseRanking()
    {
        if (rankingManager != null)
        {
            ShowFirebaseSyncIndicator();
            rankingManager.LoadFirebaseRanking();
        }
    }
    
    // ============================================
    // MÉTODOS PÚBLICOS PARA BOTONES (ACTUALIZADOS)
    // ============================================
    
    public void OnPlayButtonPressed() => StartGame();
    public void OnPauseButtonPressed() => PauseGame();
    public void OnResumeButtonPressed() => ResumeGame();
    public void OnRestartButtonPressed() => RestartGame();
    public void OnMainMenuButtonPressed() => ShowMainMenu();
    public void OnSettingsButtonPressed() => ShowSettings();
    public void OnRankingButtonPressed() => ShowRanking();
    public void OnRegisterNewPlayerButtonPressed() => ShowPlayerRegistration();
    public void OnLoginOrRegisterButtonPressed() => ShowLoginOrRegister();
    public void OnQuitButtonPressed() => Application.Quit();
    
    // ⭐ NUEVOS BOTONES FIREBASE
    public void OnForceFirebaseSyncButtonPressed() => ForceFirebaseSync();
    public void OnLoadFirebaseRankingButtonPressed() => LoadFirebaseRanking();
    
    // ============================================
    // INPUT HANDLING
    // ============================================
    
    void HandleInput()
    {
        // ESC para pausar/resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
                PauseGame();
            else if (currentState == GameState.Paused)
                ResumeGame();
            else if (currentState == GameState.PlayerRegistration || 
                     currentState == GameState.Ranking || 
                     currentState == GameState.Settings ||
                     currentState == GameState.LoginOrRegister)
                ShowMainMenu();
        }
        
        // R para reiniciar (solo en game over)
        if (Input.GetKeyDown(KeyCode.R) && currentState == GameState.GameOver)
        {
            RestartGame();
        }
        
        // T para mostrar ranking (solo en main menu)
        if (Input.GetKeyDown(KeyCode.T) && currentState == GameState.MainMenu)
        {
            ShowRanking();
        }
        
        // Enter para confirmar registro o login
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (currentState == GameState.PlayerRegistration && registerButton != null && registerButton.interactable)
                OnRegisterButtonPressed();
            else if (currentState == GameState.LoginOrRegister && loginButton != null && loginButton.interactable)
                OnLoginButtonPressed();
        }
        
        // ⭐ NUEVOS SHORTCUTS FIREBASE
        #if UNITY_EDITOR
        // F para forzar game over por obstáculo letal
        if (Input.GetKeyDown(KeyCode.F) && currentState == GameState.Playing)
        {
            TriggerFatalObstacleGameOver();
        }
        
        // G para forzar sincronización Firebase
        if (Input.GetKeyDown(KeyCode.G) && currentState == GameState.MainMenu)
        {
            ForceFirebaseSync();
        }
        
        // L para logout rápido
        if (Input.GetKeyDown(KeyCode.L) && currentState == GameState.MainMenu)
        {
            OnLogoutButtonPressed();
        }
        
        // H para cargar ranking de Firebase
        if (Input.GetKeyDown(KeyCode.H) && currentState == GameState.MainMenu)
        {
            LoadFirebaseRanking();
        }
        #endif
    }
    
    // ============================================
    // CLEANUP
    // ============================================
    
    void OnDestroy()
    {
        // Desuscribirse de eventos para evitar memory leaks
       if (rankingManager != null)
        {
            rankingManager.OnPlayerRegistered -= OnPlayerRegistered;
            rankingManager.OnPlayerRankChanged -= OnPlayerRankChanged;
            rankingManager.OnRankingUpdated -= OnRankingUpdated;
            rankingManager.OnFirebaseSyncCompleted -= OnFirebaseSyncCompleted;
            rankingManager.OnFirebaseSyncError -= OnFirebaseSyncError;
            
            // ⭐ NUEVO CLEANUP: Eventos de conexión Firebase
            if (rankingManager.firebaseSync != null)
            {
                rankingManager.firebaseSync.OnConnectionTested -= OnFirebaseConnectionTested;
                rankingManager.firebaseSync.OnFirebaseReady -= OnFirebaseReady;
            }
        }
        
        // Limpiar listeners de UI
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.RemoveAllListeners();
        
        if (playerNameInput != null)
            playerNameInput.onValueChanged.RemoveAllListeners();
        
        if (playerEmailInput != null)
            playerEmailInput.onValueChanged.RemoveAllListeners();
        
        if (playerPhoneInput != null)
            playerPhoneInput.onValueChanged.RemoveAllListeners();
        
         if (termsAndConditionsToggle != null)
            termsAndConditionsToggle.onValueChanged.RemoveAllListeners();
            if (preparationPanel != null)  
    {  
        var texts = preparationPanel.GetComponentsInChildren<TextMeshProUGUI>();  
        foreach (var text in texts)  
            text.text = ""; // Resetear texto  
    }  
        
        // ⭐ DETENER COROUTINES
        if (syncIndicatorCoroutine != null)
        {
            StopCoroutine(syncIndicatorCoroutine);
            syncIndicatorCoroutine = null;
        }
        if (firebaseConnectionWaitCoroutine != null)
        {
            StopCoroutine(firebaseConnectionWaitCoroutine);
            firebaseConnectionWaitCoroutine = null;
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        // Pausar automáticamente si el juego está activo
        if (pauseStatus && currentState == GameState.Playing)
        {
            PauseGame();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        // Pausar cuando pierde el foco
        if (!hasFocus && currentState == GameState.Playing)
        {
            PauseGame();
        }
    }
    
    // ============================================
    // MÉTODOS DE DEBUGGING EN EDITOR
    // ============================================
    
    #if UNITY_EDITOR
    [ContextMenu("Force Game Over")]
    void ForceGameOver()
    {
        if (currentState == GameState.Playing)
            TriggerFatalObstacleGameOver();
    }
    
    [ContextMenu("Clear Current Player")]
    void ClearCurrentPlayer()
    {
        if (rankingManager != null)
        {
            rankingManager.LogoutCurrentPlayer();
            UpdateCurrentPlayerUI();
        }
    }
    
    [ContextMenu("Show Registration")]
    void DebugShowRegistration()
    {
        ShowPlayerRegistration();
    }
    
    [ContextMenu("Show Login or Register")]
    void DebugShowLoginOrRegister()
    {
        ShowLoginOrRegister();
    }
    
    [ContextMenu("Show Ranking")]
    void DebugShowRanking()
    {
        ShowRanking();
    }
    
    [ContextMenu("Test Firebase Connection")]
    void TestFirebaseConnection()
    {
        if (rankingManager?.firebaseSync != null)
        {
            rankingManager.firebaseSync.TestConnection();
        }
        else
        {
            Debug.LogWarning("FirebaseRankingManager or FirebaseRankingSync not found");
        }
    }
    
    [ContextMenu("Force Firebase Sync")]
    void DebugForceFirebaseSync()
    {
        ForceFirebaseSync();
    }
    
    [ContextMenu("Load Firebase Ranking")]
    void DebugLoadFirebaseRanking()
    {
        LoadFirebaseRanking();
    }
    
    [ContextMenu("Test Firebase Status")]
    void TestFirebaseStatus()
    {
        Debug.Log("=== FIREBASE STATUS ===");
        Debug.Log($"Firebase Enabled: {IsFirebaseEnabled()}");
        Debug.Log($"Has Current Player: {HasRegisteredPlayer()}");
        Debug.Log($"Current Player: {GetCurrentPlayerName()}");
        Debug.Log($"Current Rank: {GetCurrentPlayerRank()}");
        Debug.Log($"Is Syncing: {isFirebaseSyncing}");
        
        if (rankingManager != null)
        {
            Debug.Log($"Total Players: {rankingManager.GetTotalPlayers()}");
            var topPlayers = rankingManager.GetTopPlayers(3);
            Debug.Log($"Top 3 players:");
            for (int i = 0; i < topPlayers.Count; i++)
            {
                Debug.Log($"  {i+1}. {topPlayers[i].name} - {topPlayers[i].bestScore} pts");
            }
        }
    }
    
    [ContextMenu("Simulate Login Flow")]
    void SimulateLoginFlow()
    {
        Debug.Log("=== SIMULATING COMPLETE LOGIN FLOW WITH FIREBASE ===");
        
        // Step 1: Clear current session
        if (rankingManager != null)
        {
            rankingManager.LogoutCurrentPlayer();
        }
        Debug.Log("Step 1: Logged out");
        
        // Step 2: Show login/register
        ShowLoginOrRegister();
        Debug.Log("Step 2: Showing login/register panel");
        
        Debug.Log("=== MANUAL TESTING REQUIRED FROM HERE ===");
        Debug.Log("Please test registration and login manually through UI");
    }
    #endif
}

// ============================================
// FIN DEL GAMEUIMANAGER CON FIREBASE
// ============================================