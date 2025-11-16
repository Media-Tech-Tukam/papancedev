using UnityEngine;

// Script temporal para configurar obstáculos rápidamente
public class ObstacleQuickSetup : MonoBehaviour
{
    [Header("Quick Setup for Static Obstacles")]
    [Range(5f, 50f)]
    public float obstacleSpacing = 15f; // Cada cuántos metros aparece un obstáculo
    
    [Range(0f, 1f)]
    public float staticObstacleProbability = 0.8f; // 80% de probabilidad de obstáculos estáticos
    
    public GameObject simpleObstaclePrefab; // Asignar un cubo básico
    
    [ContextMenu("Setup for Static Obstacles Only")]
    void SetupForStaticObstaclesOnly()
    {
        ObstacleGenerator generator = FindObjectOfType<ObstacleGenerator>();
        if (generator == null)
        {
            Debug.LogError("No ObstacleGenerator found! Create one first.");
            return;
        }
        
        // Configurar parámetros para obstáculos estáticos frecuentes
        generator.obstacleSpacing = obstacleSpacing;
        generator.minObstacleDistance = obstacleSpacing * 0.7f;
        generator.increaseDifficulty = false; // Desactivar dificultad progresiva para testing
        generator.usePatterns = false; // Solo obstáculos individuales
        
        // Crear obstáculo simple si no existe
        if (simpleObstaclePrefab == null)
        {
            CreateSimpleObstaclePrefab();
        }
        
        // Configurar array de obstáculos con solo estáticos
        SetupStaticOnlyObstacles(generator);
        
        Debug.Log($"✅ ObstacleGenerator configured for static obstacles every {obstacleSpacing}m");
    }
    
    void CreateSimpleObstaclePrefab()
    {
        // Crear un cubo rojo simple
        GameObject simpleCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        simpleCube.name = "Simple_Static_Obstacle";
        simpleCube.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
        
        // Material rojo
        Renderer renderer = simpleCube.GetComponent<Renderer>();
        renderer.material.color = Color.red;
        
        // Configurar collider
        BoxCollider collider = simpleCube.GetComponent<BoxCollider>();
        collider.isTrigger = false; // Colisión física
        
        // Agregar comportamiento de obstáculo
        ObstacleCollision collision = simpleCube.AddComponent<ObstacleCollision>();
        collision.effectType = ObstacleCollision.ObstacleEffect.SlowDown;
        collision.effectStrength = 0.3f; // Ralentiza a 30% de velocidad
        collision.effectDuration = 1.5f;
        
        // Configurar tag
        simpleCube.tag = "Obstacle";
        
        // Convertir en prefab (guardar referencia)
        simpleObstaclePrefab = simpleCube;
        
        Debug.Log("✅ Simple obstacle prefab created");
    }
    
    void SetupStaticOnlyObstacles(ObstacleGenerator generator)
    {
        // Crear array con solo el obstáculo estático
        var obstacleData = new ObstacleGenerator.ObstacleData[1];
        
        obstacleData[0] = new ObstacleGenerator.ObstacleData
        {
            prefab = simpleObstaclePrefab,
            obstacleName = "Static Cube",
            type = ObstacleGenerator.ObstacleType.Static,
            minDifficulty = 0f,
            spawnWeight = 1f,
            positionOffset = Vector3.zero,
            canBeInPattern = true
        };
        
        generator.availableObstacles = obstacleData;
        
        // Limpiar patrones para que solo use obstáculos individuales
        generator.obstaclePatterns = new ObstacleGenerator.ObstaclePattern[0];
        
        Debug.Log("✅ Generator configured with static obstacles only");
    }
    
    [ContextMenu("Test Obstacle Spacing")]
    void TestObstacleSpacing()
    {
        SplineMathGenerator spline = FindObjectOfType<SplineMathGenerator>();
        if (spline == null)
        {
            Debug.LogError("No SplineMathGenerator found!");
            return;
        }
        
        float totalLength = spline.GetTotalLength();
        int expectedObstacles = Mathf.FloorToInt(totalLength / obstacleSpacing);
        
        Debug.Log($"📊 Spline length: {totalLength:F1}m");
        Debug.Log($"📊 Obstacle spacing: {obstacleSpacing}m");
        Debug.Log($"📊 Expected obstacles: {expectedObstacles}");
        
        ObstacleGenerator generator = FindObjectOfType<ObstacleGenerator>();
        if (generator != null)
        {
            Debug.Log($"📊 Current active obstacles: {generator.GetActiveObstacleCount()}");
        }
    }
}