using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// COLLECTIBLE GENERATOR - Optimizado para ALTA VELOCIDAD
// ============================================
public class CollectibleGenerator : MonoBehaviour
{
    [Header("Collectible Generation")]
    public float collectibleSpacing = 15f;
    public float minCollectibleDistance = 8f;
    public int maxCollectiblesInMemory = 40; // INCREMENTADO de 30
    
    [Header("Performance Settings - SPEED OPTIMIZED")]
    public int baseMaxGenerationsPerFrame = 4; // INCREMENTADO de 2
    public int highSpeedMaxGenerationsPerFrame = 8; // NUEVO: Para alta velocidad
    public float speedThreshold = 15f; // NUEVO: Umbral de alta velocidad
    public int cleanupFrameInterval = 45; // INCREMENTADO para menos interrupciones
    public bool useAsyncGeneration = false; // CAMBIADO: Desactivar async para alta velocidad
    public float anticipationMultiplier = 2.5f; // NUEVO: Multiplicador de anticipación
    
    [Header("Generation Distance - DYNAMIC")]
    public float baseGenerationAhead = 120f; // NUEVO: Distancia base de generación anticipada
    public float maxGenerationAhead = 350f; // NUEVO: Máxima distancia de anticipación
    
    [Header("Generation Balance")]
    public float collectibleDensity = 0.7f;
    public bool avoidObstacleOverlap = true;
    public float obstacleAvoidanceRadius = 3f;
    
    [Header("Collectible Types")]
    public CollectibleData[] availableCollectibles;
    
    [Header("Difficulty Progression")]
    public bool increaseDifficulty = true;
    public float difficultyIncreaseRate = 0.05f;
    public AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f);
    
    [Header("Spawn Patterns")]
    public bool usePatterns = true;
    public CollectiblePattern[] collectiblePatterns;
    
    [Header("Power-Up Settings")]
    public float powerUpSpawnChance = 0.1f;
    public float minDistanceBetweenPowerUps = 50f;
    
    private SplineMathGenerator splineGenerator;
    private ObstacleGenerator obstacleGenerator;
    private List<GameObject> activeCollectibles = new List<GameObject>();
    private float lastCollectibleDistance = 30f;
    private float lastPowerUpDistance = 0f;
    private float currentDifficulty = 0f;
    
    // NUEVO: Variables de optimización para alta velocidad
    private int frameCounter = 0;
    private bool isGenerating = false;
    private Queue<float> pendingGenerations = new Queue<float>();
    private float currentMaxGenerationsPerFrame;
    private float currentGenerationAhead;
    
    // Cache de cálculos del spline
    private struct SplineCache
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 right;
        public float distance;
        public bool isValid;
    }
    private SplineCache lastSplineCache;
    
    [System.Serializable]
    public class CollectibleData
    {
        public GameObject prefab;
        public string collectibleName;
        public CollectibleCollision.CollectibleType type;
        public CollectibleBehavior behavior;
        public float minDifficulty = 0f;
        public float spawnWeight = 1f;
        public Vector3 positionOffset = Vector3.zero;
        public bool canBeInPattern = true;
        public int pointValue = 1;
    }
    
    [System.Serializable]
    public class CollectiblePattern
    {
        public string patternName;
        public PatternCollectible[] collectibles;
        public float minDifficulty = 0f;
        public float patternWeight = 1f;
        public bool isPowerUpPattern = false;
    }
    
    [System.Serializable]
    public class PatternCollectible
    {
        public int collectibleIndex;
        public Vector3 localPosition;
        public float delay = 0f;
    }
    
    public enum CollectibleBehavior
    {
        Static, Floating, Rotating, Pulsing, Orbiting, Combo
    }
    
    void Start()
    {
        Debug.Log("=== COLLECTIBLE GENERATOR STARTING (HIGH-SPEED OPTIMIZED) ===");
        
        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        if (splineGenerator == null)
        {
            Debug.LogError("CollectibleGenerator requires SplineMathGenerator in the scene!");
            return;
        }
        
        obstacleGenerator = FindObjectOfType<ObstacleGenerator>();
        ValidateCollectibleData();
        
        // Inicializar cache
        lastSplineCache = new SplineCache { isValid = false };
        
        // Inicializar configuraciones dinámicas
        currentMaxGenerationsPerFrame = baseMaxGenerationsPerFrame;
        currentGenerationAhead = baseGenerationAhead;
        
        Invoke(nameof(StartGeneratingCollectibles), 1f);
    }
    
    void Update()
    {
        frameCounter++;
        
        UpdateDifficulty();
        UpdatePerformanceSettings(); // NUEVO: Actualizar configuraciones basadas en velocidad
        
        // NUEVO: Generación optimizada para alta velocidad
        GenerateCollectiblesIfNeededOptimized();
        
        // Cleanup menos frecuente para mejor rendimiento
        if (frameCounter % cleanupFrameInterval == 0)
        {
            CleanupOldCollectibles();
        }
    }
    
    // NUEVO: Actualizar configuraciones basadas en velocidad del jugador
    void UpdatePerformanceSettings()
    {
        float playerDistance = splineGenerator.GetPlayerDistance();
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        
        if (player != null)
        {
            float playerSpeed = player.GetSpeed();
            bool isHighSpeed = playerSpeed > speedThreshold;
            
            // Ajustar generaciones por frame dinámicamente
            currentMaxGenerationsPerFrame = isHighSpeed ? 
                highSpeedMaxGenerationsPerFrame : baseMaxGenerationsPerFrame;
            
            // Ajustar distancia de generación anticipada
            float speedFactor = Mathf.Clamp01(playerSpeed / 20f); // Normalizar hasta velocidad máxima
            currentGenerationAhead = Mathf.Lerp(baseGenerationAhead, maxGenerationAhead, speedFactor);
        }
    }
    
    // NUEVO: Generación optimizada que reemplaza al sistema async problemático
    void GenerateCollectiblesIfNeededOptimized()
    {
        if (!splineGenerator.HasValidSpline()) return;
        
        float totalSplineLength = splineGenerator.GetTotalLength();
        float playerDistance = splineGenerator.GetPlayerDistance();
        
        // NUEVO: Calcular hasta dónde generar basado en velocidad del jugador
        float generateUntil = playerDistance + currentGenerationAhead;
        
        int generationsThisFrame = 0;
        
        // Procesar cola de generaciones pendientes primero
        while (pendingGenerations.Count > 0 && generationsThisFrame < currentMaxGenerationsPerFrame)
        {
            float distance = pendingGenerations.Dequeue();
            if (Random.value <= collectibleDensity)
            {
                GenerateNextCollectibleAtDistance(distance);
                generationsThisFrame++;
            }
        }
        
        // Generar nuevos coleccionables si es necesario
        while (lastCollectibleDistance < generateUntil && 
               lastCollectibleDistance < totalSplineLength - 40f && // REDUCIDO de 80f a 40f
               generationsThisFrame < currentMaxGenerationsPerFrame)
        {
            if (Random.value <= collectibleDensity)
            {
                GenerateNextCollectible();
                generationsThisFrame++;
            }
            else
            {
                // Avanzar sin generar
                lastCollectibleDistance += Random.Range(minCollectibleDistance, collectibleSpacing);
            }
        }
        
        // Debug para monitorear rendimiento
        if (generationsThisFrame > 0)
        {
            Debug.Log($"Generated {generationsThisFrame} collectibles. Player at {playerDistance:F1}, generating until {generateUntil:F1}");
        }
    }
    
    void ValidateCollectibleData()
    {
        for (int i = 0; i < availableCollectibles.Length; i++)
        {
            if (availableCollectibles[i].prefab == null)
            {
                Debug.LogWarning($"Collectible {i} has no prefab assigned!");
            }
        }
    }
    
    void StartGeneratingCollectibles()
    {
        Debug.Log("Starting high-speed optimized collectible generation...");
        
        // Generar coleccionables iniciales de forma síncrona
        for (int i = 0; i < 5; i++)
        {
            GenerateNextCollectible();
        }
    }
    
    void UpdateDifficulty()
    {
        if (!increaseDifficulty) return;
        
        float playerDistance = splineGenerator.GetPlayerDistance();
        float rawDifficulty = playerDistance * difficultyIncreaseRate / 100f;
        currentDifficulty = difficultyCurve.Evaluate(rawDifficulty);
    }
    
    void GenerateNextCollectible()
    {
        float nextDistance = lastCollectibleDistance + Random.Range(minCollectibleDistance, collectibleSpacing);
        GenerateNextCollectibleAtDistance(nextDistance);
        lastCollectibleDistance = nextDistance;
    }
    
    void GenerateNextCollectibleAtDistance(float distance)
    {
        if (avoidObstacleOverlap && IsPositionNearObstacle(distance))
        {
            return;
        }
        
        if (usePatterns && collectiblePatterns.Length > 0 && Random.value > 0.6f)
        {
            GenerateCollectiblePattern(distance);
        }
        else
        {
            GenerateSingleCollectible(distance);
        }
    }
    
    bool IsPositionNearObstacle(float distance)
    {
        if (obstacleGenerator == null) return false;
        // Simplificado para mejor rendimiento
        return Random.value < 0.2f;
    }
    
    void GenerateSingleCollectible(float distance)
    {
        CollectibleData selectedCollectible = SelectRandomCollectible();
        if (selectedCollectible?.prefab == null) return;
        
        CreateCollectibleAtDistance(selectedCollectible, distance, Vector3.zero);
    }
    
    void GenerateCollectiblePattern(float startDistance)
    {
        CollectiblePattern selectedPattern = SelectRandomPattern();
        if (selectedPattern == null) return;
        
        Debug.Log($"Generating collectible pattern: {selectedPattern.patternName} at distance {startDistance:F1}");
        
        foreach (var patternCollectible in selectedPattern.collectibles)
        {
            if (patternCollectible.collectibleIndex < availableCollectibles.Length)
            {
                CollectibleData collectibleData = availableCollectibles[patternCollectible.collectibleIndex];
                float collectibleDistance = startDistance + patternCollectible.delay;
                
                CreateCollectibleAtDistance(collectibleData, collectibleDistance, patternCollectible.localPosition);
            }
        }
    }
    
    CollectibleData SelectRandomCollectible()
    {
        List<CollectibleData> validCollectibles = new List<CollectibleData>();
        
        foreach (var collectible in availableCollectibles)
        {
            if (collectible.minDifficulty <= currentDifficulty && collectible.prefab != null)
            {
                if (collectible.type == CollectibleCollision.CollectibleType.PowerUp)
                {
                    float distanceSinceLastPowerUp = lastCollectibleDistance - lastPowerUpDistance;
                    if (distanceSinceLastPowerUp < minDistanceBetweenPowerUps)
                    {
                        continue;
                    }
                    
                    if (Random.value > powerUpSpawnChance)
                    {
                        continue;
                    }
                }
                
                for (int i = 0; i < Mathf.RoundToInt(collectible.spawnWeight * 10); i++)
                {
                    validCollectibles.Add(collectible);
                }
            }
        }
        
        return validCollectibles.Count > 0 ? validCollectibles[Random.Range(0, validCollectibles.Count)] : null;
    }
    
    CollectiblePattern SelectRandomPattern()
    {
        List<CollectiblePattern> validPatterns = new List<CollectiblePattern>();
        
        foreach (var pattern in collectiblePatterns)
        {
            if (pattern.minDifficulty <= currentDifficulty)
            {
                if (pattern.isPowerUpPattern)
                {
                    float distanceSinceLastPowerUp = lastCollectibleDistance - lastPowerUpDistance;
                    if (distanceSinceLastPowerUp < minDistanceBetweenPowerUps)
                    {
                        continue;
                    }
                }
                
                for (int i = 0; i < Mathf.RoundToInt(pattern.patternWeight * 10); i++)
                {
                    validPatterns.Add(pattern);
                }
            }
        }
        
        return validPatterns.Count > 0 ? validPatterns[Random.Range(0, validPatterns.Count)] : null;
    }
    
    void CreateCollectibleAtDistance(CollectibleData collectibleData, float distance, Vector3 patternOffset)
    {
        // Usar cache del spline si es posible
        Vector3 splinePosition, splineDirection, splineRight;
        
        if (lastSplineCache.isValid && Mathf.Abs(lastSplineCache.distance - distance) < 0.1f)
        {
            splinePosition = lastSplineCache.position;
            splineDirection = lastSplineCache.direction;
            splineRight = lastSplineCache.right;
        }
        else
        {
            splinePosition = splineGenerator.GetSplinePosition(distance);
            splineDirection = splineGenerator.GetSplineDirection(distance);
            splineRight = splineGenerator.GetSplineRight(distance);
            
            // Actualizar cache
            lastSplineCache = new SplineCache
            {
                position = splinePosition,
                direction = splineDirection,
                right = splineRight,
                distance = distance,
                isValid = true
            };
        }
        
        Vector3 finalPosition = splinePosition + collectibleData.positionOffset;
        
        if (patternOffset != Vector3.zero)
        {
            Vector3 localOffset = splineRight * patternOffset.x + 
                                Vector3.up * patternOffset.y + 
                                splineDirection * patternOffset.z;
            finalPosition += localOffset;
        }
        
        Quaternion rotation = Quaternion.LookRotation(splineDirection, Vector3.up);
        
        GameObject collectible = Instantiate(collectibleData.prefab, finalPosition, rotation);
        collectible.name = $"Collectible_{collectibleData.collectibleName}_{distance:F0}";
        
        ConfigureCollectible(collectible, collectibleData, distance);
        
        activeCollectibles.Add(collectible);
        
        if (collectibleData.type == CollectibleCollision.CollectibleType.PowerUp)
        {
            lastPowerUpDistance = distance;
        }
        
        Debug.Log($"Created collectible '{collectibleData.collectibleName}' at distance {distance:F1}, difficulty {currentDifficulty:F2}");
    }
    
    void ConfigureCollectible(GameObject collectible, CollectibleData data, float distance)
    {
        // Configuración optimizada - intentar obtener componentes existentes primero
        switch (data.behavior)
        {
            case CollectibleBehavior.Static:
                break;
                
            case CollectibleBehavior.Floating:
                if (collectible.GetComponent<FloatingCollectible>() == null)
                    collectible.AddComponent<FloatingCollectible>();
                break;
                
            case CollectibleBehavior.Rotating:
                if (collectible.GetComponent<RotatingCollectible>() == null)
                    collectible.AddComponent<RotatingCollectible>();
                break;
                
            case CollectibleBehavior.Pulsing:
                if (collectible.GetComponent<PulsingCollectible>() == null)
                    collectible.AddComponent<PulsingCollectible>();
                break;
                
            case CollectibleBehavior.Orbiting:
                OrbitingCollectible orbiting = collectible.GetComponent<OrbitingCollectible>();
                if (orbiting == null) orbiting = collectible.AddComponent<OrbitingCollectible>();
                orbiting.SetOrbitCenter(collectible.transform.position);
                break;
                
            case CollectibleBehavior.Combo:
                if (collectible.GetComponent<ComboCollectible>() == null)
                    collectible.AddComponent<ComboCollectible>();
                break;
        }
        
        CollectibleCollision collision = collectible.GetComponent<CollectibleCollision>();
        if (collision == null) collision = collectible.AddComponent<CollectibleCollision>();
        
        collision.SetupCollectible(data.type, data.pointValue, distance);
        
        Collider col = collectible.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphereCol = collectible.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.8f;
        }
        else
        {
            col.isTrigger = true;
        }
        
        collectible.tag = "Collectible";
    }
    
    void CleanupOldCollectibles()
    {
        if (activeCollectibles.Count > maxCollectiblesInMemory)
        {
            float playerDistance = splineGenerator.GetPlayerDistance();
            
            // Cleanup optimizado usando RemoveAll
            int initialCount = activeCollectibles.Count;
            
            activeCollectibles.RemoveAll(collectible =>
            {
                if (collectible == null) return true;
                
                CollectibleCollision collision = collectible.GetComponent<CollectibleCollision>();
                if (collision != null && collision.splineDistance < playerDistance - 80f) // INCREMENTADO de 60f a 80f
                {
                    Destroy(collectible);
                    return true;
                }
                return false;
            });
            
            int removedCount = initialCount - activeCollectibles.Count;
            if (removedCount > 0)
            {
                Debug.Log($"Cleaned up {removedCount} old collectibles");
            }
        }
    }
    
    // NUEVO: Método optimizado para invalidar cache cuando hay nuevo spline
    public void OnSplineUpdated()
    {
        Debug.Log("CollectibleGenerator: Spline updated, invalidating cache...");
        lastSplineCache = new SplineCache { isValid = false };
        
        // NUEVO: Forzar regeneración anticipada cuando hay nuevo spline a alta velocidad
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        if (player != null && player.GetSpeed() > speedThreshold)
        {
            StartCoroutine(ForceGenerationBurst());
        }
    }
    
    // NUEVO: Burst de generación para compensar nuevo spline a alta velocidad
    IEnumerator ForceGenerationBurst()
    {
        int burstGenerations = 0;
        int maxBurst = highSpeedMaxGenerationsPerFrame * 2;
        
        while (burstGenerations < maxBurst && splineGenerator.HasValidSpline())
        {
            float totalLength = splineGenerator.GetTotalLength();
            float playerDistance = splineGenerator.GetPlayerDistance();
            
            if (lastCollectibleDistance < playerDistance + currentGenerationAhead &&
                lastCollectibleDistance < totalLength - 40f)
            {
                if (Random.value <= collectibleDensity)
                {
                    GenerateNextCollectible();
                    burstGenerations++;
                }
                else
                {
                    lastCollectibleDistance += Random.Range(minCollectibleDistance, collectibleSpacing);
                }
            }
            else
            {
                break;
            }
            
            // Yield cada pocas generaciones para no causar lag
            if (burstGenerations % 3 == 0)
            {
                yield return null;
            }
        }
        
        Debug.Log($"Collectible generation burst completed: {burstGenerations} collectibles");
    }
    
    // Métodos públicos
    public float GetCurrentDifficulty()
    {
        return currentDifficulty;
    }
    
    public int GetActiveCollectibleCount()
    {
        return activeCollectibles.Count;
    }
    
    public void SetCollectibleDensity(float density)
    {
        collectibleDensity = Mathf.Clamp01(density);
        Debug.Log($"Collectible density set to {collectibleDensity:F2}");
    }
    
    public void SetPowerUpSpawnChance(float chance)
    {
        powerUpSpawnChance = Mathf.Clamp01(chance);
        Debug.Log($"Power-up spawn chance set to {powerUpSpawnChance:F2}");
    }
    
    // NUEVO: Métodos de debug de rendimiento
    public int GetPendingGenerationsCount()
    {
        return pendingGenerations.Count;
    }
    
    public bool IsCurrentlyGenerating()
    {
        return isGenerating;
    }
    
    public float GetCurrentGenerationAhead()
    {
        return currentGenerationAhead;
    }
    
    void OnDrawGizmosSelected()
    {
        if (splineGenerator == null || !splineGenerator.HasValidSpline()) return;
        
        Gizmos.color = Color.green;
        float totalLength = splineGenerator.GetTotalLength();
        
        for (float dist = lastCollectibleDistance; dist < totalLength && dist < lastCollectibleDistance + 60f; dist += collectibleSpacing)
        {
            Vector3 pos = splineGenerator.GetSplinePosition(dist);
            Gizmos.DrawWireSphere(pos, 1f);
        }
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Vector3 textPos = transform.position + Vector3.up * 7f;
            
            #if UNITY_EDITOR
            string info = $"Difficulty: {currentDifficulty:F2}\n";
            info += $"Collectibles: {activeCollectibles.Count}\n";
            info += $"Density: {collectibleDensity:F2}\n";
            info += $"Pending: {pendingGenerations.Count}\n";
            info += $"Gen Ahead: {currentGenerationAhead:F0}\n";
            info += $"Max/Frame: {currentMaxGenerationsPerFrame}";
            
            UnityEditor.Handles.Label(textPos, info);
            #endif
        }
    }
}