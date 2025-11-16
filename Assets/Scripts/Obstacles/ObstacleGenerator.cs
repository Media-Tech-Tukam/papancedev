using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// OBSTACLE GENERATOR - Optimizado para ALTA VELOCIDAD
// ============================================
public class ObstacleGenerator : MonoBehaviour
{
    [Header("Obstacle Generation")]
    public float obstacleSpacing = 25f;
    public float minObstacleDistance = 15f;
    public int maxObstaclesInMemory = 25; // INCREMENTADO de 20
    
    [Header("Performance Settings - SPEED OPTIMIZED")]
    public int baseMaxGenerationsPerFrame = 3; // INCREMENTADO de 1
    public int highSpeedMaxGenerationsPerFrame = 6; // NUEVO: Para alta velocidad
    public float speedThreshold = 15f; // NUEVO: Umbral de alta velocidad
    public int cleanupFrameInterval = 60; // INCREMENTADO para menos interrupciones
    public bool useAsyncGeneration = false; // CAMBIADO: Desactivar async para alta velocidad
    public float anticipationMultiplier = 3f; // NUEVO: Multiplicador de anticipación
    
    [Header("Generation Distance - DYNAMIC")]
    public float baseGenerationAhead = 150f; // NUEVO: Distancia base de generación anticipada
    public float maxGenerationAhead = 400f; // NUEVO: Máxima distancia de anticipación
    
    [Header("Obstacle Types")]
    public ObstacleData[] availableObstacles;
    
    [Header("Difficulty Progression")]
    public bool increaseDifficulty = true;
    public float difficultyIncreaseRate = 0.1f;
    public AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f);
    
    [Header("Spawn Patterns")]
    public bool usePatterns = true;
    public ObstaclePattern[] obstaclePatterns;
    
    [Header("Fatal Obstacle Settings")]
    [Range(0f, 1f)]
    public float fatalObstacleChance = 0.05f;
    public float minDistanceBeforeFatal = 200f;
    public float fatalObstacleSpacing = 100f;
    
    private SplineMathGenerator splineGenerator;
    private List<GameObject> activeObstacles = new List<GameObject>();
    private float lastObstacleDistance = 50f;
    private float lastFatalObstacleDistance = 0f;
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
    
    // Cache de validación de obstáculos letales
    private struct FatalObstacleValidation
    {
        public bool canGenerate;
        public float lastCheckDistance;
        public bool isValid;
    }
    private FatalObstacleValidation fatalCache;
    
    [System.Serializable]
    public class ObstacleData
    {
        public GameObject prefab;
        public string obstacleName;
        public ObstacleType type;
        public float minDifficulty = 0f;
        public float spawnWeight = 1f;
        public Vector3 positionOffset = Vector3.zero;
        public bool canBeInPattern = true;
        public bool isFatalObstacle = false;
    }
    
    [System.Serializable]
    public class ObstaclePattern
    {
        public string patternName;
        public PatternObstacle[] obstacles;
        public float minDifficulty = 0f;
        public float patternWeight = 1f;
        public bool containsFatalObstacle = false;
    }
    
    [System.Serializable]
    public class PatternObstacle
    {
        public int obstacleIndex;
        public Vector3 localPosition;
        public float delay = 0f;
    }
    
    public enum ObstacleType
    {
        Static, Moving, Rotating, Temporal, Fatal
    }
    
    void Start()
    {
        Debug.Log("=== OBSTACLE GENERATOR STARTING (HIGH-SPEED OPTIMIZED) ===");
        
        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        if (splineGenerator == null)
        {
            Debug.LogError("ObstacleGenerator requires SplineMathGenerator in the scene!");
            return;
        }
        
        ValidateObstacleData();
        
        // Inicializar caches
        lastSplineCache = new SplineCache { isValid = false };
        fatalCache = new FatalObstacleValidation { isValid = false };
        
        // Inicializar configuraciones dinámicas
        currentMaxGenerationsPerFrame = baseMaxGenerationsPerFrame;
        currentGenerationAhead = baseGenerationAhead;
        
        Invoke(nameof(StartGeneratingObstacles), 0.5f);
    }
    
    void Update()
    {
        frameCounter++;
        
        UpdateDifficulty();
        UpdatePerformanceSettings(); // NUEVO: Actualizar configuraciones basadas en velocidad
        
        // NUEVO: Generación optimizada para alta velocidad
        GenerateObstaclesIfNeededOptimized();
        
        // Cleanup menos frecuente para mejor rendimiento
        if (frameCounter % cleanupFrameInterval == 0)
        {
            CleanupOldObstacles();
        }
    }
    
    // NUEVO: Actualizar configuraciones basadas en velocidad del jugador
    void UpdatePerformanceSettings()
    {
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
    void GenerateObstaclesIfNeededOptimized()
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
            GenerateNextObstacleAtDistance(distance);
            generationsThisFrame++;
        }
        
        // Generar nuevos obstáculos si es necesario
        while (lastObstacleDistance < generateUntil && 
               lastObstacleDistance < totalSplineLength - 50f && // REDUCIDO de 100f a 50f
               generationsThisFrame < currentMaxGenerationsPerFrame)
        {
            GenerateNextObstacle();
            generationsThisFrame++;
        }
        
        // Debug para monitorear rendimiento
        if (generationsThisFrame > 0)
        {
            Debug.Log($"Generated {generationsThisFrame} obstacles. Player at {playerDistance:F1}, generating until {generateUntil:F1}");
        }
    }
    
    void ValidateObstacleData()
    {
        for (int i = 0; i < availableObstacles.Length; i++)
        {
            if (availableObstacles[i].prefab == null)
            {
                Debug.LogWarning($"Obstacle {i} has no prefab assigned!");
            }
            
            if (availableObstacles[i].isFatalObstacle)
            {
                availableObstacles[i].type = ObstacleType.Fatal;
                
                if (availableObstacles[i].minDifficulty < 0.3f)
                {
                    availableObstacles[i].minDifficulty = 0.3f;
                    Debug.LogWarning($"Fatal obstacle '{availableObstacles[i].obstacleName}' difficulty increased to 0.3f");
                }
                
                if (availableObstacles[i].spawnWeight > 0.3f)
                {
                    availableObstacles[i].spawnWeight = 0.3f;
                    Debug.LogWarning($"Fatal obstacle '{availableObstacles[i].obstacleName}' spawn weight reduced to 0.3f");
                }
            }
        }
    }
    
    void StartGeneratingObstacles()
    {
        Debug.Log("Starting high-speed optimized obstacle generation...");
        
        // Generar obstáculos iniciales de forma síncrona
        for (int i = 0; i < 3; i++)
        {
            GenerateNextObstacle();
        }
    }
    
    void UpdateDifficulty()
    {
        if (!increaseDifficulty) return;
        
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        if (player != null)
        {
            float playerDistance = player.GetCurrentDistance();
            float rawDifficulty = playerDistance * difficultyIncreaseRate / 100f;
            currentDifficulty = difficultyCurve.Evaluate(rawDifficulty);
        }
    }
    
    void GenerateNextObstacle()
    {
        float nextDistance = lastObstacleDistance + Random.Range(minObstacleDistance, obstacleSpacing);
        GenerateNextObstacleAtDistance(nextDistance);
        lastObstacleDistance = nextDistance;
    }
    
    void GenerateNextObstacleAtDistance(float distance)
    {
        // Usar cache para validación de obstáculos letales
        bool shouldGenerateFatal = ShouldGenerateFatalObstacleCached(distance);
        
        if (shouldGenerateFatal)
        {
            GenerateFatalObstacle(distance);
        }
        else
        {
            if (usePatterns && obstaclePatterns.Length > 0 && Random.value > 0.7f)
            {
                GenerateObstaclePattern(distance);
            }
            else
            {
                GenerateSingleObstacle(distance, false);
            }
        }
    }
    
    bool ShouldGenerateFatalObstacleCached(float distance)
    {
        // Usar cache si la distancia es similar
        if (fatalCache.isValid && Mathf.Abs(fatalCache.lastCheckDistance - distance) < 10f)
        {
            return fatalCache.canGenerate;
        }
        
        // Calcular nueva validación
        bool canGenerate = ShouldGenerateFatalObstacle(distance);
        
        // Actualizar cache
        fatalCache = new FatalObstacleValidation
        {
            canGenerate = canGenerate,
            lastCheckDistance = distance,
            isValid = true
        };
        
        return canGenerate;
    }
    
    bool ShouldGenerateFatalObstacle(float distance)
    {
        if (distance < minDistanceBeforeFatal)
            return false;
        
        if (distance - lastFatalObstacleDistance < fatalObstacleSpacing)
            return false;
        
        if (currentDifficulty < 0.3f)
            return false;
        
        if (!HasAvailableFatalObstacles())
            return false;
        
        float adjustedChance = fatalObstacleChance * currentDifficulty;
        return Random.value < adjustedChance;
    }
    
    bool HasAvailableFatalObstacles()
    {
        foreach (var obstacle in availableObstacles)
        {
            if (obstacle.isFatalObstacle && obstacle.minDifficulty <= currentDifficulty && obstacle.prefab != null)
            {
                return true;
            }
        }
        return false;
    }
    
    void GenerateFatalObstacle(float distance)
    {
        ObstacleData selectedObstacle = SelectRandomFatalObstacle();
        if (selectedObstacle?.prefab == null) return;
        
        CreateObstacleAtDistance(selectedObstacle, distance, Vector3.zero);
        lastFatalObstacleDistance = distance;
        
        Debug.Log($"💀 FATAL OBSTACLE generated at distance {distance:F1}! Type: {selectedObstacle.obstacleName}");
    }
    
    ObstacleData SelectRandomFatalObstacle()
    {
        List<ObstacleData> validFatalObstacles = new List<ObstacleData>();
        
        foreach (var obstacle in availableObstacles)
        {
            if (obstacle.isFatalObstacle && obstacle.minDifficulty <= currentDifficulty && obstacle.prefab != null)
            {
                for (int i = 0; i < Mathf.RoundToInt(obstacle.spawnWeight * 10); i++)
                {
                    validFatalObstacles.Add(obstacle);
                }
            }
        }
        
        return validFatalObstacles.Count > 0 ? validFatalObstacles[Random.Range(0, validFatalObstacles.Count)] : null;
    }
    
    void GenerateSingleObstacle(float distance, bool allowFatal = false)
    {
        ObstacleData selectedObstacle = SelectRandomObstacle(allowFatal);
        if (selectedObstacle?.prefab == null) return;
        
        CreateObstacleAtDistance(selectedObstacle, distance, Vector3.zero);
    }
    
    void GenerateObstaclePattern(float startDistance)
    {
        ObstaclePattern selectedPattern = SelectRandomPattern();
        if (selectedPattern == null) return;
        
        Debug.Log($"Generating pattern: {selectedPattern.patternName} at distance {startDistance:F1}");
        
        foreach (var patternObstacle in selectedPattern.obstacles)
        {
            if (patternObstacle.obstacleIndex < availableObstacles.Length)
            {
                ObstacleData obstacleData = availableObstacles[patternObstacle.obstacleIndex];
                float obstacleDistance = startDistance + patternObstacle.delay;
                
                CreateObstacleAtDistance(obstacleData, obstacleDistance, patternObstacle.localPosition);
            }
        }
    }
    
    ObstacleData SelectRandomObstacle(bool allowFatal = false)
    {
        List<ObstacleData> validObstacles = new List<ObstacleData>();
        
        foreach (var obstacle in availableObstacles)
        {
            if (obstacle.isFatalObstacle && !allowFatal)
                continue;
            
            if (obstacle.minDifficulty <= currentDifficulty && obstacle.prefab != null)
            {
                for (int i = 0; i < Mathf.RoundToInt(obstacle.spawnWeight * 10); i++)
                {
                    validObstacles.Add(obstacle);
                }
            }
        }
        
        return validObstacles.Count > 0 ? validObstacles[Random.Range(0, validObstacles.Count)] : null;
    }
    
    ObstaclePattern SelectRandomPattern()
    {
        List<ObstaclePattern> validPatterns = new List<ObstaclePattern>();
        
        foreach (var pattern in obstaclePatterns)
        {
            if (pattern.containsFatalObstacle)
                continue;
            
            if (pattern.minDifficulty <= currentDifficulty)
            {
                for (int i = 0; i < Mathf.RoundToInt(pattern.patternWeight * 10); i++)
                {
                    validPatterns.Add(pattern);
                }
            }
        }
        
        return validPatterns.Count > 0 ? validPatterns[Random.Range(0, validPatterns.Count)] : null;
    }
    
    void CreateObstacleAtDistance(ObstacleData obstacleData, float distance, Vector3 patternOffset)
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
        
        Vector3 finalPosition = splinePosition + obstacleData.positionOffset;
        
        if (patternOffset != Vector3.zero)
        {
            Vector3 localOffset = splineRight * patternOffset.x + 
                                Vector3.up * patternOffset.y + 
                                splineDirection * patternOffset.z;
            finalPosition += localOffset;
        }
        
        Quaternion rotation = Quaternion.LookRotation(splineDirection, Vector3.up);
        
        GameObject obstacle = Instantiate(obstacleData.prefab, finalPosition, rotation);
        obstacle.name = $"Obstacle_{obstacleData.obstacleName}_{distance:F0}";
        
        if (obstacleData.isFatalObstacle)
        {
            ConfigureFatalObstacle(obstacle, obstacleData, distance);
        }
        else
        {
            ConfigureObstacle(obstacle, obstacleData, distance);
        }
        
        activeObstacles.Add(obstacle);
        
        string obstacleTypeText = obstacleData.isFatalObstacle ? "💀 FATAL" : "normal";
        Debug.Log($"Created {obstacleTypeText} obstacle '{obstacleData.obstacleName}' at distance {distance:F1}, difficulty {currentDifficulty:F2}");
    }
    
    void ConfigureFatalObstacle(GameObject obstacle, ObstacleData data, float distance)
    {
        // Configuración optimizada - verificar componentes existentes primero
        switch (data.type)
        {
            case ObstacleType.Fatal:
                break;
                
            case ObstacleType.Moving:
                MovingObstacle moving = obstacle.GetComponent<MovingObstacle>();
                if (moving == null) moving = obstacle.AddComponent<MovingObstacle>();
                moving.moveSpeed *= 0.8f;
                break;
                
            case ObstacleType.Rotating:
                RotatingObstacle rotating = obstacle.GetComponent<RotatingObstacle>();
                if (rotating == null) rotating = obstacle.AddComponent<RotatingObstacle>();
                rotating.rotationSpeed *= 0.7f;
                break;
                
            case ObstacleType.Temporal:
                TemporalObstacle temporal = obstacle.GetComponent<TemporalObstacle>();
                if (temporal == null) temporal = obstacle.AddComponent<TemporalObstacle>();
                temporal.visibleTime = Mathf.Max(temporal.visibleTime, 3f);
                break;
        }
        
        ObstacleCollision collision = obstacle.GetComponent<ObstacleCollision>();
        if (collision == null) collision = obstacle.AddComponent<ObstacleCollision>();
        
        collision.effectType = ObstacleCollision.ObstacleEffect.GameOver;
        collision.splineDistance = distance;
        collision.destroyOnCollision = true;
        
        Collider col = obstacle.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = obstacle.AddComponent<BoxCollider>();
            boxCol.isTrigger = false;
        }
        
        ConfigureFatalVisuals(obstacle);
        obstacle.tag = "FatalObstacle";
    }
    
    void ConfigureFatalVisuals(GameObject obstacle)
    {
        Renderer[] renderers = obstacle.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                renderer.material.color = Color.red;
            }
            
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.red * 0.5f);
            }
        }
        
        // Solo agregar partículas si no existen ya
        if (obstacle.GetComponentInChildren<ParticleSystem>() == null)
        {
            GameObject warningEffect = new GameObject("WarningEffect");
            warningEffect.transform.SetParent(obstacle.transform);
            warningEffect.transform.localPosition = Vector3.zero;
            
            ParticleSystem particles = warningEffect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = Color.red;
            main.startSize = 0.1f;
            main.startLifetime = 1f;
            main.maxParticles = 20;
            
            var emission = particles.emission;
            emission.rateOverTime = 10f;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2f;
        }
        
        // Audio solo si no existe
        if (obstacle.GetComponent<AudioSource>() == null)
        {
            AudioSource audioSource = obstacle.AddComponent<AudioSource>();
            audioSource.volume = 0.3f;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
        }
    }
    
    void ConfigureObstacle(GameObject obstacle, ObstacleData data, float distance)
    {
        // Configuración optimizada
        switch (data.type)
        {
            case ObstacleType.Static:
                break;
                
            case ObstacleType.Moving:
                if (obstacle.GetComponent<MovingObstacle>() == null)
                    obstacle.AddComponent<MovingObstacle>();
                break;
                
            case ObstacleType.Rotating:
                if (obstacle.GetComponent<RotatingObstacle>() == null)
                    obstacle.AddComponent<RotatingObstacle>();
                break;
                
            case ObstacleType.Temporal:
                if (obstacle.GetComponent<TemporalObstacle>() == null)
                    obstacle.AddComponent<TemporalObstacle>();
                break;
        }
        
        ObstacleCollision collision = obstacle.GetComponent<ObstacleCollision>();
        if (collision == null) collision = obstacle.AddComponent<ObstacleCollision>();
        collision.splineDistance = distance;
        
        Collider col = obstacle.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = obstacle.AddComponent<BoxCollider>();
            boxCol.isTrigger = false;
        }
        
        obstacle.tag = "Obstacle";
    }
    
    void CleanupOldObstacles()
    {
        if (activeObstacles.Count > maxObstaclesInMemory)
        {
            ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
            float playerDistance = player != null ? player.GetCurrentDistance() : 0f;
            
            // Cleanup optimizado usando RemoveAll
            int initialCount = activeObstacles.Count;
            
            activeObstacles.RemoveAll(obstacle =>
            {
                if (obstacle == null) return true;
                
                ObstacleCollision collision = obstacle.GetComponent<ObstacleCollision>();
                if (collision != null && collision.splineDistance < playerDistance - 75f) // REDUCIDO de 50f a 75f
                {
                    Destroy(obstacle);
                    return true;
                }
                return false;
            });
            
            int removedCount = initialCount - activeObstacles.Count;
            if (removedCount > 0)
            {
                Debug.Log($"Cleaned up {removedCount} old obstacles");
            }
        }
    }
    
    // Métodos públicos
    public float GetCurrentDifficulty()
    {
        return currentDifficulty;
    }
    
    public int GetActiveObstacleCount()
    {
        return activeObstacles.Count;
    }
    
    public int GetActiveFatalObstacleCount()
    {
        int count = 0;
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null && obstacle.CompareTag("FatalObstacle"))
                count++;
        }
        return count;
    }
    
    public float GetLastFatalObstacleDistance()
    {
        return lastFatalObstacleDistance;
    }
    
    public void SetFatalObstacleChance(float newChance)
    {
        fatalObstacleChance = Mathf.Clamp01(newChance);
        // Invalidar cache cuando cambian las reglas
        fatalCache = new FatalObstacleValidation { isValid = false };
    }
    
    public void ForceFatalObstacle()
    {
        if (splineGenerator.HasValidSpline())
        {
            float nextDistance = lastObstacleDistance + minObstacleDistance;
            GenerateFatalObstacle(nextDistance);
            lastObstacleDistance = nextDistance;
        }
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
    
    // NUEVO: Método para invalidar caches (llamado desde SplineMathGenerator)
    public void InvalidateCaches()
    {
        lastSplineCache = new SplineCache { isValid = false };
        fatalCache = new FatalObstacleValidation { isValid = false };
        
        // NUEVO: Forzar burst de generación si el jugador va rápido
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
            
            if (lastObstacleDistance < playerDistance + currentGenerationAhead &&
                lastObstacleDistance < totalLength - 50f)
            {
                GenerateNextObstacle();
                burstGenerations++;
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
        
        Debug.Log($"Obstacle generation burst completed: {burstGenerations} obstacles");
    }
    
    void OnDrawGizmosSelected()
    {
        if (splineGenerator == null || !splineGenerator.HasValidSpline()) return;
        
        Gizmos.color = Color.red;
        float totalLength = splineGenerator.GetTotalLength();
        
        for (float dist = lastObstacleDistance; dist < totalLength && dist < lastObstacleDistance + 100f; dist += obstacleSpacing)
        {
            Vector3 pos = splineGenerator.GetSplinePosition(dist);
            Gizmos.DrawWireCube(pos, Vector3.one * 2f);
        }
        
        if (lastFatalObstacleDistance > 0f)
        {
            Gizmos.color = Color.black;
            Vector3 fatalPos = splineGenerator.GetSplinePosition(lastFatalObstacleDistance);
            Gizmos.DrawWireSphere(fatalPos, 3f);
        }
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 textPos = transform.position + Vector3.up * 5f;
            
            #if UNITY_EDITOR
            string infoText = $"Difficulty: {currentDifficulty:F2}\n" +
                             $"Obstacles: {activeObstacles.Count}\n" +
                             $"Fatal Chance: {fatalObstacleChance:P1}\n" +
                             $"Fatal Count: {GetActiveFatalObstacleCount()}\n" +
                             $"Pending: {pendingGenerations.Count}\n" +
                             $"Gen Ahead: {currentGenerationAhead:F0}\n" +
                             $"Max/Frame: {currentMaxGenerationsPerFrame}";
            UnityEditor.Handles.Label(textPos, infoText);
            #endif
        }
    }
}