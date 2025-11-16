using UnityEngine;
using System.Collections.Generic;

// ============================================
// OBSTACLE SETUP HELPER - Para configuración rápida
// ============================================
public class ObstacleSetupHelper : MonoBehaviour
{
    [Header("Quick Setup")]
    public bool createTestObstacles = true;
    public Material obstacleMaterial;
    
    [Header("Test Obstacle Settings")]
    public int testObstacleCount = 5;
    public Vector3 obstacleSize = new Vector3(1f, 2f, 1f);
    
    [Header("Advanced Setup")]
    public bool setupAudioSources = false;
    public AudioClip defaultCollisionSound;
    
    void Start()
    {
        if (createTestObstacles)
        {
            CreateTestObstacles();
        }
    }
    
    [ContextMenu("Create Test Obstacles")]
    void CreateTestObstacles()
    {
        Debug.Log("Creating test obstacles...");
        
        // Crear carpeta para obstáculos
        GameObject obstacleParent = new GameObject("Test Obstacles");
        
        // Crear diferentes tipos de obstáculos
        CreateStaticObstacle(obstacleParent.transform);
        CreateMovingObstacle(obstacleParent.transform);
        CreateRotatingObstacle(obstacleParent.transform);
        CreateTemporalObstacle(obstacleParent.transform);
        CreateComboObstacle(obstacleParent.transform);
        
        Debug.Log("Test obstacles created! Assign them to the ObstacleGenerator.");
    }
    
    GameObject CreateStaticObstacle(Transform parent)
    {
        GameObject obstacle = CreateBasicObstacle("Static_Obstacle", parent);
        
        // Configurar como obstáculo estático
        ObstacleCollision collision = obstacle.AddComponent<ObstacleCollision>();
        collision.effectType = ObstacleCollision.ObstacleEffect.SlowDown;
        collision.effectStrength = 0.5f;
        collision.effectDuration = 1f;
        
        return obstacle;
    }
    
    GameObject CreateMovingObstacle(Transform parent)
    {
        GameObject obstacle = CreateBasicObstacle("Moving_Obstacle", parent);
        obstacle.GetComponent<Renderer>().material.color = Color.blue;
        
        // Configurar movimiento
        MovingObstacle moving = obstacle.AddComponent<MovingObstacle>();
        moving.movementType = MovingObstacle.MovementType.Lateral;
        moving.moveSpeed = 3f;
        moving.moveRange = 4f;
        moving.oscillate = true;
        
        // Configurar colisión
        ObstacleCollision collision = obstacle.AddComponent<ObstacleCollision>();
        collision.effectType = ObstacleCollision.ObstacleEffect.PushBack;
        collision.effectStrength = 5f;
        
        return obstacle;
    }
    
    GameObject CreateRotatingObstacle(Transform parent)
    {
        GameObject obstacle = CreateBasicObstacle("Rotating_Obstacle", parent);
        obstacle.GetComponent<Renderer>().material.color = Color.yellow;
        
        // Hacer más alargado para que se vea la rotación
        obstacle.transform.localScale = new Vector3(3f, 0.5f, 0.5f);
        
        // Configurar rotación
        RotatingObstacle rotating = obstacle.AddComponent<RotatingObstacle>();
        rotating.rotationAxis = Vector3.up;
        rotating.rotationSpeed = 45f;
        rotating.rotationType = RotatingObstacle.RotationType.Continuous;
        
        // Configurar colisión
        ObstacleCollision collision = obstacle.AddComponent<ObstacleCollision>();
        collision.effectType = ObstacleCollision.ObstacleEffect.Bounce;
        collision.effectStrength = 10f;
        
        return obstacle;
    }
    
    GameObject CreateTemporalObstacle(Transform parent)
    {
        GameObject obstacle = CreateBasicObstacle("Temporal_Obstacle", parent);
        obstacle.GetComponent<Renderer>().material.color = Color.magenta;
        
        // Configurar comportamiento temporal
        TemporalObstacle temporal = obstacle.AddComponent<TemporalObstacle>();
        temporal.visibleTime = 2f;
        temporal.hiddenTime = 1f;
        temporal.startVisible = true;
        temporal.useScaling = true;
        temporal.transitionSpeed = 8f;
        
        // Configurar colisión
        ObstacleCollision collision = obstacle.AddComponent<ObstacleCollision>();
        collision.effectType = ObstacleCollision.ObstacleEffect.Stop;
        collision.effectDuration = 1.5f;
        
        return obstacle;
    }
    
    GameObject CreateComboObstacle(Transform parent)
    {
        GameObject obstacle = CreateBasicObstacle("Combo_Obstacle", parent);
        obstacle.GetComponent<Renderer>().material.color = Color.cyan;
        
        // Combinar movimiento y rotación
        MovingObstacle moving = obstacle.AddComponent<MovingObstacle>();
        moving.movementType = MovingObstacle.MovementType.Circular;
        moving.moveSpeed = 1f;
        moving.moveRange = 3f;
        
        RotatingObstacle rotating = obstacle.AddComponent<RotatingObstacle>();
        rotating.rotationSpeed = 90f;
        rotating.rotationType = RotatingObstacle.RotationType.Continuous;
        
        // Configurar colisión más fuerte
        ObstacleCollision collision = obstacle.AddComponent<ObstacleCollision>();
        collision.effectType = ObstacleCollision.ObstacleEffect.Damage;
        collision.effectStrength = 2f;
        collision.destroyOnCollision = true;
        
        return obstacle;
    }
    
    GameObject CreateBasicObstacle(string name, Transform parent)
    {
        // Crear cubo básico
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.parent = parent;
        obstacle.transform.localScale = obstacleSize;
        
        // Configurar material
        Renderer renderer = obstacle.GetComponent<Renderer>();
        if (obstacleMaterial != null)
        {
            renderer.material = obstacleMaterial;
        }
        else
        {
            renderer.material.color = Color.red;
        }
        
        // Configurar collider
        BoxCollider collider = obstacle.GetComponent<BoxCollider>();
        collider.isTrigger = false; // Colisión física real
        
        // Configurar tag
        obstacle.tag = "Obstacle";
        
        return obstacle;
    }
    
    [ContextMenu("Setup Obstacle Generator")]
    void SetupObstacleGenerator()
    {
        ObstacleGenerator generator = FindObjectOfType<ObstacleGenerator>();
        if (generator == null)
        {
            Debug.LogError("No ObstacleGenerator found in scene!");
            return;
        }
        
        // Configurar datos de obstáculos básicos
        if (generator.availableObstacles == null || generator.availableObstacles.Length == 0)
        {
            SetupDefaultObstacleData(generator);
        }
        
        // Configurar patrones básicos
        if (generator.obstaclePatterns == null || generator.obstaclePatterns.Length == 0)
        {
            SetupDefaultPatterns(generator);
        }
        
        Debug.Log("ObstacleGenerator configured with default settings!");
    }
    
    void SetupDefaultObstacleData(ObstacleGenerator generator)
    {
        // Encontrar obstáculos de prueba
        GameObject[] testObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        
        if (testObstacles.Length == 0)
        {
            Debug.LogWarning("No test obstacles found! Create them first.");
            return;
        }
        
        // Crear array de datos de obstáculos
        var obstacleDataList = new List<ObstacleGenerator.ObstacleData>();
        
        foreach (GameObject obstacle in testObstacles)
        {
            var data = new ObstacleGenerator.ObstacleData();
            data.prefab = obstacle;
            data.obstacleName = obstacle.name;
            data.minDifficulty = 0f;
            data.spawnWeight = 1f;
            data.canBeInPattern = true;
            
            // Determinar tipo basado en componentes
            if (obstacle.GetComponent<MovingObstacle>() != null)
            {
                data.type = ObstacleGenerator.ObstacleType.Moving;
                data.minDifficulty = 0.2f;
            }
            else if (obstacle.GetComponent<RotatingObstacle>() != null)
            {
                data.type = ObstacleGenerator.ObstacleType.Rotating;
                data.minDifficulty = 0.3f;
            }
            else if (obstacle.GetComponent<TemporalObstacle>() != null)
            {
                data.type = ObstacleGenerator.ObstacleType.Temporal;
                data.minDifficulty = 0.4f;
            }
            else
            {
                data.type = ObstacleGenerator.ObstacleType.Static;
            }
            
            obstacleDataList.Add(data);
        }
        
        generator.availableObstacles = obstacleDataList.ToArray();
    }
    
    void SetupDefaultPatterns(ObstacleGenerator generator)
    {
        var patterns = new List<ObstacleGenerator.ObstaclePattern>();
        
        // Patrón 1: Línea de obstáculos estáticos
        var linePattern = new ObstacleGenerator.ObstaclePattern();
        linePattern.patternName = "Static Line";
        linePattern.minDifficulty = 0.1f;
        linePattern.patternWeight = 1f;
        linePattern.obstacles = new ObstacleGenerator.PatternObstacle[]
        {
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 0, localPosition = new Vector3(-2f, 0f, 0f), delay = 0f },
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 0, localPosition = new Vector3(2f, 0f, 0f), delay = 0f }
        };
        patterns.Add(linePattern);
        
        // Patrón 2: Zigzag de obstáculos móviles
        var zigzagPattern = new ObstacleGenerator.ObstaclePattern();
        zigzagPattern.patternName = "Moving Zigzag";
        zigzagPattern.minDifficulty = 0.3f;
        zigzagPattern.patternWeight = 0.8f;
        zigzagPattern.obstacles = new ObstacleGenerator.PatternObstacle[]
        {
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 1, localPosition = new Vector3(-3f, 0f, 0f), delay = 0f },
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 1, localPosition = new Vector3(3f, 0f, 0f), delay = 5f },
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 1, localPosition = new Vector3(0f, 0f, 0f), delay = 10f }
        };
        patterns.Add(zigzagPattern);
        
        // Patrón 3: Combo complejo (alta dificultad)
        var comboPattern = new ObstacleGenerator.ObstaclePattern();
        comboPattern.patternName = "Complex Combo";
        comboPattern.minDifficulty = 0.6f;
        comboPattern.patternWeight = 0.5f;
        comboPattern.obstacles = new ObstacleGenerator.PatternObstacle[]
        {
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 2, localPosition = new Vector3(-2f, 0f, 0f), delay = 0f },
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 3, localPosition = new Vector3(0f, 0f, 0f), delay = 3f },
            new ObstacleGenerator.PatternObstacle { obstacleIndex = 2, localPosition = new Vector3(2f, 0f, 0f), delay = 6f }
        };
        patterns.Add(comboPattern);
        
        generator.obstaclePatterns = patterns.ToArray();
    }
    
    [ContextMenu("Test Obstacle Collision")]
    void TestObstacleCollision()
    {
        Debug.Log("Testing obstacle collision system...");
        
        // Buscar el jugador y un obstáculo para probar
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        ObstacleCollision testObstacle = FindObjectOfType<ObstacleCollision>();
        
        if (player != null && testObstacle != null)
        {
            // Simular colisión
            testObstacle.SendMessage("HandlePlayerCollision", player.gameObject, SendMessageOptions.DontRequireReceiver);
            Debug.Log("Collision test executed!");
        }
        else
        {
            Debug.LogWarning("Need both player and obstacle with ObstacleCollision component to test!");
        }
    }
    
    [ContextMenu("Clear All Test Obstacles")]
    void ClearAllTestObstacles()
    {
        GameObject testParent = GameObject.Find("Test Obstacles");
        if (testParent != null)
        {
            if (Application.isPlaying)
                Destroy(testParent);
            else
                DestroyImmediate(testParent);
            Debug.Log("All test obstacles cleared!");
        }
    }
    
    [ContextMenu("Create Single Moving Obstacle")]
    void CreateSingleMovingObstacle()
    {
        GameObject parent = GameObject.Find("Test Obstacles");
        if (parent == null)
        {
            parent = new GameObject("Test Obstacles");
        }
        
        GameObject movingObs = CreateMovingObstacle(parent.transform);
        Debug.Log($"Created single moving obstacle: {movingObs.name}");
    }
    
    [ContextMenu("Create Single Rotating Obstacle")]
    void CreateSingleRotatingObstacle()
    {
        GameObject parent = GameObject.Find("Test Obstacles");
        if (parent == null)
        {
            parent = new GameObject("Test Obstacles");
        }
        
        GameObject rotatingObs = CreateRotatingObstacle(parent.transform);
        Debug.Log($"Created single rotating obstacle: {rotatingObs.name}");
    }
    
    [ContextMenu("Validate Obstacle Tags")]
    void ValidateObstacleTags()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        Debug.Log($"Found {obstacles.Length} obstacles with 'Obstacle' tag");
        
        foreach (GameObject obs in obstacles)
        {
            ObstacleCollision collision = obs.GetComponent<ObstacleCollision>();
            if (collision == null)
            {
                Debug.LogWarning($"Obstacle '{obs.name}' missing ObstacleCollision component!");
            }
            else
            {
                Debug.Log($"✓ Obstacle '{obs.name}' properly configured");
            }
        }
    }
    
    [ContextMenu("Test All Obstacle Types")]
    void TestAllObstacleTypes()
    {
        Debug.Log("=== TESTING ALL OBSTACLE TYPES ===");
        
        // Test Moving Obstacles
        MovingObstacle[] movingObs = FindObjectsOfType<MovingObstacle>();
        Debug.Log($"Moving Obstacles: {movingObs.Length}");
        
        // Test Rotating Obstacles  
        RotatingObstacle[] rotatingObs = FindObjectsOfType<RotatingObstacle>();
        Debug.Log($"Rotating Obstacles: {rotatingObs.Length}");
        
        // Test Temporal Obstacles
        TemporalObstacle[] temporalObs = FindObjectsOfType<TemporalObstacle>();
        Debug.Log($"Temporal Obstacles: {temporalObs.Length}");
        
        // Test Collision Components
        ObstacleCollision[] collisionObs = FindObjectsOfType<ObstacleCollision>();
        Debug.Log($"Obstacles with Collision: {collisionObs.Length}");
        
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar configuración visual
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, obstacleSize);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"Test Obstacles: {testObstacleCount}\nSize: {obstacleSize}");
        #endif
    }
}