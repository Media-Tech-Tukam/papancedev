using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// TRACK VISUALIZER - Optimizado para ALTA VELOCIDAD
// ============================================
public class TrackVisualizer : MonoBehaviour
{
    [Header("Visual Configuration")]
    [SerializeField] private TrackPrefabData[] trackPrefabs;
    public float prefabSpacing = 2f;
    public int maxPrefabsInMemory = 150; // INCREMENTADO de 100
    
    [Header("Performance Settings - SPEED OPTIMIZED")]
    public int baseMaxPrefabsPerFrame = 4; // INCREMENTADO de 3
    public int highSpeedMaxPrefabsPerFrame = 8; // NUEVO: Para alta velocidad
    public float speedThreshold = 15f; // NUEVO: Umbral de alta velocidad
    public int cleanupFrameInterval = 90; // INCREMENTADO para menos interrupciones
    public bool useAsyncGeneration = false; // CAMBIADO: Desactivar async para alta velocidad
    public bool usePrefabBatching = true;
    
    [Header("Generation Distance - DYNAMIC")]
    public float baseGenerationAhead = 100f; // NUEVO: Distancia base de generación anticipada
    public float maxGenerationAhead = 300f; // NUEVO: Máxima distancia de anticipación
    public float cleanupSafetyDistance = 120f; // NUEVO: Distancia de seguridad para cleanup
    
    [Header("Segment Prefab Selection")]
    public bool useRandomSelection = true;
    public bool avoidConsecutiveSegments = true;
    [Range(0f, 100f)]
    public float globalWeightMultiplier = 1f;
    
    [Header("Prefab Alignment")]
    public bool alignToSpline = true;
    public bool adjustHeight = true;
    public float heightOffset = 0f;
    
    [Header("Triggers")]
    public bool addTriggers = true;
    public float triggerSpacing = 50f;
    
    private SplineMathGenerator splineGenerator;
    private List<GameObject> instantiatedPrefabs = new List<GameObject>();
    private Dictionary<int, int> segmentToPrefabIndex = new Dictionary<int, int>();
    private float lastPrefabDistance = 0f;
    private float lastTriggerDistance = 0f;
    private int lastProcessedSegment = -1;
    private int lastUsedPrefabIndex = -1;
    
    // NUEVO: Variables de optimización para alta velocidad
    private int frameCounter = 0;
    private bool isGenerating = false;
    private Queue<float> pendingPrefabDistances = new Queue<float>();
    private Queue<float> pendingTriggerDistances = new Queue<float>();
    private float currentMaxPrefabsPerFrame;
    private float currentGenerationAhead;
    
    // Cache de configuración de prefabs
    private Dictionary<GameObject, PrefabConfiguration> prefabConfigCache = new Dictionary<GameObject, PrefabConfiguration>();
    
    // NUEVO: Tracking de distancias para cleanup seguro
    private List<PrefabDistanceInfo> prefabDistanceTracker = new List<PrefabDistanceInfo>();
    
    [System.Serializable]
    public class TrackPrefabData
    {
        public GameObject prefab;
        public string name = "Track Variant";
        [Range(0f, 100f)]
        public float weight = 1f;
        [TextArea(2, 3)]
        public string description = "";
    }
    
    private struct PrefabConfiguration
    {
        public bool hasRigidbody;
        public bool hasValidColliders;
        public bool needsConfiguration;
        public Bounds bounds;
    }
    
    // NUEVO: Estructura para tracking de distancias de prefabs
    private struct PrefabDistanceInfo
    {
        public GameObject prefab;
        public float distance;
        public int segmentIndex;
    }
    
    void Start()
    {
        Debug.Log("=== TRACK VISUALIZER STARTING (HIGH-SPEED OPTIMIZED) ===");
        
        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        if (splineGenerator == null)
        {
            Debug.LogError("TrackVisualizer requires SplineMathGenerator in the scene!");
            return;
        }
        
        if (!ValidatePrefabs())
        {
            Debug.LogError("No valid track prefabs assigned!");
            return;
        }
        
        // Pre-cachear configuraciones de prefabs
        PreCachePrefabConfigurations();
        
        // Inicializar configuraciones dinámicas
        currentMaxPrefabsPerFrame = baseMaxPrefabsPerFrame;
        currentGenerationAhead = baseGenerationAhead;
        
        Invoke(nameof(GenerateInitialPrefabs), 0.1f);
    }
    
    void Update()
    {
        frameCounter++;
        
        UpdatePerformanceSettings(); // NUEVO: Actualizar configuraciones basadas en velocidad
        
        // NUEVO: Generación optimizada para alta velocidad
        GeneratePrefabsIfNeededOptimized();
        
        // Cleanup menos frecuente y más inteligente
        if (frameCounter % cleanupFrameInterval == 0)
        {
            CleanupOldPrefabsSafe(); // NUEVO: Cleanup seguro
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
            currentMaxPrefabsPerFrame = isHighSpeed ? 
                highSpeedMaxPrefabsPerFrame : baseMaxPrefabsPerFrame;
            
            // Ajustar distancia de generación anticipada
            float speedFactor = Mathf.Clamp01(playerSpeed / 20f);
            currentGenerationAhead = Mathf.Lerp(baseGenerationAhead, maxGenerationAhead, speedFactor);
        }
    }
    
    // NUEVO: Generación optimizada que reemplaza al sistema async problemático
    void GeneratePrefabsIfNeededOptimized()
    {
        if (!splineGenerator.HasValidSpline()) return;
        
        float totalSplineLength = splineGenerator.GetTotalLength();
        float playerDistance = splineGenerator.GetPlayerDistance();
        
        // NUEVO: Calcular hasta dónde generar basado en velocidad del jugador
        float generateUntil = playerDistance + currentGenerationAhead;
        
        int prefabsThisFrame = 0;
        
        // Procesar cola de generaciones pendientes primero
        while (pendingPrefabDistances.Count > 0 && prefabsThisFrame < currentMaxPrefabsPerFrame)
        {
            float distance = pendingPrefabDistances.Dequeue();
            CreatePrefabAtDistance(distance);
            prefabsThisFrame++;
        }
        
        // Generar nuevos prefabs si es necesario
        while (lastPrefabDistance < generateUntil && 
               lastPrefabDistance < totalSplineLength - 30f && // REDUCIDO de 50f para generar más cerca del final
               prefabsThisFrame < currentMaxPrefabsPerFrame)
        {
            CreatePrefabAtDistance(lastPrefabDistance);
            lastPrefabDistance += prefabSpacing;
            prefabsThisFrame++;
        }
        
        // Generar triggers con menos frecuencia
        if (addTriggers && frameCounter % 60 == 0) // Menos frecuente
        {
            while (lastTriggerDistance < generateUntil && 
                   lastTriggerDistance < totalSplineLength - triggerSpacing)
            {
                lastTriggerDistance += triggerSpacing;
                AddTriggerAtDistance(lastTriggerDistance);
                break; // Solo uno por vez
            }
        }
        
        // Debug para monitorear rendimiento
        if (prefabsThisFrame > 0)
        {
            Debug.Log($"Generated {prefabsThisFrame} track prefabs. Player at {playerDistance:F1}, generating until {generateUntil:F1}");
        }
    }
    
    void PreCachePrefabConfigurations()
    {
        foreach (var prefabData in trackPrefabs)
        {
            if (prefabData.prefab != null)
            {
                PrefabConfiguration config = new PrefabConfiguration
                {
                    hasRigidbody = prefabData.prefab.GetComponent<Rigidbody>() != null,
                    hasValidColliders = prefabData.prefab.GetComponentsInChildren<Collider>().Length > 0,
                    needsConfiguration = true
                };
                
                // Calcular bounds aproximados
                Renderer[] renderers = prefabData.prefab.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }
                    config.bounds = bounds;
                }
                
                prefabConfigCache[prefabData.prefab] = config;
            }
        }
        
        Debug.Log($"Pre-cached configurations for {prefabConfigCache.Count} prefabs");
    }
    
    bool ValidatePrefabs()
    {
        if (trackPrefabs == null || trackPrefabs.Length == 0)
        {
            Debug.LogError("No track prefabs assigned!");
            return false;
        }
        
        int validPrefabs = 0;
        for (int i = 0; i < trackPrefabs.Length; i++)
        {
            if (trackPrefabs[i].prefab != null && trackPrefabs[i].weight > 0)
            {
                validPrefabs++;
            }
            else
            {
                Debug.LogWarning($"TrackPrefab [{i}] is invalid (null prefab or zero weight)");
            }
        }
        
        Debug.Log($"Found {validPrefabs} valid track prefabs out of {trackPrefabs.Length}");
        return validPrefabs > 0;
    }
    
    void GenerateInitialPrefabs()
    {
        if (!splineGenerator.HasValidSpline())
        {
            Debug.LogWarning("No valid spline found for visualization");
            return;
        }
        
        Debug.Log("Generating initial track prefabs optimized...");
        
        // Generar prefabs iniciales de forma síncrona
        float totalLength = splineGenerator.GetTotalLength();
        int initialPrefabs = 0;
        
        while (lastPrefabDistance < totalLength && initialPrefabs < 30) // Más prefabs iniciales
        {
            CreatePrefabAtDistance(lastPrefabDistance);
            lastPrefabDistance += prefabSpacing;
            initialPrefabs++;
        }
        
        Debug.Log($"Generated {initialPrefabs} initial prefabs");
    }
    
    public void OnSplineUpdated()
    {
        Debug.Log("Spline updated, generating new prefabs optimized...");
        
        // NUEVO: Forzar burst de generación si el jugador va rápido
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        if (player != null && player.GetSpeed() > speedThreshold)
        {
            StartCoroutine(ForceGenerationBurst());
        }
        else
        {
            // Generación normal para velocidad baja
            GeneratePrefabsIfNeededOptimized();
        }
    }
    
    // NUEVO: Burst de generación para compensar nuevo spline a alta velocidad
    IEnumerator ForceGenerationBurst()
    {
        int burstPrefabs = 0;
        int maxBurst = highSpeedMaxPrefabsPerFrame * 3;
        
        while (burstPrefabs < maxBurst && splineGenerator.HasValidSpline())
        {
            float totalLength = splineGenerator.GetTotalLength();
            float playerDistance = splineGenerator.GetPlayerDistance();
            
            if (lastPrefabDistance < playerDistance + currentGenerationAhead &&
                lastPrefabDistance < totalLength - 30f)
            {
                CreatePrefabAtDistance(lastPrefabDistance);
                lastPrefabDistance += prefabSpacing;
                burstPrefabs++;
            }
            else
            {
                break;
            }
            
            // Yield cada pocos prefabs para no causar lag
            if (burstPrefabs % 4 == 0)
            {
                yield return null;
            }
        }
        
        Debug.Log($"Track prefab generation burst completed: {burstPrefabs} prefabs");
    }
    
    void CreatePrefabAtDistance(float distance)
    {
        int currentSegmentIndex = GetSegmentIndexAtDistance(distance);
        
        if (!segmentToPrefabIndex.ContainsKey(currentSegmentIndex))
        {
            int selectedPrefabIndex = SelectPrefabForSegment(currentSegmentIndex);
            segmentToPrefabIndex[currentSegmentIndex] = selectedPrefabIndex;
            
            Debug.Log($"NEW SEGMENT {currentSegmentIndex}: Selected prefab '{trackPrefabs[selectedPrefabIndex].name}'");
        }
        
        int prefabIndex = segmentToPrefabIndex[currentSegmentIndex];
        TrackPrefabData selectedPrefabData = trackPrefabs[prefabIndex];
        
        Vector3 splinePosition = splineGenerator.GetSplinePosition(distance);
        Vector3 splineDirection = splineGenerator.GetSplineDirection(distance);
        
        if (adjustHeight)
        {
            splinePosition.y += heightOffset;
        }
        
        Quaternion rotation = Quaternion.identity;
        if (alignToSpline && splineDirection != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(splineDirection, Vector3.up);
        }
        
        GameObject prefab = Instantiate(selectedPrefabData.prefab, splinePosition, rotation);
        prefab.name = $"TrackPrefab_Seg{currentSegmentIndex}_{selectedPrefabData.name}_Dist_{distance:F1}";
        
        // Configuración optimizada usando cache
        ConfigurePrefabOptimized(prefab, selectedPrefabData.prefab, distance);
        
        instantiatedPrefabs.Add(prefab);
        
        // NUEVO: Agregar al tracker de distancias para cleanup seguro
        prefabDistanceTracker.Add(new PrefabDistanceInfo
        {
            prefab = prefab,
            distance = distance,
            segmentIndex = currentSegmentIndex
        });
    }
    
    int GetSegmentIndexAtDistance(float distance)
    {
        float approximateSegmentLength = splineGenerator.segmentsPerTrack * splineGenerator.segmentLength;
        int segmentIndex = Mathf.FloorToInt(distance / approximateSegmentLength);
        return segmentIndex;
    }
    
    int SelectPrefabForSegment(int segmentIndex)
    {
        if (!useRandomSelection)
        {
            for (int i = 0; i < trackPrefabs.Length; i++)
            {
                if (trackPrefabs[i].prefab != null && trackPrefabs[i].weight > 0)
                    return i;
            }
            return 0;
        }
        
        List<int> validIndices = new List<int>();
        List<float> weights = new List<float>();
        
        for (int i = 0; i < trackPrefabs.Length; i++)
        {
            TrackPrefabData prefabData = trackPrefabs[i];
            
            if (prefabData.prefab == null || prefabData.weight <= 0)
                continue;
            
            if (avoidConsecutiveSegments && i == lastUsedPrefabIndex && segmentIndex > 0)
                continue;
            
            validIndices.Add(i);
            weights.Add(prefabData.weight * globalWeightMultiplier);
        }
        
        if (validIndices.Count == 0)
        {
            Debug.LogWarning($"No valid prefab candidates for segment {segmentIndex}, using fallback selection");
            for (int i = 0; i < trackPrefabs.Length; i++)
            {
                if (trackPrefabs[i].prefab != null)
                {
                    lastUsedPrefabIndex = i;
                    return i;
                }
            }
            return 0;
        }
        
        int selectedIndex = WeightedRandomSelection(validIndices, weights);
        lastUsedPrefabIndex = selectedIndex;
        return selectedIndex;
    }
    
    int WeightedRandomSelection(List<int> indices, List<float> weights)
    {
        float totalWeight = 0f;
        foreach (float weight in weights)
            totalWeight += weight;
        
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        for (int i = 0; i < indices.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
                return indices[i];
        }
        
        return indices[indices.Count - 1];
    }
    
    void ConfigurePrefabOptimized(GameObject instance, GameObject originalPrefab, float distance)
    {
        if (!prefabConfigCache.TryGetValue(originalPrefab, out PrefabConfiguration config))
        {
            // Si no está en cache, configurar normalmente (fallback)
            ConfigurePrefab(instance, distance);
            return;
        }
        
        // Solo hacer las configuraciones necesarias basadas en el cache
        if (config.hasRigidbody)
        {
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
            }
        }
        
        // Solo verificar colliders si el cache indica que es necesario
        if (!config.hasValidColliders)
        {
            // Agregar BoxCollider solo si no hay colliders válidos
            BoxCollider boxCollider = instance.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(5f, 0.1f, prefabSpacing);
        }
        else
        {
            // Configuración rápida de colliders existentes
            MeshCollider[] meshColliders = instance.GetComponentsInChildren<MeshCollider>();
            foreach (MeshCollider meshCollider in meshColliders)
            {
                if (!meshCollider.convex)
                {
                    meshCollider.convex = true;
                }
            }
        }
        
        // Configurar layer si es necesario
        if (instance.layer == 0)
        {
            instance.layer = LayerMask.NameToLayer("Default");
        }
    }
    
    void ConfigurePrefab(GameObject prefab, float distance)
    {
        // Versión original para fallback
        Rigidbody rb = prefab.GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }
        
        MeshCollider[] meshColliders = prefab.GetComponentsInChildren<MeshCollider>();
        foreach (MeshCollider meshCollider in meshColliders)
        {
            if (!meshCollider.convex)
            {
                meshCollider.convex = true;
            }
        }
        
        Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            BoxCollider boxCollider = prefab.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(5f, 0.1f, prefabSpacing);
        }
        
        if (prefab.layer == 0)
        {
            prefab.layer = LayerMask.NameToLayer("Default");
        }
    }
    
    void AddTriggerAtDistance(float distance)
    {
        // Búsqueda optimizada del prefab más cercano
        GameObject closestPrefab = FindClosestPrefabToDistance(distance);
        
        if (closestPrefab != null)
        {
            // Solo agregar trigger si no existe ya
            if (closestPrefab.GetComponent<SplineTrigger>() == null)
            {
                BoxCollider trigger = closestPrefab.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(5f, 2f, prefabSpacing);
                
                SplineTrigger triggerScript = closestPrefab.AddComponent<SplineTrigger>();
                triggerScript.splineGenerator = splineGenerator;
                triggerScript.triggerDistance = distance;
                
                Debug.Log($"Added trigger at distance {distance:F1}");
            }
        }
    }
    
    GameObject FindClosestPrefabToDistance(float distance)
    {
        GameObject closestPrefab = null;
        float closestDistance = float.MaxValue;
        Vector3 splinePosition = splineGenerator.GetSplinePosition(distance);
        
        // Solo verificar los últimos N prefabs para eficiencia
        int startIndex = Mathf.Max(0, instantiatedPrefabs.Count - 30);
        
        for (int i = startIndex; i < instantiatedPrefabs.Count; i++)
        {
            if (instantiatedPrefabs[i] != null)
            {
                Vector3 prefabPosition = instantiatedPrefabs[i].transform.position;
                float dist = Vector3.Distance(prefabPosition, splinePosition);
                
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPrefab = instantiatedPrefabs[i];
                }
            }
        }
        
        return closestPrefab;
    }
    
    // NUEVO: Cleanup seguro que respeta la velocidad del jugador
    void CleanupOldPrefabsSafe()
    {
        if (instantiatedPrefabs.Count > maxPrefabsInMemory)
        {
            float playerDistance = splineGenerator.GetPlayerDistance();
            
            // NUEVO: Cleanup inteligente basado en distancia del jugador
            int initialCount = instantiatedPrefabs.Count;
            int initialTrackerCount = prefabDistanceTracker.Count;
            
            // Limpiar tracker primero
            prefabDistanceTracker.RemoveAll(info =>
            {
                if (info.prefab == null) return true;
                
                // Solo remover prefabs que están suficientemente lejos del jugador
                if (info.distance < playerDistance - cleanupSafetyDistance)
                {
                    return true;
                }
                return false;
            });
            
            // Limpiar lista de prefabs basado en el tracker
            instantiatedPrefabs.RemoveAll(prefab =>
            {
                if (prefab == null) return true;
                
                // Verificar si el prefab está en el tracker (si no está, está marcado para borrar)
                bool isInTracker = false;
                foreach (var info in prefabDistanceTracker)
                {
                    if (info.prefab == prefab)
                    {
                        isInTracker = true;
                        break;
                    }
                }
                
                if (!isInTracker)
                {
                    Destroy(prefab);
                    return true;
                }
                return false;
            });
            
            CleanupOldSegmentTracking();
            
            int removedPrefabs = initialCount - instantiatedPrefabs.Count;
            int removedTracker = initialTrackerCount - prefabDistanceTracker.Count;
            
            if (removedPrefabs > 0)
            {
                Debug.Log($"SAFE CLEANUP: Removed {removedPrefabs} prefabs (tracker: {removedTracker}) - Player at {playerDistance:F1}");
            }
        }
    }
    
    void CleanupOldSegmentTracking()
    {
        float playerDistance = splineGenerator.GetPlayerDistance();
        int currentPlayerSegment = GetSegmentIndexAtDistance(playerDistance);
        
        List<int> segmentsToRemove = new List<int>();
        foreach (var kvp in segmentToPrefabIndex)
        {
            if (kvp.Key < currentPlayerSegment - 10) // Más conservador
            {
                segmentsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int segmentToRemove in segmentsToRemove)
        {
            segmentToPrefabIndex.Remove(segmentToRemove);
        }
        
        if (segmentsToRemove.Count > 0)
        {
            Debug.Log($"Cleaned up tracking for {segmentsToRemove.Count} old segments");
        }
    }
    
    // Métodos de utilidad para debugging optimizado
    public void LogSegmentPrefabUsage()
    {
        Debug.Log("=== SEGMENT PREFAB USAGE (HIGH-SPEED OPTIMIZED) ===");
        foreach (var kvp in segmentToPrefabIndex)
        {
            int segmentIndex = kvp.Key;
            int prefabIndex = kvp.Value;
            string prefabName = trackPrefabs[prefabIndex].name;
            Debug.Log($"Segment {segmentIndex}: Using prefab '{prefabName}' (index {prefabIndex})");
        }
        
        // Información adicional de rendimiento
        Debug.Log($"Pending prefabs: {pendingPrefabDistances.Count}");
        Debug.Log($"Pending triggers: {pendingTriggerDistances.Count}");
        Debug.Log($"Currently generating: {isGenerating}");
        Debug.Log($"Generation ahead: {currentGenerationAhead:F1}");
        Debug.Log($"Max per frame: {currentMaxPrefabsPerFrame}");
    }
    
    public void LogPrefabStatistics()
    {
        Dictionary<int, int> usage = new Dictionary<int, int>();
        
        foreach (var kvp in segmentToPrefabIndex)
        {
            int prefabIndex = kvp.Value;
            if (usage.ContainsKey(prefabIndex))
                usage[prefabIndex]++;
            else
                usage[prefabIndex] = 1;
        }
        
        Debug.Log("=== PREFAB SEGMENT STATISTICS (HIGH-SPEED OPTIMIZED) ===");
        for (int i = 0; i < trackPrefabs.Length; i++)
        {
            int count = usage.ContainsKey(i) ? usage[i] : 0;
            float percentage = segmentToPrefabIndex.Count > 0 ? (float)count / segmentToPrefabIndex.Count * 100f : 0f;
            Debug.Log($"Prefab '{trackPrefabs[i].name}': {count} segments ({percentage:F1}%)");
        }
        
        // Estadísticas de cache y rendimiento
        Debug.Log($"Prefab config cache entries: {prefabConfigCache.Count}");
        Debug.Log($"Distance tracker entries: {prefabDistanceTracker.Count}");
    }
    
    public void ForceNextSegmentPrefab(int prefabIndex)
    {
        if (prefabIndex >= 0 && prefabIndex < trackPrefabs.Length)
        {
            float playerDistance = splineGenerator.GetPlayerDistance();
            int nextSegment = GetSegmentIndexAtDistance(playerDistance) + 1;
            segmentToPrefabIndex[nextSegment] = prefabIndex;
            Debug.Log($"Forced segment {nextSegment} to use prefab '{trackPrefabs[prefabIndex].name}'");
        }
    }
    
    public void RegenerateVisualization()
    {
        // Parar generación asíncrona primero
        StopAllCoroutines();
        isGenerating = false;
        
        foreach (GameObject prefab in instantiatedPrefabs)
        {
            if (prefab != null)
                DestroyImmediate(prefab);
        }
        
        instantiatedPrefabs.Clear();
        segmentToPrefabIndex.Clear();
        pendingPrefabDistances.Clear();
        pendingTriggerDistances.Clear();
        prefabDistanceTracker.Clear(); // NUEVO: Limpiar tracker
        lastPrefabDistance = 0f;
        lastTriggerDistance = 0f;
        lastProcessedSegment = -1;
        lastUsedPrefabIndex = -1;
        
        GenerateInitialPrefabs();
    }
    
    // NUEVO: Métodos públicos de control de rendimiento
    public void SetPerformanceSettings(int maxPerFrame, int cleanupInterval, bool asyncGeneration)
    {
        baseMaxPrefabsPerFrame = maxPerFrame;
        cleanupFrameInterval = cleanupInterval;
        useAsyncGeneration = asyncGeneration;
        
        Debug.Log($"Performance settings updated: maxPerFrame={maxPerFrame}, cleanupInterval={cleanupInterval}, async={asyncGeneration}");
    }
    
    public bool IsCurrentlyGenerating()
    {
        return isGenerating;
    }
    
    public int GetPendingPrefabCount()
    {
        return pendingPrefabDistances.Count;
    }
    
    public int GetPendingTriggerCount()
    {
        return pendingTriggerDistances.Count;
    }
    
    public float GetCurrentGenerationAhead()
    {
        return currentGenerationAhead;
    }
    
    // Métodos públicos originales
    public int GetPrefabCount()
    {
        return instantiatedPrefabs.Count;
    }
    
    public float GetLastPrefabDistance()
    {
        return lastPrefabDistance;
    }
    
    public int GetPrefabVarietyCount()
    {
        return trackPrefabs != null ? trackPrefabs.Length : 0;
    }
    
    public int GetActiveSegmentCount()
    {
        return segmentToPrefabIndex.Count;
    }
    
    void OnDrawGizmosSelected()
    {
        if (splineGenerator == null || !splineGenerator.HasValidSpline()) return;
        
        float totalLength = splineGenerator.GetTotalLength();
        
        for (float dist = lastPrefabDistance; dist < totalLength && dist < lastPrefabDistance + 50f; dist += prefabSpacing)
        {
            Vector3 pos = splineGenerator.GetSplinePosition(dist);
            
            int segmentIndex = GetSegmentIndexAtDistance(dist);
            Gizmos.color = GetGizmoColorForSegment(segmentIndex);
            Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
        }
        
        if (addTriggers)
        {
            Gizmos.color = Color.red;
            for (float dist = lastTriggerDistance; dist < totalLength && dist < lastTriggerDistance + 100f; dist += triggerSpacing)
            {
                Vector3 pos = splineGenerator.GetSplinePosition(dist);
                Gizmos.DrawWireSphere(pos, 1f);
            }
        }
        
        Gizmos.color = Color.white;
        float approximateSegmentLength = splineGenerator.segmentsPerTrack * splineGenerator.segmentLength;
        for (float dist = 0; dist < totalLength; dist += approximateSegmentLength)
        {
            Vector3 pos = splineGenerator.GetSplinePosition(dist);
            Gizmos.DrawWireSphere(pos + Vector3.up * 2f, 0.5f);
        }
        
        // NUEVO: Mostrar información de rendimiento en gizmos
        if (Application.isPlaying)
        {
            // Mostrar zona de cleanup seguro
            float playerDistance = splineGenerator.GetPlayerDistance();
            Gizmos.color = Color.yellow;
            Vector3 cleanupPos = splineGenerator.GetSplinePosition(playerDistance - cleanupSafetyDistance);
            Gizmos.DrawWireSphere(cleanupPos, 3f);
            
            #if UNITY_EDITOR
            Vector3 textPos = transform.position + Vector3.up * 8f;
            string info = $"Prefabs: {instantiatedPrefabs.Count}\n";
            info += $"Pending Prefabs: {pendingPrefabDistances.Count}\n";
            info += $"Pending Triggers: {pendingTriggerDistances.Count}\n";
            info += $"Generating: {isGenerating}\n";
            info += $"Gen Ahead: {currentGenerationAhead:F0}\n";
            info += $"Max/Frame: {currentMaxPrefabsPerFrame}\n";
            info += $"Cache Entries: {prefabConfigCache.Count}\n";
            info += $"Distance Tracker: {prefabDistanceTracker.Count}";
            
            UnityEditor.Handles.Label(textPos, info);
            #endif
        }
    }
    
    Color GetGizmoColorForSegment(int segmentIndex)
    {
        Color[] colors = { Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.red };
        return colors[segmentIndex % colors.Length];
    }
}

// SplineTrigger permanece igual
public class SplineTrigger : MonoBehaviour
{
    public SplineMathGenerator splineGenerator;
    public float triggerDistance;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            splineGenerator.TriggerNextSegment(triggerDistance);
            Debug.Log($"Triggered next spline segment at distance {triggerDistance:F1}");
        }
    }
}