using UnityEngine;

public class CollisionDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Tools")]
    public float checkRadius = 10f;
    
    void Start()
    {
        Invoke(nameof(DiagnoseScene), 1f); // Esperar 1 segundo para que se generen obstáculos
    }
    
    void Update()
    {
        // Verificar obstáculos cercanos cada 3 segundos
        if (Time.time % 3f < 0.1f)
        {
            CheckNearbyObstacles();
        }
    }
    
    void DiagnoseScene()
    {
        Debug.Log("=== COLLISION DIAGNOSTIC ===");
        
        // 1. Verificar ObstacleGenerator
        ObstacleGenerator generator = FindObjectOfType<ObstacleGenerator>();
        if (generator == null)
        {
            Debug.LogError("❌ No ObstacleGenerator found in scene!");
            return;
        }
        
        Debug.Log($"✅ ObstacleGenerator found: {generator.GetActiveObstacleCount()} active obstacles");
        
        // 2. Verificar obstáculos con tag
        GameObject[] obstaclesWithTag = GameObject.FindGameObjectsWithTag("Obstacle");
        Debug.Log($"📊 Obstacles with 'Obstacle' tag: {obstaclesWithTag.Length}");
        
        // 3. Verificar obstáculos con ObstacleCollision
        ObstacleCollision[] obstacleCollisions = FindObjectsOfType<ObstacleCollision>();
        Debug.Log($"📊 Objects with ObstacleCollision: {obstacleCollisions.Length}");
        
        // 4. Verificar si hay obstáculos cerca del player
        CheckNearbyObstacles();
        
        // 5. Información del spline
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        if (player != null)
        {
            Debug.Log($"🎮 Player distance on spline: {player.GetCurrentDistance():F1}");
            Debug.Log($"🎮 Player position: {player.transform.position}");
        }
    }
    
    void CheckNearbyObstacles()
    {
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        if (player == null) return;
        
        // Buscar obstáculos en un radio
        Collider[] nearbyColliders = Physics.OverlapSphere(player.transform.position, checkRadius);
        
        int obstacleCount = 0;
        foreach (Collider col in nearbyColliders)
        {
            if (col.CompareTag("Obstacle"))
            {
                obstacleCount++;
                float distance = Vector3.Distance(player.transform.position, col.transform.position);
                Debug.Log($"🎯 Nearby obstacle: {col.name} at distance {distance:F1}");
                
                // Verificar si tiene ObstacleCollision
                ObstacleCollision obsCol = col.GetComponent<ObstacleCollision>();
                if (obsCol == null)
                {
                    Debug.LogWarning($"⚠️ Obstacle {col.name} missing ObstacleCollision component!");
                }
            }
        }
        
        if (obstacleCount == 0)
        {
            Debug.LogWarning($"⚠️ No obstacles found within {checkRadius}m of player");
        }
    }
    
    [ContextMenu("Force Generate Obstacle Near Player")]
    void ForceGenerateObstacleNearPlayer()
    {
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        if (player == null)
        {
            Debug.LogError("No player found!");
            return;
        }
        
        // Crear obstáculo simple cerca del jugador
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Test_Collision_Obstacle";
        obstacle.tag = "Obstacle";
        
        // Posicionar delante del jugador
        Vector3 playerPos = player.transform.position;
        obstacle.transform.position = playerPos + player.transform.forward * 5f;
        obstacle.transform.localScale = new Vector3(2f, 2f, 2f);
        
        // Configurar material rojo
        Renderer renderer = obstacle.GetComponent<Renderer>();
        renderer.material.color = Color.red;
        
        // Agregar ObstacleCollision
        ObstacleCollision obsCol = obstacle.AddComponent<ObstacleCollision>();
        obsCol.effectType = ObstacleCollision.ObstacleEffect.SlowDown;
        obsCol.effectStrength = 0.5f;
        obsCol.effectDuration = 2f;
        
        Debug.Log($"🎯 Test obstacle created at {obstacle.transform.position}");
    }
    
    [ContextMenu("Test Manual Collision")]
    void TestManualCollision()
    {
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        ObstacleCollision[] obstacles = FindObjectsOfType<ObstacleCollision>();
        
        if (player == null)
        {
            Debug.LogError("No player found!");
            return;
        }
        
        if (obstacles.Length == 0)
        {
            Debug.LogError("No obstacles found!");
            return;
        }
        
        // Probar colisión manual con el primer obstáculo
        Debug.Log($"🧪 Testing manual collision with {obstacles[0].name}");
        obstacles[0].HandlePlayerCollision(player.gameObject);
    }
    
    [ContextMenu("Check Physics Settings")]
    void CheckPhysicsSettings()
    {
        Debug.Log("=== PHYSICS SETTINGS ===");
        
        // Verificar que las layers por defecto colisionan
        bool defaultCollision = Physics.GetIgnoreLayerCollision(0, 0);
        Debug.Log($"Default layer collision enabled: {!defaultCollision}");
        
        if (defaultCollision)
        {
            Debug.LogError("❌ Default layer collision is DISABLED! Enable it in Project Settings > Physics");
        }
        
        // Verificar configuración de física
        Debug.Log($"Fixed Timestep: {Time.fixedDeltaTime}");
        Debug.Log($"Default Contact Offset: {Physics.defaultContactOffset}");
        Debug.Log($"Bounce Threshold: {Physics.bounceThreshold}");
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar radio de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
        
        // Mostrar obstáculos cercanos
        Collider[] nearby = Physics.OverlapSphere(transform.position, checkRadius);
        foreach (Collider col in nearby)
        {
            if (col.CompareTag("Obstacle"))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, col.transform.position);
                Gizmos.DrawWireCube(col.transform.position, col.bounds.size);
            }
        }
    }
}