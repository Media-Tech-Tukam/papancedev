using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PassengerManager : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [SerializeField] private float spawnRatePerMinute = 12f; // spawnRatePeatones del GDD
    [SerializeField] private float minSpawnDistance = 100f; // Distancia mínima entre spawns
    [SerializeField] private float spawnAheadDistance = 300f; // Distancia de spawn por delante
    [SerializeField] private float cleanupBehindDistance = 100f; // Distancia de cleanup por detrás
    [SerializeField] private float spawnHeightOffset = 0.5f;
    
    [Header("Passenger Types")]
    [SerializeField] private PassengerType[] passengerTypes;
    [SerializeField] private float electrolitSpawnChance = 0.05f; // 5% probabilidad
    [SerializeField] private float electrolitCooldownTime = 30f; // Tiempo entre electrolit spawns
    
    [Header("Pickup Configuration")]
    [SerializeField] private float pickupSpeedThreshold = 8f; // Velocidad máxima para recoger
    [SerializeField] private float pickupRadius = 3f; // Radio de detección
    [SerializeField] private int minPassengersPerPickup = 1;
    [SerializeField] private int maxPassengersPerPickup = 4; // maxPorParada del GDD
    [SerializeField] private float pickupCooldown = 2f; // Tiempo entre pickups
    
    [Header("Side Distribution")]
    [SerializeField] private float leftSideChance = 0.5f; // 50% izquierda, 50% derecha
    [SerializeField] private Vector2 lateralOffsetRange = new Vector2(2f, 4f); // Rango de posición lateral
    [SerializeField] private bool avoidRecentSides = true; // Evitar spawns consecutivos del mismo lado
    
    [Header("Prefabs")]
    [SerializeField] private GameObject normalPassengerPrefab;
    [SerializeField] private GameObject electrolitPassengerPrefab;
    [SerializeField] private GameObject pickupEffectPrefab;
    [SerializeField] private Transform passengerParent; // Contenedor para organizar
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] pickupSounds;
    [SerializeField] private AudioClip[] passengerCheerSounds;
    [SerializeField] private AudioClip electrolitPickupSound;
    [SerializeField] private float audioVolume = 0.7f;
    [SerializeField] private bool autoFindAudioSource = true;
    
    [Header("Performance")]
    [SerializeField] private int maxActivePassengers = 50;
    [SerializeField] private float poolingPrewarm = 20; // Pasajeros pre-instanciados
    [SerializeField] private bool useObjectPooling = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showSpawnZones = false;
    [SerializeField] private bool showPickupRange = false;
    [SerializeField] private Color debugSpawnColor = Color.green;
    [SerializeField] private Color debugPickupColor = Color.yellow;
    
    // Referencias del sistema
    private ChivaController chivaController;
    private ChivaSplineGenerator splineGenerator;
    private DrunkennessSystem drunkennessSystem;
    
    // Estado del spawning
    private bool isSpawningActive = false;
    private float lastSpawnTime = 0f;
    private float lastSpawnDistance = 0f;
    private float lastElectrolitSpawnTime = 0f;
    private float lastPickupTime = 0f;
    private bool lastSpawnWasLeftSide = false;
    
    // Tracking de pasajeros
    private List<ActivePassenger> activePassengers = new List<ActivePassenger>();
    private Queue<GameObject> passengerPool = new Queue<GameObject>();
    private Queue<GameObject> electrolitPool = new Queue<GameObject>();
    private int currentPassengerCount = 0;
    private int totalPassengersPickedUp = 0;
    private int totalPassengersLost = 0;
    
    // Corrutinas
    private Coroutine spawningCoroutine;
    private Coroutine cleanupCoroutine;
    
    // Eventos
    public System.Action<int> OnPassengerPickup; // Cantidad recogida
    public System.Action<int> OnPassengerLost; // Cantidad perdida
    public System.Action<float> OnElectrolitPickup; // Cantidad de reducción de borrachera
    public System.Action<ActivePassenger> OnPassengerSpawned; // Pasajero creado
    public System.Action<Vector3> OnPickupEffect; // Posición del efecto
    
    [System.Serializable]
    public class PassengerType
    {
        public string name = "Normal";
        public GameObject prefab;
        public float spawnWeight = 1f; // Peso en la distribución aleatoria
        public int minPickupCount = 1;
        public int maxPickupCount = 3;
        public bool isSpecial = false;
    }
    
    [System.Serializable]
    public class ActivePassenger
    {
        public GameObject gameObject;
        public PassengerComponent passengerComponent;
        public float spawnDistance;
        public Vector3 spawnPosition;
        public bool isLeftSide;
        public bool isElectrolit;
        public float spawnTime;
        
        public bool IsValid => gameObject != null && passengerComponent != null;
        public bool IsPickedUp => passengerComponent != null ? passengerComponent.IsPickedUp : false;
    }
    
    // Getters públicos
    public int CurrentPassengerCount => currentPassengerCount;
    public int TotalPassengersPickedUp => totalPassengersPickedUp;
    public int TotalPassengersLost => totalPassengersLost;
    public bool IsSpawning => isSpawningActive;
    public int ActivePassengerCount => activePassengers.Count;
    
    void Start()
    {
        Debug.Log("=== PASSENGER MANAGER STARTING ===");
        
        InitializeComponents();
        SetupObjectPooling();
        SetupEventListeners();
        
        Debug.Log($"Passenger manager initialized. Spawn rate: {spawnRatePerMinute}/min");
    }
    
    void InitializeComponents()
    {
        // Buscar referencias
        chivaController = FindObjectOfType<ChivaController>();
        splineGenerator = FindObjectOfType<ChivaSplineGenerator>();
        drunkennessSystem = FindObjectOfType<DrunkennessSystem>();
        
        if (chivaController == null)
            Debug.LogError("PassengerManager: ChivaController not found!");
        if (splineGenerator == null)
            Debug.LogError("PassengerManager: ChivaSplineGenerator not found!");
        
        // Audio source
        if (autoFindAudioSource && audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Contenedor para organización
        if (passengerParent == null)
        {
            GameObject parentObj = new GameObject("Passengers");
            parentObj.transform.SetParent(transform);
            passengerParent = parentObj.transform;
        }
    }
    
    void SetupObjectPooling()
    {
        if (!useObjectPooling) return;
        
        Debug.Log($"Setting up passenger object pooling ({poolingPrewarm} pre-warmed)");
        
        // Pre-crear pasajeros normales
        for (int i = 0; i < poolingPrewarm; i++)
        {
            if (normalPassengerPrefab != null)
            {
                GameObject passenger = CreatePassengerInstance(normalPassengerPrefab, false);
                passengerPool.Enqueue(passenger);
            }
        }
        
        // Pre-crear algunos electrolit
        for (int i = 0; i < poolingPrewarm * 0.2f; i++)
        {
            if (electrolitPassengerPrefab != null)
            {
                GameObject electrolit = CreatePassengerInstance(electrolitPassengerPrefab, false);
                electrolitPool.Enqueue(electrolit);
            }
        }
    }
    
    void SetupEventListeners()
    {
        // Conectar con el sistema de borrachera si existe
        if (drunkennessSystem != null)
        {
            OnPassengerPickup += (count) => {
                drunkennessSystem.UpdatePassengerCount(currentPassengerCount);
            };
            OnPassengerLost += (count) => {
                drunkennessSystem.UpdatePassengerCount(currentPassengerCount);
            };
        }
    }
    
    void Update()
    {
        if (isSpawningActive)
        {
            UpdatePassengerStates();
            CheckPickupOpportunities();
        }
    }
    
    void UpdatePassengerStates()
    {
        if (chivaController == null) return;
        
        float chivaDistance = chivaController.CurrentDistance;
        
        // Limpiar pasajeros muy lejanos
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            ActivePassenger passenger = activePassengers[i];
            
            if (!passenger.IsValid)
            {
                activePassengers.RemoveAt(i);
                continue;
            }
            
            // Verificar si está demasiado atrás
            float distanceBehind = chivaDistance - passenger.spawnDistance;
            if (distanceBehind > cleanupBehindDistance)
            {
                DestroyPassenger(passenger, false);
                activePassengers.RemoveAt(i);
            }
        }
    }
    
    void CheckPickupOpportunities()
    {
        if (chivaController == null || !chivaController.CanPickupPassengers) return;
        if (Time.time - lastPickupTime < pickupCooldown) return;
        
        Vector3 chivaPosition = chivaController.transform.position;
        
        // Buscar pasajeros en rango
        for (int i = activePassengers.Count - 1; i >= 0; i--)
        {
            ActivePassenger passenger = activePassengers[i];
            
            if (!passenger.IsValid || passenger.IsPickedUp) continue;
            
            float distance = Vector3.Distance(chivaPosition, passenger.spawnPosition);
            if (distance <= pickupRadius)
            {
                PickupPassenger(passenger);
                lastPickupTime = Time.time;
                break; // Solo un pickup por frame
            }
        }
    }
    
    // ========== SPAWNING SYSTEM ==========
    
    public void StartSpawning()
    {
        if (isSpawningActive) return;
        
        Debug.Log("Starting passenger spawning");
        isSpawningActive = true;
        
        if (spawningCoroutine != null)
            StopCoroutine(spawningCoroutine);
        
        if (cleanupCoroutine != null)
            StopCoroutine(cleanupCoroutine);
        
        spawningCoroutine = StartCoroutine(PassengerSpawningLoop());
        cleanupCoroutine = StartCoroutine(PeriodicCleanup());
    }
    
    public void StopSpawning()
    {
        Debug.Log("Stopping passenger spawning");
        isSpawningActive = false;
        
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
        
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
            cleanupCoroutine = null;
        }
    }
    
    private IEnumerator PassengerSpawningLoop()
    {
        while (isSpawningActive)
        {
            if (ShouldSpawnPassenger())
            {
                SpawnRandomPassenger();
            }
            
            // Esperar según el spawn rate
            float spawnInterval = 60f / spawnRatePerMinute; // Convertir rate/minuto a intervalo
            yield return new WaitForSeconds(spawnInterval + Random.Range(-1f, 1f));
        }
    }
    
    private IEnumerator PeriodicCleanup()
    {
        while (isSpawningActive)
        {
            yield return new WaitForSeconds(5f);
            
            // Limpiar pasajeros inválidos
            activePassengers.RemoveAll(p => !p.IsValid);
            
            // Control de memoria
            if (activePassengers.Count > maxActivePassengers)
            {
                Debug.LogWarning($"Too many active passengers ({activePassengers.Count}), cleaning oldest");
                CleanupOldestPassengers(maxActivePassengers * 0.8f);
            }
        }
    }
    
    private bool ShouldSpawnPassenger()
    {
        if (chivaController == null || splineGenerator == null) return false;
        if (activePassengers.Count >= maxActivePassengers) return false;
        
        float currentDistance = chivaController.CurrentDistance;
        
        // Verificar distancia mínima
        if (currentDistance - lastSpawnDistance < minSpawnDistance) return false;
        
        // Verificar que no esté muy cerca del final
        if (!splineGenerator.IsValidPassengerDistance(currentDistance + spawnAheadDistance)) return false;
        
        return true;
    }
    
    private void SpawnRandomPassenger()
    {
        if (chivaController == null || splineGenerator == null) return;
        
        // Determinar tipo de pasajero
        bool isElectrolit = ShouldSpawnElectrolit();
        
        // Determinar lado
        bool isLeftSide = DetermineSide();
        
        // Calcular posición de spawn
        float spawnDistance = chivaController.CurrentDistance + spawnAheadDistance;
        Vector3 spawnPosition = CalculateSpawnPosition(spawnDistance, isLeftSide);
        
        // Crear pasajero
        ActivePassenger newPassenger = CreatePassenger(spawnPosition, spawnDistance, isLeftSide, isElectrolit);
        
        if (newPassenger != null)
        {
            activePassengers.Add(newPassenger);
            lastSpawnDistance = spawnDistance;
            lastSpawnWasLeftSide = isLeftSide;
            OnPassengerSpawned?.Invoke(newPassenger);
            
            if (debugMode)
            {
                Debug.Log($"Spawned {(isElectrolit ? "ELECTROLIT" : "normal")} passenger at distance {spawnDistance:F0} ({(isLeftSide ? "LEFT" : "RIGHT")})");
            }
        }
    }
    
    private bool ShouldSpawnElectrolit()
    {
        // Verificar cooldown
        if (Time.time - lastElectrolitSpawnTime < electrolitCooldownTime) return false;
        
        // Verificar probabilidad
        return Random.value < electrolitSpawnChance;
    }
    
    private bool DetermineSide()
    {
        // Evitar lados repetidos si está habilitado
        if (avoidRecentSides && Random.value < 0.7f)
        {
            return !lastSpawnWasLeftSide;
        }
        
        return Random.value < leftSideChance;
    }
    
    private Vector3 CalculateSpawnPosition(float distance, bool isLeftSide)
    {
        Vector3 roadPosition = splineGenerator.GetSplinePosition(distance);
        Vector3 rightVector = splineGenerator.GetSplineRight(distance);
        
        // Calcular offset lateral
        float lateralDistance = Random.Range(lateralOffsetRange.x, lateralOffsetRange.y);
        if (isLeftSide) lateralDistance = -lateralDistance;
        
        Vector3 lateralOffset = rightVector * lateralDistance;
        Vector3 heightOffset = Vector3.up * spawnHeightOffset;
        
        return roadPosition + lateralOffset + heightOffset;
    }
    
    private ActivePassenger CreatePassenger(Vector3 position, float distance, bool isLeftSide, bool isElectrolit)
    {
        GameObject passengerObj = GetPassengerFromPool(isElectrolit);
        
        if (passengerObj == null) return null;
        
        // Configurar posición
        passengerObj.transform.position = position;
        passengerObj.transform.LookAt(position + splineGenerator.GetSplineDirection(distance));
        passengerObj.SetActive(true);
        
        // Obtener/configurar componente
        PassengerComponent passengerComp = passengerObj.GetComponent<PassengerComponent>();
        if (passengerComp == null)
            passengerComp = passengerObj.AddComponent<PassengerComponent>();
        
        passengerComp.Initialize(isElectrolit, isLeftSide);
        
        // Crear estructura de datos
        ActivePassenger activePassenger = new ActivePassenger
        {
            gameObject = passengerObj,
            passengerComponent = passengerComp,
            spawnDistance = distance,
            spawnPosition = position,
            isLeftSide = isLeftSide,
            isElectrolit = isElectrolit,
            spawnTime = Time.time
        };
        
        if (isElectrolit)
        {
            lastElectrolitSpawnTime = Time.time;
        }
        
        return activePassenger;
    }
    
    private GameObject GetPassengerFromPool(bool isElectrolit)
    {
        Queue<GameObject> pool = isElectrolit ? electrolitPool : passengerPool;
        GameObject prefab = isElectrolit ? electrolitPassengerPrefab : normalPassengerPrefab;
        
        if (useObjectPooling && pool.Count > 0)
        {
            return pool.Dequeue();
        }
        else
        {
            return CreatePassengerInstance(prefab, true);
        }
    }
    
    private GameObject CreatePassengerInstance(GameObject prefab, bool setActive = true)
    {
        if (prefab == null) return null;
        
        GameObject instance = Instantiate(prefab, passengerParent);
        instance.SetActive(setActive);
        
        return instance;
    }
    
    // ========== PICKUP SYSTEM ==========
    
    private void PickupPassenger(ActivePassenger passenger)
    {
        if (passenger?.passengerComponent == null) return;
        
        // Determinar cuántos pasajeros suben
        int pickupCount = Random.Range(minPassengersPerPickup, maxPassengersPerPickup + 1);
        
        // Procesar pickup
        passenger.passengerComponent.PerformPickup(pickupCount);
        
        // Actualizar contadores
        currentPassengerCount += pickupCount;
        totalPassengersPickedUp += pickupCount;
        
        // Efectos especiales para electrolit
        if (passenger.isElectrolit)
        {
            OnElectrolitPickup?.Invoke(-1f); // Usar valor por defecto
            PlayAudio(electrolitPickupSound);
        }
        else
        {
            PlayPickupAudio();
        }
        
        // Efectos visuales
        CreatePickupEffect(passenger.spawnPosition);
        
        // Notificar eventos
        OnPassengerPickup?.Invoke(pickupCount);
        
        // Cleanup
        DestroyPassenger(passenger, true);
        
        if (debugMode)
        {
            Debug.Log($"Picked up {pickupCount} passengers ({(passenger.isElectrolit ? "ELECTROLIT" : "normal")}). Total: {currentPassengerCount}");
        }
    }
    
    private void PlayPickupAudio()
    {
        // Sonido de pickup
        if (pickupSounds.Length > 0)
        {
            AudioClip clip = pickupSounds[Random.Range(0, pickupSounds.Length)];
            PlayAudio(clip);
        }
        
        // Sonido de celebración ocasional
        if (Random.value < 0.3f && passengerCheerSounds.Length > 0)
        {
            StartCoroutine(PlayDelayedCheer());
        }
    }
    
    private IEnumerator PlayDelayedCheer()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        
        if (passengerCheerSounds.Length > 0)
        {
            AudioClip cheer = passengerCheerSounds[Random.Range(0, passengerCheerSounds.Length)];
            PlayAudio(cheer, audioVolume * 0.7f);
        }
    }
    
    private void PlayAudio(AudioClip clip, float volume = -1f)
    {
        if (audioSource == null || clip == null) return;
        
        if (volume < 0f) volume = audioVolume;
        audioSource.PlayOneShot(clip, volume);
    }
    
    private void CreatePickupEffect(Vector3 position)
    {
        if (pickupEffectPrefab == null) return;
        
        GameObject effect = Instantiate(pickupEffectPrefab, position, Quaternion.identity);
        OnPickupEffect?.Invoke(position);
        
        // Auto-destruir efecto después de un tiempo
        Destroy(effect, 3f);
    }
    
    // ========== CLEANUP ==========
    
    private void DestroyPassenger(ActivePassenger passenger, bool wasPickedUp)
    {
        if (passenger?.gameObject == null) return;
        
        if (useObjectPooling && !wasPickedUp)
        {
            // Devolver al pool
            passenger.gameObject.SetActive(false);
            
            if (passenger.isElectrolit)
                electrolitPool.Enqueue(passenger.gameObject);
            else
                passengerPool.Enqueue(passenger.gameObject);
        }
        else
        {
            // Destruir completamente
            Destroy(passenger.gameObject);
        }
    }
    
    private void CleanupOldestPassengers(float targetCount)
    {
        activePassengers.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        
        int toRemove = Mathf.FloorToInt(activePassengers.Count - targetCount);
        for (int i = 0; i < toRemove && i < activePassengers.Count; i++)
        {
            DestroyPassenger(activePassengers[i], false);
        }
        
        activePassengers.RemoveRange(0, toRemove);
    }
    
    // ========== MÉTODOS PÚBLICOS ==========
    
    public void LosePassengers(int count, string reason = "")
    {
        int actualLoss = Mathf.Min(count, currentPassengerCount);
        currentPassengerCount -= actualLoss;
        totalPassengersLost += actualLoss;
        
        OnPassengerLost?.Invoke(actualLoss);
        
        Debug.Log($"Lost {actualLoss} passengers ({reason}). Remaining: {currentPassengerCount}");
    }
    
    public void ForceSpawnPassenger(bool isElectrolit = false, bool isLeftSide = true)
    {
        if (chivaController == null) return;
        
        float spawnDistance = chivaController.CurrentDistance + 50f;
        Vector3 spawnPosition = CalculateSpawnPosition(spawnDistance, isLeftSide);
        ActivePassenger passenger = CreatePassenger(spawnPosition, spawnDistance, isLeftSide, isElectrolit);
        
        if (passenger != null)
        {
            activePassengers.Add(passenger);
            Debug.Log($"Force spawned {(isElectrolit ? "electrolit" : "normal")} passenger");
        }
    }
    
    public void ClearAllPassengers()
    {
        foreach (ActivePassenger passenger in activePassengers)
        {
            DestroyPassenger(passenger, false);
        }
        
        activePassengers.Clear();
        currentPassengerCount = 0;
        
        Debug.Log("All passengers cleared");
    }
    
    // ========== GIZMOS Y DEBUG ==========
    
    void OnDrawGizmos()
    {
        if (!debugMode) return;
        
        if (chivaController != null && splineGenerator?.HasValidSpline() == true)
        {
            float chivaDistance = chivaController.CurrentDistance;
            
            // Mostrar zona de spawn
            if (showSpawnZones)
            {
                Gizmos.color = debugSpawnColor;
                Vector3 spawnCenter = splineGenerator.GetSplinePosition(chivaDistance + spawnAheadDistance);
                Gizmos.DrawWireCube(spawnCenter, new Vector3(lateralOffsetRange.y * 2f, 2f, 10f));
            }
            
            // Mostrar rango de pickup
            if (showPickupRange)
            {
                Gizmos.color = debugPickupColor;
                Gizmos.DrawWireSphere(chivaController.transform.position, pickupRadius);
            }
            
            // Mostrar pasajeros activos
            foreach (ActivePassenger passenger in activePassengers)
            {
                if (passenger.IsValid)
                {
                    Gizmos.color = passenger.isElectrolit ? Color.cyan : Color.white;
                    Gizmos.DrawWireSphere(passenger.spawnPosition, 1f);
                    
                    Gizmos.color = passenger.isLeftSide ? Color.red : Color.blue;
                    Gizmos.DrawSphere(passenger.spawnPosition + Vector3.up * 2f, 0.3f);
                }
            }
        }
    }
    
    void OnDestroy()
    {
        StopSpawning();
    }
}

// ========== COMPONENTE DE PASAJERO ==========

public class PassengerComponent : MonoBehaviour
{
    [Header("Passenger Config")]
    [SerializeField] private bool isElectrolit = false;
    [SerializeField] private bool isLeftSide = true;
    [SerializeField] private bool isPickedUp = false;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationSpeedVariation = 0.2f;
    
    [Header("Visual")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject electrolitVisual;
    [SerializeField] private ParticleSystem pickupEffect;
    
    public bool IsElectrolit => isElectrolit;
    public bool IsLeftSide => isLeftSide;
    public bool IsPickedUp => isPickedUp;
    
    void Start()
    {
        SetupVisuals();
        SetupAnimation();
    }
    
    public void Initialize(bool electrolit, bool leftSide)
    {
        isElectrolit = electrolit;
        isLeftSide = leftSide;
        isPickedUp = false;
        
        SetupVisuals();
        SetupAnimation();
    }
    
    private void SetupVisuals()
    {
        if (normalVisual != null)
            normalVisual.SetActive(!isElectrolit);
        
        if (electrolitVisual != null)
            electrolitVisual.SetActive(isElectrolit);
    }
    
    private void SetupAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f + Random.Range(-animationSpeedVariation, animationSpeedVariation);
            
            // Trigger apropiado según el lado
            string trigger = isLeftSide ? "WaveLeft" : "WaveRight";
            animator.SetTrigger(trigger);
        }
    }
    
    public void PerformPickup(int count)
    {
        if (isPickedUp) return;
        
        isPickedUp = true;
        
        // Efectos de pickup
        if (pickupEffect != null)
            pickupEffect.Play();
        
        if (animator != null)
            animator.SetTrigger("PickedUp");
        
        // El objeto será limpiado por el PassengerManager
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPickedUp)
        {
            // El pickup será manejado por PassengerManager basado en velocidad
        }
    }
}
