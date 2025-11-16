using UnityEngine;
using System.Collections.Generic;

// ============================================
// COLLECTIBLE SETUP HELPER - Para configuración rápida
// ============================================
public class CollectibleSetupHelper : MonoBehaviour
{
    [Header("Quick Setup")]
    public bool createTestCollectibles = true;
    public Material[] collectibleMaterials; // [0]Coin, [1]Gem, [2]PowerCoin, [3]Bonus, [4]PowerUp
    
    [Header("Test Collectible Settings")]
    public Vector3 collectibleSize = new Vector3(0.8f, 0.8f, 0.8f);
    public bool setupAudioSources = false;
    public AudioClip defaultCollectSound;
    
    [Header("Material Colors")]
    public Color coinColor = Color.yellow;
    public Color gemColor = Color.blue;
    public Color powerCoinColor = Color.green;
    public Color bonusColor = Color.magenta;
    public Color powerUpColor = Color.red;
    
    void Start()
    {
        if (createTestCollectibles)
        {
            CreateTestCollectibles();
        }
    }
    
    [ContextMenu("Create Test Collectibles")]
    void CreateTestCollectibles()
    {
        Debug.Log("Creating test collectibles...");
        
        // Crear carpeta para coleccionables
        GameObject collectibleParent = new GameObject("Test Collectibles");
        
        // Crear diferentes tipos de coleccionables
        CreateCoinCollectible(collectibleParent.transform);
        CreateGemCollectible(collectibleParent.transform);
        CreatePowerCoinCollectible(collectibleParent.transform);
        CreateBonusItemCollectible(collectibleParent.transform);
        CreatePowerUpCollectible(collectibleParent.transform);
        
        Debug.Log("Test collectibles created! Assign them to the CollectibleGenerator.");
    }
    
    GameObject CreateCoinCollectible(Transform parent)
    {
        GameObject collectible = CreateBasicCollectible("Coin_Collectible", parent, coinColor);
        
        // Configurar como moneda básica
        CollectibleCollision collision = collectible.AddComponent<CollectibleCollision>();
        collision.collectibleType = CollectibleCollision.CollectibleType.Coin;
        collision.pointValue = 1;
        collision.isMagnetic = false;
        
        // Comportamiento flotante suave
        FloatingCollectible floating = collectible.AddComponent<FloatingCollectible>();
        floating.floatHeight = 0.3f;
        floating.floatSpeed = 1.5f;
        
        // Rotación lenta
        RotatingCollectible rotating = collectible.AddComponent<RotatingCollectible>();
        rotating.rotationSpeed = 45f;
        
        return collectible;
    }
    
    GameObject CreateGemCollectible(Transform parent)
    {
        GameObject collectible = CreateBasicCollectible("Gem_Collectible", parent, gemColor);
        
        // Usar forma de diamante (octahedron)
        DestroyImmediate(collectible.GetComponent<MeshFilter>());
        DestroyImmediate(collectible.GetComponent<MeshRenderer>());
        
        // Crear mesh de diamante
        GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
        diamond.transform.parent = collectible.transform;
        diamond.transform.localPosition = Vector3.zero;
        diamond.transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);
        diamond.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
        diamond.GetComponent<Renderer>().material.color = gemColor;
        
        // Configurar como gema
        CollectibleCollision collision = collectible.AddComponent<CollectibleCollision>();
        collision.collectibleType = CollectibleCollision.CollectibleType.Gem;
        collision.pointValue = 5;
        collision.isMagnetic = true;
        collision.magnetRange = 3f;
        
        // Comportamiento pulsante
        PulsingCollectible pulsing = collectible.AddComponent<PulsingCollectible>();
        pulsing.pulseScale = 0.2f;
        pulsing.pulseSpeed = 2f;
        pulsing.pulseColor = true;
        pulsing.baseColor = gemColor;
        pulsing.pulseColorTarget = Color.white;
        
        // Rotación rápida
        RotatingCollectible rotating = collectible.AddComponent<RotatingCollectible>();
        rotating.rotationSpeed = 120f;
        rotating.wobbleRotation = true;
        
        return collectible;
    }
    
    GameObject CreatePowerCoinCollectible(Transform parent)
    {
        GameObject collectible = CreateBasicCollectible("PowerCoin_Collectible", parent, powerCoinColor);
        
        // Hacer más grande que moneda normal
        collectible.transform.localScale = collectibleSize * 1.3f;
        
        // Configurar como power coin
        CollectibleCollision collision = collectible.AddComponent<CollectibleCollision>();
        collision.collectibleType = CollectibleCollision.CollectibleType.PowerCoin;
        collision.pointValue = 10;
        collision.isMagnetic = true;
        collision.magnetRange = 4f;
        
        // Comportamiento flotante y pulsante
        FloatingCollectible floating = collectible.AddComponent<FloatingCollectible>();
        floating.floatHeight = 0.8f;
        floating.floatSpeed = 2f;
        
        PulsingCollectible pulsing = collectible.AddComponent<PulsingCollectible>();
        pulsing.pulseScale = 0.4f;
        pulsing.pulseSpeed = 3f;
        pulsing.pulseColor = true;
        pulsing.baseColor = powerCoinColor;
        pulsing.pulseColorTarget = Color.yellow;
        
        // Rastro visual
        TrailCollectible trail = collectible.AddComponent<TrailCollectible>();
        trail.trailTime = 0.8f;
        trail.trailWidth = 0.15f;
        trail.trailColor = powerCoinColor;
        
        return collectible;
    }
    
    GameObject CreateBonusItemCollectible(Transform parent)
    {
        GameObject collectible = CreateBasicCollectible("BonusItem_Collectible", parent, bonusColor);
        
        // Forma de estrella (usando múltiples cubos)
        CreateStarShape(collectible);
        
        // Configurar como bonus item
        CollectibleCollision collision = collectible.AddComponent<CollectibleCollision>();
        collision.collectibleType = CollectibleCollision.CollectibleType.BonusItem;
        collision.pointValue = 25;
        collision.isMagnetic = true;
        collision.magnetRange = 6f;
        
        // Comportamiento orbital
        OrbitingCollectible orbiting = collectible.AddComponent<OrbitingCollectible>();
        orbiting.orbitRadius = 1.5f;
        orbiting.orbitSpeed = 2f;
        orbiting.ellipticalOrbit = true;
        orbiting.SetOrbitCenter(collectible.transform.position);
        
        // Rotación compleja
        RotatingCollectible rotating = collectible.AddComponent<RotatingCollectible>();
        rotating.rotationSpeed = 180f;
        rotating.wobbleRotation = true;
        rotating.wobbleAmount = 30f;
        
        return collectible;
    }
    
    GameObject CreatePowerUpCollectible(Transform parent)
    {
        GameObject collectible = CreateBasicCollectible("PowerUp_Collectible", parent, powerUpColor);
        
        // Forma especial para power-up (capsule)
        DestroyImmediate(collectible.GetComponent<MeshFilter>());
        DestroyImmediate(collectible.GetComponent<MeshRenderer>());
        
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.parent = collectible.transform;
        capsule.transform.localPosition = Vector3.zero;
        capsule.transform.localScale = Vector3.one;
        capsule.GetComponent<Renderer>().material.color = powerUpColor;
        
        // Configurar como power-up
        CollectibleCollision collision = collectible.AddComponent<CollectibleCollision>();
        collision.collectibleType = CollectibleCollision.CollectibleType.PowerUp;
        collision.pointValue = 0; // Power-ups no dan puntos directos
        collision.powerUpType = CollectibleCollision.PowerUpType.SpeedBoost; // Por defecto
        collision.powerUpDuration = 5f;
        collision.powerUpStrength = 2f;
        collision.isMagnetic = true;
        collision.magnetRange = 5f;
        
        // Comportamiento combo complejo
        ComboCollectible combo = collectible.AddComponent<ComboCollectible>();
        combo.ConfigureCombo(true, true, true, true); // Todos los efectos
        
        // Efecto magnético especial
        MagneticCollectible magnetic = collectible.AddComponent<MagneticCollectible>();
        magnetic.magneticRange = 3f;
        magnetic.magneticStrength = 2f;
        magnetic.showMagneticField = true;
        
        return collectible;
    }
    
    void CreateStarShape(GameObject parent)
    {
        // Crear forma de estrella con 5 puntas usando cubos pequeños
        Vector3[] starPoints = {
            new Vector3(0f, 1f, 0f),      // Punta superior
            new Vector3(0.3f, 0.3f, 0f),   // Derecha superior
            new Vector3(1f, 0f, 0f),       // Punta derecha
            new Vector3(0.3f, -0.3f, 0f),  // Derecha inferior
            new Vector3(0f, -1f, 0f),      // Punta inferior
            new Vector3(-0.3f, -0.3f, 0f), // Izquierda inferior
            new Vector3(-1f, 0f, 0f),      // Punta izquierda
            new Vector3(-0.3f, 0.3f, 0f)   // Izquierda superior
        };
        
        for (int i = 0; i < starPoints.Length; i++)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
            point.transform.parent = parent.transform;
            point.transform.localPosition = starPoints[i] * 0.3f;
            point.transform.localScale = Vector3.one * 0.2f;
            point.GetComponent<Renderer>().material.color = bonusColor;
        }
        
        // Remover el collider original del parent
        Collider parentCollider = parent.GetComponent<Collider>();
        if (parentCollider != null)
        {
            DestroyImmediate(parentCollider);
        }
    }
    
    GameObject CreateBasicCollectible(string name, Transform parent, Color color)
    {
        // Crear esfera básica
        GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        collectible.name = name;
        collectible.transform.parent = parent;
        collectible.transform.localScale = collectibleSize;
        
        // Configurar material
        Renderer renderer = collectible.GetComponent<Renderer>();
        if (collectibleMaterials != null && collectibleMaterials.Length > 0)
        {
            // Usar material asignado si está disponible
            renderer.material = collectibleMaterials[0];
        }
        
        renderer.material.color = color;
        
        // Configurar collider como trigger
        SphereCollider collider = collectible.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 1.2f; // Radio generoso para fácil recolección
        
        // Configurar tag
        collectible.tag = "Collectible";
        
        return collectible;
    }
    
    [ContextMenu("Setup Collectible Generator")]
    void SetupCollectibleGenerator()
    {
        var generator = FindObjectOfType<CollectibleGenerator>();
        if (generator == null)
        {
            Debug.LogError("No CollectibleGenerator found in scene! Use 'Create Simple Collectible Generator' first.");
            return;
        }
        
        Debug.Log("CollectibleGenerator found!");
        Debug.Log("📋 Manual Configuration Steps:");
        Debug.Log("1. Expand 'Available Collectibles' in the inspector");
        Debug.Log("2. Set array size to 5");
        Debug.Log("3. Drag test collectible prefabs to each slot");
        Debug.Log("4. Configure each collectible's properties manually");
        Debug.Log("✅ Generator is ready for manual setup!");
    }
    
    [ContextMenu("Show Configuration Guide")]
    void ShowConfigurationGuide()
    {
        Debug.Log("=== COLLECTIBLE SYSTEM CONFIGURATION GUIDE ===");
        Debug.Log("");
        Debug.Log("🔧 COLLECTIBLE GENERATOR SETUP:");
        Debug.Log("1. Available Collectibles Array:");
        Debug.Log("   - Size: 5");
        Debug.Log("   - Slot 0: Coin (Type: Coin, Behavior: Static, Value: 1)");
        Debug.Log("   - Slot 1: Gem (Type: Gem, Behavior: Floating, Value: 5)");
        Debug.Log("   - Slot 2: PowerCoin (Type: PowerCoin, Behavior: Pulsing, Value: 10)");
        Debug.Log("   - Slot 3: BonusItem (Type: BonusItem, Behavior: Orbiting, Value: 25)");
        Debug.Log("   - Slot 4: PowerUp (Type: PowerUp, Behavior: Combo, Value: 0)");
        Debug.Log("");
        Debug.Log("⚙️ RECOMMENDED SETTINGS:");
        Debug.Log("   - Collectible Spacing: 15");
        Debug.Log("   - Min Collectible Distance: 8");
        Debug.Log("   - Collectible Density: 0.7");
        Debug.Log("   - Max Collectibles In Memory: 30");
        Debug.Log("   - Power Up Spawn Chance: 0.1");
        Debug.Log("");
        Debug.Log("🎯 After setup, test in play mode!");
    }
    
    [ContextMenu("Test Collectible Collection")]
    void TestCollectibleCollection()
    {
        Debug.Log("Testing collectible collection system...");
        
        // Buscar el jugador y un coleccionable para probar
        ImprovedSplineFollower player = FindObjectOfType<ImprovedSplineFollower>();
        CollectibleCollision testCollectible = FindObjectOfType<CollectibleCollision>();
        
        if (player != null && testCollectible != null)
        {
            // Simular recolección
            testCollectible.HandlePlayerCollection(player.gameObject);
            Debug.Log("Collection test executed!");
        }
        else
        {
            Debug.LogWarning("Need both player and collectible with CollectibleCollision component to test!");
        }
    }
    
    [ContextMenu("Clear All Test Collectibles")]
    void ClearAllTestCollectibles()
    {
        GameObject testParent = GameObject.Find("Test Collectibles");
        if (testParent != null)
        {
            if (Application.isPlaying)
                Destroy(testParent);
            else
                DestroyImmediate(testParent);
            Debug.Log("All test collectibles cleared!");
        }
    }
    
    [ContextMenu("Create Simple Collectible Manager")]
    void CreateCollectibleManager()
    {
        var existingManager = FindObjectOfType<CollectibleManager>();
        if (existingManager != null)
        {
            Debug.LogWarning("CollectibleManager already exists!");
            return;
        }
        
        GameObject managerObject = new GameObject("Collectible Manager");
        var manager = managerObject.AddComponent<CollectibleManager>();
        
        Debug.Log("CollectibleManager created successfully!");
    }
    
    [ContextMenu("Create Simple Collectible Generator")]
    void CreateCollectibleGenerator()
    {
        var existingGenerator = FindObjectOfType<CollectibleGenerator>();
        if (existingGenerator != null)
        {
            Debug.LogWarning("CollectibleGenerator already exists!");
            return;
        }
        
        GameObject generatorObject = new GameObject("Collectible Generator");
        var generator = generatorObject.AddComponent<CollectibleGenerator>();
        
        // Configurar valores por defecto
        generator.collectibleSpacing = 15f;
        generator.minCollectibleDistance = 8f;
        generator.collectibleDensity = 0.7f;
        generator.maxCollectiblesInMemory = 30;
        
        Debug.Log("CollectibleGenerator created with default settings!");
        Debug.Log("📋 Next: Assign test collectible prefabs to the 'Available Collectibles' array");
    }
    
    [ContextMenu("Setup Complete Collectible System")]
    void SetupCompleteCollectibleSystem()
    {
        Debug.Log("=== SETTING UP COMPLETE COLLECTIBLE SYSTEM ===");
        
        // 1. Crear CollectibleManager si no existe
        CreateCollectibleManager();
        
        // 2. Crear CollectibleGenerator si no existe
        CreateCollectibleGenerator();
        
        // 3. Crear coleccionables de prueba
        CreateTestCollectibles();
        
        Debug.Log("✅ Collectible system components created!");
        Debug.Log("");
        Debug.Log("📋 FINAL STEP - Manual Configuration Required:");
        Debug.Log("1. Find the 'Collectible Generator' GameObject in your scene");
        Debug.Log("2. In the inspector, expand 'Available Collectibles'");
        Debug.Log("3. Set the size to 5 (for 5 different collectible types)");
        Debug.Log("4. Drag the prefabs from 'Test Collectibles' folder to each slot:");
        Debug.Log("   - Slot 0: Coin_Collectible");
        Debug.Log("   - Slot 1: Gem_Collectible");
        Debug.Log("   - Slot 2: PowerCoin_Collectible");
        Debug.Log("   - Slot 3: BonusItem_Collectible");
        Debug.Log("   - Slot 4: PowerUp_Collectible");
        Debug.Log("5. Configure the data for each slot (name, type, behavior, values)");
        Debug.Log("6. Test in play mode!");
        Debug.Log("");
        Debug.Log("🎯 System ready for manual configuration!");
    }
    
    [ContextMenu("Validate Collectible Tags")]
    void ValidateCollectibleTags()
    {
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        Debug.Log($"Found {collectibles.Length} objects with 'Collectible' tag");
        
        foreach (GameObject col in collectibles)
        {
            CollectibleCollision collision = col.GetComponent<CollectibleCollision>();
            if (collision == null)
            {
                Debug.LogWarning($"Collectible '{col.name}' missing CollectibleCollision component!");
            }
            else
            {
                Debug.Log($"✓ Collectible '{col.name}' properly configured - Type: {collision.collectibleType}, Value: {collision.pointValue}");
            }
        }
    }
    
    [ContextMenu("Test All Collectible Types")]
    void TestAllCollectibleTypes()
    {
        Debug.Log("=== TESTING ALL COLLECTIBLE TYPES ===");
        
        // Test Floating Collectibles
        FloatingCollectible[] floatingCols = FindObjectsOfType<FloatingCollectible>();
        Debug.Log($"Floating Collectibles: {floatingCols.Length}");
        
        // Test Rotating Collectibles  
        RotatingCollectible[] rotatingCols = FindObjectsOfType<RotatingCollectible>();
        Debug.Log($"Rotating Collectibles: {rotatingCols.Length}");
        
        // Test Pulsing Collectibles
        PulsingCollectible[] pulsingCols = FindObjectsOfType<PulsingCollectible>();
        Debug.Log($"Pulsing Collectibles: {pulsingCols.Length}");
        
        // Test Orbiting Collectibles
        OrbitingCollectible[] orbitingCols = FindObjectsOfType<OrbitingCollectible>();
        Debug.Log($"Orbiting Collectibles: {orbitingCols.Length}");
        
        // Test Collision Components
        CollectibleCollision[] collisionCols = FindObjectsOfType<CollectibleCollision>();
        Debug.Log($"Collectibles with Collision: {collisionCols.Length}");
        
        // Test Manager
        CollectibleManager manager = FindObjectOfType<CollectibleManager>();
        Debug.Log($"CollectibleManager exists: {manager != null}");
        
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar configuración visual
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, collectibleSize);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"Collectible Size: {collectibleSize}\nSetup Audio: {setupAudioSources}");
        #endif
    }
}