using UnityEngine;

public class EnhancedCollisionDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 1.5f;
    public bool useRaycastDetection = true;
    public bool useOverlapDetection = true;
    public bool showDebugRays = true;
    
    [Header("Collectible Detection")]
    public bool detectCollectibles = true;
    public float collectibleDetectionRadius = 2f; // Radio más grande para coleccionables
    public bool useCollectibleRaycast = false; // Los coleccionables no necesitan raycast normalmente
    
    [Header("Debug")]
    public bool logAllDetections = true;
    public bool logCollectibleDetections = true;
    
    private ImprovedSplineFollower player;
    
    void Start()
    {
        player = GetComponent<ImprovedSplineFollower>();
        if (player == null)
        {
            Debug.LogError("EnhancedCollisionDetector must be on the same GameObject as ImprovedSplineFollower!");
        }
        
        Debug.Log("🔍 Enhanced Collision Detector started - Detecting Obstacles AND Collectibles");
        
        if (detectCollectibles && logCollectibleDetections)
        {
            Debug.Log($"✨ Collectible detection enabled - Radius: {collectibleDetectionRadius}");
        }
    }
    
    void Update()
    {
        // Detección de obstáculos (sistema original)
        if (useOverlapDetection)
        {
            CheckOverlapCollision();
        }
        
        if (useRaycastDetection)
        {
            CheckRaycastCollision();
        }
        
        // Detección de coleccionables (nuevo sistema)
        if (detectCollectibles)
        {
            CheckCollectibleDetection();
        }
    }
    
    // ============================================
    // DETECCIÓN DE OBSTÁCULOS (SISTEMA ORIGINAL)
    // ============================================
    
    void CheckOverlapCollision()
    {
        // Detectar colisiones usando OverlapSphere
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        
        foreach (Collider hit in hitColliders)
        {
            if (hit.gameObject != this.gameObject && hit.CompareTag("Obstacle"))
            {
                ProcessObstacleCollision(hit.gameObject, "OVERLAP");
            }
        }
    }
    
    void CheckRaycastCollision()
    {
        // Detectar usando raycast hacia adelante
        Vector3 rayDirection = transform.forward;
        float rayDistance = detectionRadius * 2f;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, rayDirection, out hit, rayDistance))
        {
            if (hit.collider.CompareTag("Obstacle"))
            {
                ProcessObstacleCollision(hit.collider.gameObject, "RAYCAST");
            }
        }
        
        // Debug visual
        if (showDebugRays)
        {
            Debug.DrawRay(transform.position, rayDirection * rayDistance, Color.red, 0.1f);
        }
    }
    
    void ProcessObstacleCollision(GameObject obstacle, string detectionType)
    {
        if (logAllDetections)
        {
            Debug.Log($"🎯 {detectionType} OBSTACLE DETECTION: {obstacle.name}");
        }
        
        // Verificar que no hayamos procesado este obstáculo recientemente
        if (HasRecentlyProcessedObstacle(obstacle))
        {
            return;
        }
        
        // Buscar ObstacleCollision component
        ObstacleCollision obsCol = obstacle.GetComponent<ObstacleCollision>();
        if (obsCol != null)
        {
            Debug.Log($"💥 ACTIVATING OBSTACLE EFFECT: {obsCol.effectType} on {obstacle.name}");
            obsCol.HandlePlayerCollision(gameObject);
            
            // Marcar como procesado
            MarkObstacleAsProcessed(obstacle);
        }
        else
        {
            Debug.LogWarning($"⚠️ Obstacle {obstacle.name} has no ObstacleCollision component!");
        }
    }
    
    // ============================================
    // DETECCIÓN DE COLECCIONABLES (NUEVO SISTEMA)
    // ============================================
    
    void CheckCollectibleDetection()
    {
        // Detectar coleccionables usando OverlapSphere con radio específico
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, collectibleDetectionRadius);
        
        foreach (Collider hit in hitColliders)
        {
            if (hit.gameObject != this.gameObject && hit.CompareTag("Collectible"))
            {
                ProcessCollectibleCollision(hit.gameObject, "OVERLAP");
            }
        }
        
        // Raycast opcional para coleccionables (normalmente no es necesario)
        if (useCollectibleRaycast)
        {
            CheckCollectibleRaycast();
        }
    }
    
    void CheckCollectibleRaycast()
    {
        Vector3 rayDirection = transform.forward;
        float rayDistance = collectibleDetectionRadius * 1.5f;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, rayDirection, out hit, rayDistance))
        {
            if (hit.collider.CompareTag("Collectible"))
            {
                ProcessCollectibleCollision(hit.collider.gameObject, "RAYCAST");
            }
        }
        
        // Debug visual para coleccionables
        if (showDebugRays && useCollectibleRaycast)
        {
            Debug.DrawRay(transform.position, rayDirection * rayDistance, Color.yellow, 0.1f);
        }
    }
    
    void ProcessCollectibleCollision(GameObject collectible, string detectionType)
    {
        if (logCollectibleDetections)
        {
            Debug.Log($"✨ {detectionType} COLLECTIBLE DETECTION: {collectible.name}");
        }
        
        // Verificar que no hayamos procesado este coleccionable recientemente
        if (HasRecentlyProcessedCollectible(collectible))
        {
            if (logCollectibleDetections)
            {
                Debug.Log($"⏭️ COLLECTIBLE ALREADY PROCESSED: {collectible.name}");
            }
            return;
        }
        
        // Buscar CollectibleCollision component
        CollectibleCollision collectibleCol = collectible.GetComponent<CollectibleCollision>();
        if (collectibleCol != null)
        {
            Debug.Log($"🪙 ACTIVATING COLLECTIBLE: {collectibleCol.collectibleType} on {collectible.name}");
            collectibleCol.HandlePlayerCollection(gameObject);
            
            // Marcar como procesado
            MarkCollectibleAsProcessed(collectible);
        }
        else
        {
            Debug.LogWarning($"⚠️ Collectible {collectible.name} has no CollectibleCollision component!");
        }
    }
    
    // ============================================
    // SISTEMA DE TRACKING PARA EVITAR DUPLICADOS
    // ============================================
    
    // Sistema para obstáculos (original)
    private System.Collections.Generic.List<GameObject> processedObstacles = new System.Collections.Generic.List<GameObject>();
    private System.Collections.Generic.List<float> processedObstacleTimes = new System.Collections.Generic.List<float>();
    
    // Sistema para coleccionables (nuevo)
    private System.Collections.Generic.List<GameObject> processedCollectibles = new System.Collections.Generic.List<GameObject>();
    private System.Collections.Generic.List<float> processedCollectibleTimes = new System.Collections.Generic.List<float>();
    
    bool HasRecentlyProcessedObstacle(GameObject obstacle)
    {
        return HasRecentlyProcessed(obstacle, processedObstacles, processedObstacleTimes, 2f);
    }
    
    bool HasRecentlyProcessedCollectible(GameObject collectible)
    {
        return HasRecentlyProcessed(collectible, processedCollectibles, processedCollectibleTimes, 0.5f); // Ventana más corta para coleccionables
    }
    
    bool HasRecentlyProcessed(GameObject obj, System.Collections.Generic.List<GameObject> objList, System.Collections.Generic.List<float> timeList, float cooldownTime)
    {
        int index = objList.IndexOf(obj);
        if (index >= 0)
        {
            // Si fue procesado hace menos del cooldown, ignorar
            if (Time.time - timeList[index] < cooldownTime)
            {
                return true;
            }
            else
            {
                // Limpiar entrada antigua
                objList.RemoveAt(index);
                timeList.RemoveAt(index);
            }
        }
        return false;
    }
    
    void MarkObstacleAsProcessed(GameObject obstacle)
    {
        MarkAsProcessed(obstacle, processedObstacles, processedObstacleTimes);
    }
    
    void MarkCollectibleAsProcessed(GameObject collectible)
    {
        MarkAsProcessed(collectible, processedCollectibles, processedCollectibleTimes);
    }
    
    void MarkAsProcessed(GameObject obj, System.Collections.Generic.List<GameObject> objList, System.Collections.Generic.List<float> timeList)
    {
        objList.Add(obj);
        timeList.Add(Time.time);
        
        // Limpiar lista si se vuelve muy grande
        if (objList.Count > 15)
        {
            objList.RemoveAt(0);
            timeList.RemoveAt(0);
        }
    }
    
    // ============================================
    // MÉTODOS DE UNITY PARA DETECCIÓN TRADICIONAL
    // ============================================
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔥 UNITY COLLISION ENTER: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            ProcessObstacleCollision(collision.gameObject, "UNITY_COLLISION");
        }
        else if (collision.gameObject.CompareTag("Collectible"))
        {
            ProcessCollectibleCollision(collision.gameObject, "UNITY_COLLISION");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"⚡ UNITY TRIGGER ENTER: {other.gameObject.name} (Tag: {other.gameObject.tag})");
        
        if (other.CompareTag("Obstacle"))
        {
            ProcessObstacleCollision(other.gameObject, "UNITY_TRIGGER");
        }
        else if (other.CompareTag("Collectible"))
        {
            ProcessCollectibleCollision(other.gameObject, "UNITY_TRIGGER");
        }
    }
    
    // ============================================
    // VISUALIZACIÓN Y DEBUG
    // ============================================
    
    void OnDrawGizmosSelected()
    {
        // Mostrar radio de detección de obstáculos
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Mostrar radio de detección de coleccionables
        if (detectCollectibles)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectibleDetectionRadius);
        }
        
        // Mostrar dirección de raycast para obstáculos
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * detectionRadius * 2f);
        
        // Mostrar dirección de raycast para coleccionables
        if (detectCollectibles && useCollectibleRaycast)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * collectibleDetectionRadius * 1.5f);
        }
    }
    
    // ============================================
    // MÉTODOS DE TESTING Y DEBUG
    // ============================================
    
    [ContextMenu("Test Detection Systems")]
    void TestDetectionSystems()
    {
        Debug.Log("=== TESTING DETECTION SYSTEMS ===");
        
        CheckOverlapCollision();
        CheckRaycastCollision();
        
        if (detectCollectibles)
        {
            CheckCollectibleDetection();
        }
        
        Debug.Log($"Processed obstacles history: {processedObstacles.Count}");
        Debug.Log($"Processed collectibles history: {processedCollectibles.Count}");
    }
    
    [ContextMenu("Toggle Collectible Detection")]
    void ToggleCollectibleDetection()
    {
        detectCollectibles = !detectCollectibles;
        Debug.Log($"Collectible detection: {(detectCollectibles ? "ENABLED" : "DISABLED")}");
    }
    
    [ContextMenu("Clear Processed Lists")]
    void ClearProcessedLists()
    {
        processedObstacles.Clear();
        processedObstacleTimes.Clear();
        processedCollectibles.Clear();
        processedCollectibleTimes.Clear();
        Debug.Log("Cleared all processed object lists");
    }
    
    public void SetCollectibleDetectionRadius(float radius)
    {
        collectibleDetectionRadius = radius;
        Debug.Log($"Collectible detection radius set to: {radius}");
    }
    
    public void EnableCollectibleRaycast(bool enable)
    {
        useCollectibleRaycast = enable;
        Debug.Log($"Collectible raycast: {(enable ? "ENABLED" : "DISABLED")}");
    }
}