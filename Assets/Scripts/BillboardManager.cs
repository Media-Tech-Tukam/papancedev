using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================
// BILLBOARD MANAGER - Vallas publicitarias por segmento
// ============================================
public class BillboardManager : MonoBehaviour
{
    [Header("Billboard Configuration")]
    [SerializeField] private BillboardPrefabData[] billboardPrefabs;
    public float billboardHeight = 3f; // Offset en Y
    public bool facePlayer = true; // Mirar hacia el jugador
    public float forwardOffset = 10f; // Distancia hacia adelante del inicio del segmento
    public float lateralVariation = 0f; // Variación lateral opcional (0 = centrado)
    
    [Header("Performance Settings")]
    public int maxBillboardsPerFrame = 2;
    public int cleanupFrameInterval = 90; // Menos frecuente que TrackVisualizer
    public bool useAsyncGeneration = true;
    public int maxBillboardsInMemory = 50;
    
    [Header("Billboard Selection")]
    public bool useRandomSelection = true;
    public bool avoidConsecutiveBillboards = true;
    [Range(0f, 100f)]
    public float globalWeightMultiplier = 1f;
    
    [Header("Debug")]
    public bool showBillboardPositions = true;
    public Color debugColor = Color.cyan;
    
    private SplineMathGenerator splineGenerator;
    private List<GameObject> instantiatedBillboards = new List<GameObject>();
    private Dictionary<int, int> segmentToBillboardIndex = new Dictionary<int, int>();
    private int lastProcessedSegment = -1;
    private int lastUsedBillboardIndex = -1;
    
    // Variables de optimización
    private int frameCounter = 0;
    private bool isGenerating = false;
    private Queue<int> pendingSegments = new Queue<int>();
    
    // Cache de configuración de prefabs
    private Dictionary<GameObject, BillboardConfiguration> billboardConfigCache = new Dictionary<GameObject, BillboardConfiguration>();
    
    [System.Serializable]
    public class BillboardPrefabData
    {
        public GameObject prefab;
        public string name = "Billboard Variant";
        [Range(0f, 100f)]
        public float weight = 1f;
        [TextArea(2, 3)]
        public string description = "";
    }
    
    private struct BillboardConfiguration
    {
        public bool hasRigidbody;
        public bool hasValidColliders;
        public Bounds bounds;
        public bool needsConfiguration;
    }
    
    void Start()
    {
        Debug.Log("=== BILLBOARD MANAGER STARTING ===");
        
        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        if (splineGenerator == null)
        {
            Debug.LogError("BillboardManager requires SplineMathGenerator in the scene!");
            return;
        }
        
        if (!ValidateBillboardPrefabs())
        {
            Debug.LogError("No valid billboard prefabs assigned!");
            return;
        }
        
        PreCacheBillboardConfigurations();
        
        Invoke(nameof(GenerateInitialBillboards), 0.2f);
    }
    
    void PreCacheBillboardConfigurations()
    {
        foreach (var billboardData in billboardPrefabs)
        {
            if (billboardData.prefab != null)
            {
                BillboardConfiguration config = new BillboardConfiguration
                {
                    hasRigidbody = billboardData.prefab.GetComponent<Rigidbody>() != null,
                    hasValidColliders = billboardData.prefab.GetComponentsInChildren<Collider>().Length > 0,
                    needsConfiguration = true
                };
                
                // Calcular bounds aproximados
                Renderer[] renderers = billboardData.prefab.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }
                    config.bounds = bounds;
                }
                
                billboardConfigCache[billboardData.prefab] = config;
            }
        }
        
        Debug.Log($"Pre-cached configurations for {billboardConfigCache.Count} billboard prefabs");
    }
    
    bool ValidateBillboardPrefabs()
    {
        if (billboardPrefabs == null || billboardPrefabs.Length == 0)
        {
            Debug.LogError("No billboard prefabs assigned!");
            return false;
        }
        
        int validPrefabs = 0;
        for (int i = 0; i < billboardPrefabs.Length; i++)
        {
            if (billboardPrefabs[i].prefab != null && billboardPrefabs[i].weight > 0)
            {
                validPrefabs++;
            }
            else
            {
                Debug.LogWarning($"BillboardPrefab [{i}] is invalid (null prefab or zero weight)");
            }
        }
        
        Debug.Log($"Found {validPrefabs} valid billboard prefabs out of {billboardPrefabs.Length}");
        return validPrefabs > 0;
    }
    
    void Update()
    {
        frameCounter++;
        
        if (useAsyncGeneration)
        {
            if (!isGenerating)
            {
                StartCoroutine(GenerateBillboardsAsync());
            }
        }
        else
        {
            GenerateBillboardsForNewSegments();
        }
        
        // Cleanup optimizado - menos frecuente que TrackVisualizer
        if (frameCounter % cleanupFrameInterval == 0)
        {
            CleanupOldBillboards();
        }
    }
    
    void GenerateInitialBillboards()
    {
        if (!splineGenerator.HasValidSpline())
        {
            Debug.LogWarning("No valid spline found for billboards");
            return;
        }
        
        Debug.Log("Generating initial billboards...");
        
        // Generar billboards para los segmentos existentes
        var existingSegments = GetCurrentSplineSegments();
        foreach (int segmentIndex in existingSegments)
        {
            if (segmentIndex > lastProcessedSegment)
            {
                pendingSegments.Enqueue(segmentIndex);
            }
        }
        
        if (useAsyncGeneration)
        {
            StartCoroutine(ProcessPendingSegments());
        }
        else
        {
            while (pendingSegments.Count > 0)
            {
                int segmentIndex = pendingSegments.Dequeue();
                CreateBillboardForSegment(segmentIndex);
            }
        }
    }
    
    public void OnSplineUpdated()
    {
        Debug.Log("Spline updated, checking for new billboard segments...");
        
        if (useAsyncGeneration)
        {
            if (!isGenerating)
            {
                StartCoroutine(GenerateBillboardsAsync());
            }
        }
        else
        {
            GenerateBillboardsForNewSegments();
        }
    }
    
    IEnumerator GenerateBillboardsAsync()
    {
        isGenerating = true;
        
        if (!splineGenerator.HasValidSpline())
        {
            isGenerating = false;
            yield break;
        }
        
        // Identificar nuevos segmentos
        var currentSegments = GetCurrentSplineSegments();
        foreach (int segmentIndex in currentSegments)
        {
            if (segmentIndex > lastProcessedSegment && !segmentToBillboardIndex.ContainsKey(segmentIndex))
            {
                pendingSegments.Enqueue(segmentIndex);
            }
        }
        
        // Procesar segmentos pendientes
        yield return StartCoroutine(ProcessPendingSegments());
        
        isGenerating = false;
        Debug.Log($"Generated billboards for segments up to {lastProcessedSegment}");
    }
    
    IEnumerator ProcessPendingSegments()
    {
        int processedThisFrame = 0;
        
        while (pendingSegments.Count > 0 && processedThisFrame < maxBillboardsPerFrame)
        {
            int segmentIndex = pendingSegments.Dequeue();
            CreateBillboardForSegment(segmentIndex);
            processedThisFrame++;
            
            // Yield cada billboard para mantener framerate
            if (processedThisFrame % 1 == 0)
            {
                yield return null;
            }
        }
    }
    
    void GenerateBillboardsForNewSegments()
    {
        if (!splineGenerator.HasValidSpline()) return;
        
        var currentSegments = GetCurrentSplineSegments();
        int billboardsThisFrame = 0;
        
        foreach (int segmentIndex in currentSegments)
        {
            if (segmentIndex > lastProcessedSegment && !segmentToBillboardIndex.ContainsKey(segmentIndex))
            {
                CreateBillboardForSegment(segmentIndex);
                billboardsThisFrame++;
                
                if (billboardsThisFrame >= maxBillboardsPerFrame)
                    break;
            }
        }
    }
    
    List<int> GetCurrentSplineSegments()
    {
        List<int> segments = new List<int>();
        
        // Acceder a los segmentos del spline usando reflexión o método público
        // Por ahora, estimamos basándonos en la longitud total
        float totalLength = splineGenerator.GetTotalLength();
        float approximateSegmentLength = splineGenerator.segmentsPerTrack * splineGenerator.segmentLength;
        
        int maxSegments = Mathf.FloorToInt(totalLength / approximateSegmentLength);
        
        for (int i = 0; i <= maxSegments; i++)
        {
            segments.Add(i);
        }
        
        return segments;
    }
    
    void CreateBillboardForSegment(int segmentIndex)
    {
        // Calcular posición del inicio del segmento + offset hacia adelante
        float approximateSegmentLength = splineGenerator.segmentsPerTrack * splineGenerator.segmentLength;
        float segmentStartDistance = segmentIndex * approximateSegmentLength;
        float billboardDistance = segmentStartDistance + forwardOffset;
        
        Vector3 billboardPosition = splineGenerator.GetSplinePosition(billboardDistance);
        Vector3 splineDirection = splineGenerator.GetSplineDirection(billboardDistance);
        
        // Seleccionar prefab para este segmento
        int selectedBillboardIndex = SelectBillboardForSegment(segmentIndex);
        segmentToBillboardIndex[segmentIndex] = selectedBillboardIndex;
        
        BillboardPrefabData selectedBillboardData = billboardPrefabs[selectedBillboardIndex];
        
        // Aplicar variación lateral opcional
        if (lateralVariation > 0)
        {
            Vector3 splineRight = splineGenerator.GetSplineRight(billboardDistance);
            float lateralRandomOffset = Random.Range(-lateralVariation, lateralVariation);
            billboardPosition += splineRight * lateralRandomOffset;
        }
        
        // Ajustar altura
        billboardPosition.y += billboardHeight;
        
        // Calcular rotación (mirando hacia atrás, hacia el jugador)
        Quaternion billboardRotation = Quaternion.identity;
        if (facePlayer)
        {
            // Rotar 180 grados para que mire hacia atrás (hacia donde viene el jugador)
            Vector3 billboardForward = -splineDirection;
            billboardRotation = Quaternion.LookRotation(billboardForward, Vector3.up);
        }
        
        // Instanciar billboard
        GameObject billboard = Instantiate(selectedBillboardData.prefab, billboardPosition, billboardRotation);
        billboard.name = $"Billboard_Seg{segmentIndex}_{selectedBillboardData.name}_Dist{billboardDistance:F1}";
        
        // Configurar billboard
        ConfigureBillboardOptimized(billboard, selectedBillboardData.prefab);
        
        instantiatedBillboards.Add(billboard);
        lastProcessedSegment = Mathf.Max(lastProcessedSegment, segmentIndex);
        
        Debug.Log($"Created frontal billboard for segment {segmentIndex}: '{selectedBillboardData.name}' at distance {billboardDistance:F1}");
    }
    
    int SelectBillboardForSegment(int segmentIndex)
    {
        if (!useRandomSelection)
        {
            for (int i = 0; i < billboardPrefabs.Length; i++)
            {
                if (billboardPrefabs[i].prefab != null && billboardPrefabs[i].weight > 0)
                    return i;
            }
            return 0;
        }
        
        List<int> validIndices = new List<int>();
        List<float> weights = new List<float>();
        
        for (int i = 0; i < billboardPrefabs.Length; i++)
        {
            BillboardPrefabData billboardData = billboardPrefabs[i];
            
            if (billboardData.prefab == null || billboardData.weight <= 0)
                continue;
            
            if (avoidConsecutiveBillboards && i == lastUsedBillboardIndex && segmentIndex > 0)
                continue;
            
            validIndices.Add(i);
            weights.Add(billboardData.weight * globalWeightMultiplier);
        }
        
        if (validIndices.Count == 0)
        {
            Debug.LogWarning($"No valid billboard candidates for segment {segmentIndex}, using fallback");
            for (int i = 0; i < billboardPrefabs.Length; i++)
            {
                if (billboardPrefabs[i].prefab != null)
                {
                    lastUsedBillboardIndex = i;
                    return i;
                }
            }
            return 0;
        }
        
        int selectedIndex = WeightedRandomSelection(validIndices, weights);
        lastUsedBillboardIndex = selectedIndex;
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
    
    void ConfigureBillboardOptimized(GameObject instance, GameObject originalPrefab)
    {
        if (!billboardConfigCache.TryGetValue(originalPrefab, out BillboardConfiguration config))
        {
            ConfigureBillboard(instance);
            return;
        }
        
        // Configuración optimizada usando cache
        if (config.hasRigidbody)
        {
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
            }
        }
        
        // Configurar colliders si es necesario
        if (!config.hasValidColliders)
        {
            BoxCollider boxCollider = instance.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(2f, 4f, 0.5f); // Tamaño típico de billboard
            boxCollider.isTrigger = true; // Los billboards no deberían bloquear
        }
        
        // Configurar layer
        if (instance.layer == 0)
        {
            instance.layer = LayerMask.NameToLayer("Default");
        }
        
        // Tag para identificación
        if (instance.tag == "Untagged")
        {
            instance.tag = "Billboard";
        }
    }
    
    void ConfigureBillboard(GameObject billboard)
    {
        // Configuración fallback
        Rigidbody rb = billboard.GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }
        
        Collider[] colliders = billboard.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            BoxCollider boxCollider = billboard.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(2f, 4f, 0.5f);
            boxCollider.isTrigger = true;
        }
        else
        {
            foreach (Collider collider in colliders)
            {
                collider.isTrigger = true; // Los billboards no deberían ser sólidos
            }
        }
        
        if (billboard.layer == 0)
        {
            billboard.layer = LayerMask.NameToLayer("Default");
        }
        
        if (billboard.tag == "Untagged")
        {
            billboard.tag = "Billboard";
        }
    }
    
    void CleanupOldBillboards()
    {
        if (instantiatedBillboards.Count > maxBillboardsInMemory)
        {
            int billboardsToRemove = instantiatedBillboards.Count - maxBillboardsInMemory;
            int initialCount = instantiatedBillboards.Count;
            
            // Remover los más antiguos
            for (int i = 0; i < billboardsToRemove && i < instantiatedBillboards.Count; i++)
            {
                if (instantiatedBillboards[i] != null)
                {
                    Destroy(instantiatedBillboards[i]);
                }
            }
            
            instantiatedBillboards.RemoveRange(0, billboardsToRemove);
            
            CleanupOldSegmentTracking();
            
            int actualRemoved = initialCount - instantiatedBillboards.Count;
            Debug.Log($"Cleaned up {actualRemoved} old billboards");
        }
    }
    
    void CleanupOldSegmentTracking()
    {
        float playerDistance = splineGenerator.GetPlayerDistance();
        float approximateSegmentLength = splineGenerator.segmentsPerTrack * splineGenerator.segmentLength;
        int currentPlayerSegment = Mathf.FloorToInt(playerDistance / approximateSegmentLength);
        
        List<int> segmentsToRemove = new List<int>();
        foreach (var kvp in segmentToBillboardIndex)
        {
            if (kvp.Key < currentPlayerSegment - 3) // Mantener pocos segmentos atrás
            {
                segmentsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (int segmentToRemove in segmentsToRemove)
        {
            segmentToBillboardIndex.Remove(segmentToRemove);
        }
        
        if (segmentsToRemove.Count > 0)
        {
            Debug.Log($"Cleaned up tracking for {segmentsToRemove.Count} old billboard segments");
        }
    }
    
    // Métodos públicos de utilidad
    public void LogBillboardStatistics()
    {
        Dictionary<int, int> usage = new Dictionary<int, int>();
        
        foreach (var kvp in segmentToBillboardIndex)
        {
            int billboardIndex = kvp.Value;
            if (usage.ContainsKey(billboardIndex))
                usage[billboardIndex]++;
            else
                usage[billboardIndex] = 1;
        }
        
        Debug.Log("=== BILLBOARD STATISTICS ===");
        for (int i = 0; i < billboardPrefabs.Length; i++)
        {
            int count = usage.ContainsKey(i) ? usage[i] : 0;
            float percentage = segmentToBillboardIndex.Count > 0 ? (float)count / segmentToBillboardIndex.Count * 100f : 0f;
            Debug.Log($"Billboard '{billboardPrefabs[i].name}': {count} segments ({percentage:F1}%)");
        }
        
        Debug.Log($"Total billboards: {instantiatedBillboards.Count}");
        Debug.Log($"Segments tracked: {segmentToBillboardIndex.Count}");
        Debug.Log($"Currently generating: {isGenerating}");
    }
    
    public void RegenerateBillboards()
    {
        StopAllCoroutines();
        isGenerating = false;
        
        foreach (GameObject billboard in instantiatedBillboards)
        {
            if (billboard != null)
                DestroyImmediate(billboard);
        }
        
        instantiatedBillboards.Clear();
        segmentToBillboardIndex.Clear();
        pendingSegments.Clear();
        lastProcessedSegment = -1;
        lastUsedBillboardIndex = -1;
        
        GenerateInitialBillboards();
    }
    
    public int GetBillboardCount()
    {
        return instantiatedBillboards.Count;
    }
    
    public int GetBillboardVarietyCount()
    {
        return billboardPrefabs != null ? billboardPrefabs.Length : 0;
    }
    
    public bool IsCurrentlyGenerating()
    {
        return isGenerating;
    }
    
    public int GetPendingSegmentCount()
    {
        return pendingSegments.Count;
    }
    
    void OnDrawGizmosSelected()
    {
        if (splineGenerator == null || !splineGenerator.HasValidSpline()) return;
        
        if (!showBillboardPositions) return;
        
        // Mostrar posiciones de billboards para próximos segmentos
        float totalLength = splineGenerator.GetTotalLength();
        float approximateSegmentLength = splineGenerator.segmentsPerTrack * splineGenerator.segmentLength;
        
        Gizmos.color = debugColor;
        
        for (int segmentIndex = lastProcessedSegment; segmentIndex <= lastProcessedSegment + 5; segmentIndex++)
        {
            float segmentStartDistance = segmentIndex * approximateSegmentLength;
            if (segmentStartDistance > totalLength) break;
            
            float billboardDistance = segmentStartDistance + forwardOffset;
            Vector3 billboardPosition = splineGenerator.GetSplinePosition(billboardDistance);
            Vector3 splineDirection = splineGenerator.GetSplineDirection(billboardDistance);
            
            billboardPosition.y += billboardHeight;
            
            // Mostrar posición de billboard frontal
            Gizmos.DrawWireCube(billboardPosition, new Vector3(4f, 3f, 0.5f));
            
            // Mostrar dirección hacia donde mira la valla (hacia atrás)
            Vector3 billboardForward = -splineDirection;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(billboardPosition, billboardForward * 2f);
            Gizmos.color = debugColor;
            
            // Línea conectando al inicio del segmento
            Vector3 segmentStart = splineGenerator.GetSplinePosition(segmentStartDistance);
            Gizmos.DrawLine(segmentStart, billboardPosition);
        }
        
        // Mostrar información de debugging en editor
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Vector3 textPos = transform.position + Vector3.up * 10f;
            string info = $"Billboards: {instantiatedBillboards.Count}\n";
            info += $"Pending: {pendingSegments.Count}\n";
            info += $"Last Segment: {lastProcessedSegment}\n";
            info += $"Generating: {isGenerating}";
            
            UnityEditor.Handles.Label(textPos, info);
        }
        #endif
    }
}