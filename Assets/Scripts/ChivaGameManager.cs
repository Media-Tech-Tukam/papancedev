using UnityEngine;
using UnityEngine.SceneManagement;

public class ChivaGameManager : MonoBehaviour
{
    [Header("Game Rules")]
    [SerializeField] private float maxDrunkenness = 100f; // Condición de derrota
    [SerializeField] private float victoryDistance = 25000f; // 25km = victoria
    [SerializeField] private float checkpointDistance = 1000f; // Checkpoint cada 1km
    
    [Header("Game State")]
    [SerializeField] private bool gameStarted = false;
    [SerializeField] private bool gameEnded = false;
    [SerializeField] private bool isPaused = false;
    
    [Header("Game Stats")]
    [SerializeField] private int passengersPickedUp = 0;
    [SerializeField] private int passengersLost = 0;
    [SerializeField] private float totalPlayTime = 0f;
    [SerializeField] private int drinksGiven = 0;
    [SerializeField] private float maxSpeedReached = 0f;
    
    [Header("UI References")]
    [SerializeField] private ChivaUIManager uiManager;
    [SerializeField] private bool autoFindUI = true;
    
    [Header("Audio")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private bool autoFindAudio = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private bool showGameStats = true;
    
    // Referencias a sistemas
    private ChivaController chivaController;
    private ChivaSplineGenerator splineGenerator;
    private DrunkennessSystem drunkennessSystem;
    private PassengerManager passengerManager;
    
    // Estado del juego
    private GameState currentState = GameState.Loading;
    private float lastCheckpointDistance = 0f;
    private Vector3 checkpointPosition = Vector3.zero;
    private float gameStartTime = 0f;
    
    // Eventos del juego
    public System.Action OnGameStart;
    public System.Action<GameEndReason> OnGameEnd;
    public System.Action<float> OnCheckpointReached; // Distancia del checkpoint
    public System.Action<bool> OnGamePause; // true = paused, false = unpaused
    public System.Action<GameStats> OnStatsUpdate;
    
    // Enums
    public enum GameState
    {
        Loading,        // Generando spline
        Ready,          // Listo para empezar
        Playing,        // Jugando
        Paused,         // Pausado
        Victory,        // Llegó a 25km
        Defeat,         // Borrachera llegó a 100%
        GameOver        // Fin del juego
    }
    
    public enum GameEndReason
    {
        Victory,        // Llegó al final
        Drunkenness,    // Borrachera excesiva
        Quit,           // Jugador salió
        Error           // Error del sistema
    }
    
    [System.Serializable]
    public struct GameStats
    {
        public float distanceTraveled;
        public float routeProgress;
        public float currentDrunkenness;
        public int passengersTotal;
        public int passengersLost;
        public float playTime;
        public float currentSpeed;
        public float averageSpeed;
        public int drinksCount;
        public float maxSpeed;
        
        public GameStats(ChivaGameManager manager)
        {
            distanceTraveled = manager.chivaController?.CurrentDistance ?? 0f;
            routeProgress = manager.chivaController?.RouteProgress ?? 0f;
            currentDrunkenness = manager.drunkennessSystem?.CurrentDrunkenness ?? 0f;
            passengersTotal = manager.passengersPickedUp;
            passengersLost = manager.passengersLost;
            playTime = manager.totalPlayTime;
            currentSpeed = manager.chivaController?.CurrentSpeed ?? 0f;
            averageSpeed = distanceTraveled > 0 ? distanceTraveled / playTime : 0f;
            drinksCount = manager.drinksGiven;
            maxSpeed = manager.maxSpeedReached;
        }
    }
    
    void Awake()
    {
        Debug.Log("=== CHIVA GAME MANAGER STARTING ===");
        
        // Asegurar que solo haya un GameManager
        if (FindObjectsOfType<ChivaGameManager>().Length > 1)
        {
            Debug.LogWarning("Multiple GameManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Persistir entre escenas si es necesario
        // DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        StartGameSequence();
    }
    
    void InitializeComponents()
    {
        // Buscar referencias automáticamente
        chivaController = FindObjectOfType<ChivaController>();
        splineGenerator = FindObjectOfType<ChivaSplineGenerator>();
        drunkennessSystem = FindObjectOfType<DrunkennessSystem>();
        passengerManager = FindObjectOfType<PassengerManager>();
        
        if (autoFindUI && uiManager == null)
            uiManager = FindObjectOfType<ChivaUIManager>();
        
        if (autoFindAudio)
        {
            if (musicAudioSource == null)
            {
                AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
                foreach (AudioSource source in audioSources)
                {
                    if (source.name.ToLower().Contains("music"))
                        musicAudioSource = source;
                    else if (source.name.ToLower().Contains("sfx"))
                        sfxAudioSource = source;
                }
            }
        }
        
        // Validar componentes críticos
        if (chivaController == null)
            Debug.LogError("ChivaGameManager: ChivaController not found!");
        if (splineGenerator == null)
            Debug.LogError("ChivaGameManager: ChivaSplineGenerator not found!");
            
        Debug.Log($"Components initialized. Critical systems: {(chivaController != null && splineGenerator != null ? "✓" : "✗")}");
    }
    
    void SetupEventListeners()
    {
        // Spline Generator events
        if (splineGenerator != null)
        {
            splineGenerator.OnGenerationComplete += OnSplineGenerationComplete;
            splineGenerator.OnGenerationProgress += OnSplineGenerationProgress;
        }
        
        // Chiva Controller events
        if (chivaController != null)
        {
            chivaController.OnSpeedChanged += OnChivaSpeedChanged;
            chivaController.OnProgressChanged += OnRouteProgressChanged;
        }
        
        // Drunkenness System events
        if (drunkennessSystem != null)
        {
            drunkennessSystem.OnDrunkennessChanged += OnDrunkennessChanged;
            drunkennessSystem.OnDrinkGiven += OnDrinkGiven;
        }
        
        // Passenger Manager events
        if (passengerManager != null)
        {
            passengerManager.OnPassengerPickup += OnPassengerPickup;
            passengerManager.OnPassengerLost += OnPassengerLost;
        }
    }
    
    void Update()
    {
        if (currentState == GameState.Playing && !isPaused)
        {
            UpdateGameTime();
            CheckVictoryCondition();
            CheckDefeatCondition();
            CheckCheckpoints();
            UpdateStats();
        }
        
        HandleDebugInput();
    }
    
    void UpdateGameTime()
    {
        totalPlayTime = Time.time - gameStartTime;
    }
    
    void StartGameSequence()
    {
        currentState = GameState.Loading;
        Debug.Log("Starting game sequence...");
        
        if (splineGenerator != null)
        {
            if (splineGenerator.IsGenerationComplete())
            {
                OnSplineGenerationComplete();
            }
            // Si no está completo, esperamos al evento OnGenerationComplete
        }
        else
        {
            Debug.LogError("Cannot start game: No spline generator!");
        }
    }
    
    void OnSplineGenerationComplete()
    {
        Debug.Log("Spline generation complete! Game ready to start.");
        currentState = GameState.Ready;
        
        // Inicializar checkpoint
        lastCheckpointDistance = 0f;
        checkpointPosition = chivaController?.transform.position ?? Vector3.zero;
        
        StartGame();
    }
    
    void OnSplineGenerationProgress(float progress)
    {
        uiManager?.UpdateLoadingProgress(progress);
    }
    
    void StartGame()
    {
        if (currentState != GameState.Ready) return;
        
        Debug.Log("Starting Chiva game!");
        
        currentState = GameState.Playing;
        gameStarted = true;
        gameStartTime = Time.time;
        
        // Notificar sistemas
        OnGameStart?.Invoke();
        
        // Iniciar sistemas
        if (drunkennessSystem != null)
            drunkennessSystem.StartDrinking();
        
        if (passengerManager != null)
            passengerManager.StartSpawning();
        
        if (uiManager != null)
        {
            uiManager.ShowGameUI();
            uiManager.HideLoadingUI();
        }
        
        Debug.Log("Game started successfully!");
    }
    
    void CheckVictoryCondition()
    {
        if (chivaController == null) return;
        
        float currentDistance = chivaController.CurrentDistance;
        if (currentDistance >= victoryDistance)
        {
            EndGame(GameEndReason.Victory);
        }
    }
    
    void CheckDefeatCondition()
    {
        if (drunkennessSystem == null) return;
        
        float currentDrunkenness = drunkennessSystem.CurrentDrunkenness;
        if (currentDrunkenness >= maxDrunkenness)
        {
            EndGame(GameEndReason.Drunkenness);
        }
    }
    
    void CheckCheckpoints()
    {
        if (chivaController == null) return;
        
        float currentDistance = chivaController.CurrentDistance;
        float nextCheckpoint = lastCheckpointDistance + checkpointDistance;
        
        if (currentDistance >= nextCheckpoint)
        {
            lastCheckpointDistance = nextCheckpoint;
            checkpointPosition = chivaController.transform.position;
            
            OnCheckpointReached?.Invoke(lastCheckpointDistance);
            
            if (debugMode)
            {
                Debug.Log($"Checkpoint reached at {lastCheckpointDistance/1000f:F1}km");
            }
        }
    }
    
    void UpdateStats()
    {
        // Actualizar velocidad máxima
        if (chivaController != null)
        {
            float currentSpeed = chivaController.CurrentSpeed;
            if (currentSpeed > maxSpeedReached)
                maxSpeedReached = currentSpeed;
        }
        
        // Notificar cambios de stats
        GameStats currentStats = new GameStats(this);
        OnStatsUpdate?.Invoke(currentStats);
    }
    
    void EndGame(GameEndReason reason)
    {
        if (gameEnded) return;
        
        Debug.Log($"Game ended: {reason}");
        
        gameEnded = true;
        currentState = reason == GameEndReason.Victory ? GameState.Victory : GameState.Defeat;
        
        // Parar sistemas
        if (drunkennessSystem != null)
            drunkennessSystem.StopDrinking();
        
        if (passengerManager != null)
            passengerManager.StopSpawning();
        
        // Notificar fin del juego
        OnGameEnd?.Invoke(reason);
        
        // Mostrar UI de fin del juego
        if (uiManager != null)
        {
            GameStats finalStats = new GameStats(this);
            if (reason == GameEndReason.Victory)
                uiManager.ShowVictoryScreen(finalStats);
            else
                uiManager.ShowDefeatScreen(finalStats);
        }
    }
    
    // ========== EVENT HANDLERS ==========
    
    void OnChivaSpeedChanged(float newSpeed)
    {
        // Actualizar estadísticas
    }
    
    void OnRouteProgressChanged(float progress)
    {
        // Actualizar UI de progreso
        if (uiManager != null)
            uiManager.UpdateRouteProgress(progress);
    }
    
    void OnDrunkennessChanged(float newDrunkenness)
    {
        // Actualizar UI de borrachera
        if (uiManager != null)
            uiManager.UpdateDrunkennessBar(newDrunkenness / maxDrunkenness);
        
        // Aplicar efectos a la chiva
        if (chivaController != null)
            chivaController.SetDrunkenness(newDrunkenness);
    }
    
    void OnDrinkGiven(float amount)
    {
        drinksGiven++;
        Debug.Log($"Tío borracho gave drink #{drinksGiven} (amount: {amount:F1})");
    }
    
    void OnPassengerPickup(int passengers)
    {
        passengersPickedUp += passengers;
        Debug.Log($"Picked up {passengers} passengers. Total: {passengersPickedUp}");
        
        if (uiManager != null)
            uiManager.UpdatePassengerCount(passengersPickedUp);
    }
    
    void OnPassengerLost(int passengers)
    {
        passengersLost += passengers;
        Debug.Log($"Lost {passengers} passengers. Total lost: {passengersLost}");
    }
    
    // ========== MÉTODOS PÚBLICOS DE CONTROL ==========
    
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        
        isPaused = true;
        currentState = GameState.Paused;
        Time.timeScale = 0f;
        
        OnGamePause?.Invoke(true);
        
        if (uiManager != null)
            uiManager.ShowPauseMenu();
        
        Debug.Log("Game paused");
    }
    
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        
        isPaused = false;
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        OnGamePause?.Invoke(false);
        
        if (uiManager != null)
            uiManager.HidePauseMenu();
        
        Debug.Log("Game resumed");
    }
    
    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        EndGame(GameEndReason.Quit);
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // ========== GETTERS ==========
    
    public GameState CurrentState => currentState;
    public bool IsGameActive => currentState == GameState.Playing && !isPaused;
    public GameStats GetCurrentStats() => new GameStats(this);
    public float GetVictoryProgress() => (chivaController?.CurrentDistance ?? 0f) / victoryDistance;
    public float GetDefeatProgress() => (drunkennessSystem?.CurrentDrunkenness ?? 0f) / maxDrunkenness;
    
    // ========== DEBUG ==========
    
    void HandleDebugInput()
    {
        if (!debugMode) return;
        
        if (Input.GetKeyDown(restartKey))
            RestartGame();
        
        if (Input.GetKeyDown(pauseKey))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }
    
    void OnGUI()
    {
        if (!debugMode || !showGameStats) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Box("CHIVA GAME DEBUG");
        
        GUILayout.Label($"State: {currentState}");
        GUILayout.Label($"Distance: {(chivaController?.GetDistanceKm() ?? 0f):F2} km");
        GUILayout.Label($"Progress: {GetVictoryProgress()*100f:F1}%");
        GUILayout.Label($"Speed: {(chivaController?.GetSpeedKmh() ?? 0f):F1} km/h");
        GUILayout.Label($"Drunkenness: {(drunkennessSystem?.CurrentDrunkenness ?? 0f):F1}%");
        GUILayout.Label($"Passengers: {passengersPickedUp} (-{passengersLost})");
        GUILayout.Label($"Drinks: {drinksGiven}");
        GUILayout.Label($"Time: {totalPlayTime:F1}s");
        
        GUILayout.Space(10);
        GUILayout.Label("Controls:");
        GUILayout.Label($"R - Restart | P - Pause");
        
        GUILayout.EndArea();
    }
}
