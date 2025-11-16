using UnityEngine;
using System.Collections;

public class DrunkennessSystem : MonoBehaviour
{
    [Header("Drunkenness Configuration")]
    [SerializeField] private float maxDrunkenness = 100f;
    [SerializeField] private float currentDrunkenness = 0f;
    [SerializeField] private float baseDrunkennessIncrease = 7f; // deltaBorrachera del GDD
    [SerializeField] private float drunkennessDecayRate = 1f; // Reducción natural por segundo
    
    [Header("Tío Borracho Timing")]
    [SerializeField] private float minDrinkInterval = 8f; // tragoMin del GDD
    [SerializeField] private float maxDrinkInterval = 25f; // tragoMax del GDD
    [SerializeField] private bool randomizeFirstDrink = true;
    [SerializeField] private float firstDrinkDelay = 15f; // Gracia inicial
    
    [Header("Passenger Protection")]
    [SerializeField] private int maxProtectionPassengers = 20; // Máximo beneficio
    [SerializeField] private float maxProtectionReduction = 0.8f; // 80% reducción máxima
    [SerializeField] private AnimationCurve protectionCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
    
    [Header("Special Passengers")]
    [SerializeField] private float electrolitReduction = 15f; // Reducción por pasajero electrolit
    [SerializeField] private float electrolitEffectDuration = 10f;
    
    [Header("Effects Configuration")]
    [SerializeField] private bool enableAudioEffects = true;
    [SerializeField] private bool enablePhysicsEffects = true;
    
    [Header("Audio Effects")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private float minMusicVolume = 0.3f;
    [SerializeField] private float maxMusicVolume = 1f;
    [SerializeField] private float maxReverb = 0.5f;
    [SerializeField] private bool autoFindAudio = true;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] drinkSounds;
    [SerializeField] private AudioClip[] burpSounds;
    [SerializeField] private AudioClip electrolitSound;
    [SerializeField] private float sfxVolume = 0.7f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showEffectsInfo = false;
    [SerializeField] private KeyCode forceDrinkKey = KeyCode.F;
    [SerializeField] private KeyCode electrolitTestKey = KeyCode.E;
    
    // Estado interno
    private bool isDrinkingActive = false;
    private float nextDrinkTime = 0f;
    private int currentPassengerCount = 0;
    private bool isElectroliting = false;
    private float electrolitEndTime = 0f;
    
    // Referencias
    private ChivaController chivaController;
    private PassengerManager passengerManager;
    private ChivaGameManager gameManager;
    private AudioReverbFilter reverbFilter;
    
    // Corrutinas
    private Coroutine drinkingCoroutine;
    
    // Eventos
    public System.Action<float> OnDrunkennessChanged; // Nuevo nivel de borrachera
    public System.Action<float> OnDrinkGiven; // Cantidad de trago dado
    public System.Action OnTioBorrachoEvent; // Evento del tío borracho
    public System.Action<float> OnElectroliting; // Efecto electrolit
    
    // Propiedades públicas
    public float CurrentDrunkenness => currentDrunkenness;
    public float DrunkennessPercentage => currentDrunkenness / maxDrunkenness;
    public bool IsVeryDrunk => currentDrunkenness > (maxDrunkenness * 0.7f);
    public bool IsDrinking => isDrinkingActive;
    public float TimeToNextDrink => Mathf.Max(0f, nextDrinkTime - Time.time);
    
    void Start()
    {
        Debug.Log("=== DRUNKENNESS SYSTEM STARTING ===");
        
        InitializeComponents();
        SetupEffects();
        SetupEventListeners();
        
        Debug.Log($"Drunkenness system initialized. Drink intervals: {minDrinkInterval}-{maxDrinkInterval}s");
    }
    
    void InitializeComponents()
    {
        // Buscar referencias automáticamente
        chivaController = FindObjectOfType<ChivaController>();
        passengerManager = FindObjectOfType<PassengerManager>();
        gameManager = FindObjectOfType<ChivaGameManager>();
        
        // Buscar audio
        if (autoFindAudio)
        {
            if (musicAudioSource == null || sfxAudioSource == null)
            {
                AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
                foreach (AudioSource source in audioSources)
                {
                    if (source.name.ToLower().Contains("music") && musicAudioSource == null)
                        musicAudioSource = source;
                    else if (source.name.ToLower().Contains("sfx") && sfxAudioSource == null)
                        sfxAudioSource = source;
                }
            }
        }
    }
    
    void SetupEffects()
    {
        // Configurar reverb filter para efectos de audio
        if (musicAudioSource != null && enableAudioEffects)
        {
            reverbFilter = musicAudioSource.gameObject.GetComponent<AudioReverbFilter>();
            if (reverbFilter == null)
                reverbFilter = musicAudioSource.gameObject.AddComponent<AudioReverbFilter>();
            
            reverbFilter.enabled = false; // Empezar deshabilitado
        }
    }
    
    void SetupEventListeners()
    {
        // Escuchar eventos del passenger manager
        if (passengerManager != null)
        {
            passengerManager.OnPassengerPickup += OnPassengerCountChanged;
            passengerManager.OnPassengerLost += OnPassengerCountChanged;
            passengerManager.OnElectrolitPickup += OnElectrolitPickup;
        }
    }
    
    void Update()
    {
        if (isDrinkingActive)
        {
            UpdateDrunkennessDecay();
            UpdateEffects();
            HandleElectroliting();
        }
        
        HandleDebugInput();
    }
    
    void UpdateDrunkennessDecay()
    {
        // Reducción natural muy lenta
        if (currentDrunkenness > 0f && !isElectroliting)
        {
            float decayAmount = drunkennessDecayRate * Time.deltaTime;
            currentDrunkenness = Mathf.Max(0f, currentDrunkenness - decayAmount);
        }
    }
    
    void UpdateEffects()
    {
        float drunknessFactor = DrunkennessPercentage;
        
        // Efectos de audio
        if (enableAudioEffects)
        {
            UpdateAudioEffects(drunknessFactor);
        }
    }
    
    void UpdateAudioEffects(float drunknessFactor)
    {
        // Volumen de música cambia con borrachera (como especificado)
        if (musicAudioSource != null)
        {
            float targetVolume = Mathf.Lerp(maxMusicVolume, minMusicVolume, drunknessFactor);
            musicAudioSource.volume = Mathf.Lerp(musicAudioSource.volume, targetVolume, Time.deltaTime * 2f);
        }
        
        // Reverb aumenta con borrachera
        if (reverbFilter != null)
        {
            reverbFilter.enabled = drunknessFactor > 0.2f;
            if (reverbFilter.enabled)
            {
                // Usar las propiedades correctas de AudioReverbFilter
                reverbFilter.dryLevel = Mathf.Lerp(0f, -1000f, drunknessFactor);
                reverbFilter.room = Mathf.Lerp(-1000f, -100f, drunknessFactor * maxReverb);
                reverbFilter.roomHF = Mathf.Lerp(-100f, -1000f, drunknessFactor);
                reverbFilter.decayTime = Mathf.Lerp(1f, 3f, drunknessFactor);
                reverbFilter.reflectionsLevel = Mathf.Lerp(-10000f, -500f, drunknessFactor * maxReverb);
            }
        }
    }
    
    void HandleElectroliting()
    {
        if (isElectroliting && Time.time >= electrolitEndTime)
        {
            isElectroliting = false;
            Debug.Log("Electrolit effect ended");
        }
    }
    
    // ========== SISTEMA DEL TÍO BORRACHO ==========
    
    public void StartDrinking()
    {
        if (isDrinkingActive) return;
        
        Debug.Log("Starting drinking system (Tío Borracho activated)");
        
        isDrinkingActive = true;
        
        // Programar primer trago
        float firstDelay = randomizeFirstDrink ? Random.Range(firstDrinkDelay, firstDrinkDelay * 2f) : firstDrinkDelay;
        nextDrinkTime = Time.time + firstDelay;
        
        // Iniciar corrutina del tío borracho
        if (drinkingCoroutine != null)
            StopCoroutine(drinkingCoroutine);
        
        drinkingCoroutine = StartCoroutine(TioBorrachoLoop());
    }
    
    public void StopDrinking()
    {
        Debug.Log("Stopping drinking system");
        
        isDrinkingActive = false;
        
        if (drinkingCoroutine != null)
        {
            StopCoroutine(drinkingCoroutine);
            drinkingCoroutine = null;
        }
    }
    
    private IEnumerator TioBorrachoLoop()
    {
        while (isDrinkingActive)
        {
            // Esperar hasta el próximo trago
            yield return new WaitUntil(() => Time.time >= nextDrinkTime);
            
            // Dar trago
            GiveDrink();
            
            // Programar próximo trago
            float nextInterval = Random.Range(minDrinkInterval, maxDrinkInterval);
            nextDrinkTime = Time.time + nextInterval;
            
            if (debugMode)
                Debug.Log($"Next drink in {nextInterval:F1}s");
        }
    }
    
    private void GiveDrink()
    {
        if (!isDrinkingActive) return;
        
        // Calcular cantidad del trago basado en pasajeros (fórmula del GDD)
        float protectionFactor = CalculatePassengerProtection();
        float drinkAmount = baseDrunkennessIncrease * protectionFactor;
        
        // Aplicar efectos de electrolit
        if (isElectroliting)
        {
            drinkAmount *= 0.5f; // Reducir efecto durante electrolit
        }
        
        // Añadir borrachera
        currentDrunkenness = Mathf.Min(maxDrunkenness, currentDrunkenness + drinkAmount);
        
        // Efectos visuales/audio del trago
        PlayDrinkEffects();
        
        // Notificar eventos
        OnDrinkGiven?.Invoke(drinkAmount);
        OnTioBorrachoEvent?.Invoke();
        OnDrunkennessChanged?.Invoke(currentDrunkenness);
        
        Debug.Log($"Tío borracho gave drink! Amount: {drinkAmount:F1} (protection: {protectionFactor:F2}, total: {currentDrunkenness:F1})");
    }
    
    private float CalculatePassengerProtection()
    {
        // Fórmula del GDD: f(numPasajeros) = 1 - clamp(numPasajeros / maxPasajeros, 0, maxReduction)
        float passengerRatio = Mathf.Clamp01((float)currentPassengerCount / maxProtectionPassengers);
        float protection = protectionCurve.Evaluate(passengerRatio);
        float reduction = 1f - (maxProtectionReduction * protection);
        
        return Mathf.Clamp(reduction, 1f - maxProtectionReduction, 1f);
    }
    
    private void PlayDrinkEffects()
    {
        // Sonido de trago
        if (sfxAudioSource != null && drinkSounds.Length > 0)
        {
            AudioClip drinkClip = drinkSounds[Random.Range(0, drinkSounds.Length)];
            sfxAudioSource.PlayOneShot(drinkClip, sfxVolume);
        }
        
        // Sonido opcional de eructo
        if (Random.value < 0.3f && burpSounds.Length > 0)
        {
            StartCoroutine(PlayDelayedBurp());
        }
    }
    
    private IEnumerator PlayDelayedBurp()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));
        
        if (sfxAudioSource != null && burpSounds.Length > 0)
        {
            AudioClip burpClip = burpSounds[Random.Range(0, burpSounds.Length)];
            sfxAudioSource.PlayOneShot(burpClip, sfxVolume * 0.6f);
        }
    }
    
    // ========== PASAJEROS Y ELECTROLIT ==========
    
    private void OnPassengerCountChanged(int newCount)
    {
        currentPassengerCount = Mathf.Max(0, newCount);
        Debug.Log($"Passenger count updated: {currentPassengerCount}");
    }
    
    private void OnElectrolitPickup(float reductionAmount = -1f)
    {
        if (reductionAmount < 0f)
            reductionAmount = electrolitReduction;
        
        // Reducir borrachera inmediatamente
        currentDrunkenness = Mathf.Max(0f, currentDrunkenness - reductionAmount);
        
        // Activar efecto temporal
        isElectroliting = true;
        electrolitEndTime = Time.time + electrolitEffectDuration;
        
        // Efectos de audio
        if (sfxAudioSource != null && electrolitSound != null)
        {
            sfxAudioSource.PlayOneShot(electrolitSound, sfxVolume);
        }
        
        // Notificar eventos
        OnElectroliting?.Invoke(reductionAmount);
        OnDrunkennessChanged?.Invoke(currentDrunkenness);
        
        Debug.Log($"Electrolit consumed! Reduced drunkenness by {reductionAmount:F1}. New level: {currentDrunkenness:F1}");
    }
    
    // ========== MÉTODOS PÚBLICOS ==========
    
    public void SetDrunkenness(float amount)
    {
        currentDrunkenness = Mathf.Clamp(amount, 0f, maxDrunkenness);
        OnDrunkennessChanged?.Invoke(currentDrunkenness);
    }
    
    public void AddDrunkenness(float amount)
    {
        SetDrunkenness(currentDrunkenness + amount);
    }
    
    public void ReduceDrunkenness(float amount)
    {
        SetDrunkenness(currentDrunkenness - amount);
    }
    
    public void ForceDrink()
    {
        if (isDrinkingActive)
        {
            GiveDrink();
        }
    }
    
    public void ForceElectrolit()
    {
        OnElectrolitPickup();
    }
    
    public void UpdatePassengerCount(int count)
    {
        currentPassengerCount = Mathf.Max(0, count);
    }
    
    // ========== DEBUG ==========
    
    void HandleDebugInput()
    {
        if (!debugMode) return;
        
        if (Input.GetKeyDown(forceDrinkKey))
        {
            Debug.Log("Debug: Forcing drink");
            ForceDrink();
        }
        
        if (Input.GetKeyDown(electrolitTestKey))
        {
            Debug.Log("Debug: Testing electrolit");
            ForceElectrolit();
        }
    }
    
    void OnGUI()
    {
        if (!debugMode || !showEffectsInfo) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 300, 300));
        GUILayout.Box("DRUNKENNESS DEBUG");
        
        GUILayout.Label($"Drunkenness: {currentDrunkenness:F1} / {maxDrunkenness}");
        GUILayout.Label($"Percentage: {DrunkennessPercentage*100f:F1}%");
        GUILayout.Label($"Drinking: {(isDrinkingActive ? "Active" : "Inactive")}");
        GUILayout.Label($"Next drink in: {TimeToNextDrink:F1}s");
        GUILayout.Label($"Passengers: {currentPassengerCount}");
        GUILayout.Label($"Protection: {CalculatePassengerProtection():F2}");
        GUILayout.Label($"Electroliting: {(isElectroliting ? "Yes" : "No")}");
        
        GUILayout.Space(10);
        GUILayout.Label("Debug Controls:");
        GUILayout.Label($"F - Force Drink");
        GUILayout.Label($"E - Test Electrolit");
        
        GUILayout.EndArea();
    }
    
    void OnDestroy()
    {
        // Cleanup
        if (drinkingCoroutine != null)
            StopCoroutine(drinkingCoroutine);
    }
}