using UnityEngine;

// ============================================
// COLLECTIBLE COLLISION - Maneja recolección (CON DEBUG DETALLADO)
// ============================================
public class CollectibleCollision : MonoBehaviour
{
    [Header("Collectible Settings")]
    public CollectibleType collectibleType = CollectibleType.Coin;
    public int pointValue = 1;
    public float magnetRange = 5f; // Rango para efecto magnético
    public bool isMagnetic = false;
    
    [Header("Audio & Effects")]
    public AudioClip collectSound;
    public GameObject collectEffect;
    public bool destroyOnCollect = true;
    
    [Header("Power-Up Settings")]
    public PowerUpType powerUpType = PowerUpType.None;
    public float powerUpDuration = 5f;
    public float powerUpStrength = 1.5f;
    
    [Header("DEBUG SETTINGS")]
    public bool enableDetailedDebug = true;
    
    [HideInInspector]
    public float splineDistance;
    
    private bool isCollected = false;
    private ImprovedSplineFollower playerFollower;
    private CollectibleManager collectibleManager;
    private int triggerCallCount = 0; // Contador para detectar múltiples triggers
    
    public enum CollectibleType
    {
        Coin,       // Moneda básica (1 punto)
        Gem,        // Gema valiosa (5 puntos)
        PowerCoin,  // Moneda de poder (10 puntos)
        BonusItem,  // Item especial (variable)
        PowerUp     // Power-up con efecto especial
    }
    
    public enum PowerUpType
    {
        None,
        SpeedBoost,    // Aumenta velocidad temporalmente
        Magnet,        // Atrae coleccionables automáticamente
        DoublePoints,  // Duplica puntos por un tiempo
        Shield,        // Protege de obstáculos
        SlowMotion     // Ralentiza el tiempo
    }
    
    void Start()
    {
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTIBLE START] {gameObject.name} - Type: {collectibleType}, Value: {pointValue}, DestroyOnCollect: {destroyOnCollect}");
        
        // Encontrar referencias
        playerFollower = FindObjectOfType<ImprovedSplineFollower>();
        collectibleManager = FindObjectOfType<CollectibleManager>();
        
        if (enableDetailedDebug)
        {
            Debug.Log($"[COLLECTIBLE REFS] {gameObject.name} - PlayerFollower: {playerFollower != null}, Manager: {collectibleManager != null}");
        }
        
        // Configurar valores por defecto según el tipo
        ConfigureDefaultValues();
        
        // Verificar y configurar colliders
        ValidateAndSetupColliders();
        
        // Configurar tag
        gameObject.tag = "Collectible";
        
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTIBLE SETUP COMPLETE] {gameObject.name} - Ready for collection");
    }
    
    void ValidateAndSetupColliders()
    {
        // Obtener todos los colliders en este objeto y sus hijos
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        
        if (enableDetailedDebug)
            Debug.Log($"[COLLIDER CHECK] {gameObject.name} - Found {allColliders.Length} colliders");
        
        bool hasTrigger = false;
        
        foreach (Collider col in allColliders)
        {
            if (enableDetailedDebug)
                Debug.Log($"[COLLIDER DETAIL] {gameObject.name} - Collider on '{col.gameObject.name}': IsTrigger={col.isTrigger}, Type={col.GetType().Name}");
            
            if (col.isTrigger)
            {
                hasTrigger = true;
            }
            else
            {
                // Convertir a trigger
                col.isTrigger = true;
                if (enableDetailedDebug)
                    Debug.Log($"[COLLIDER FIX] {gameObject.name} - Converted collider to trigger on '{col.gameObject.name}'");
            }
        }
        
        // Si no hay colliders, agregar uno
        if (allColliders.Length == 0)
        {
            SphereCollider newCollider = gameObject.AddComponent<SphereCollider>();
            newCollider.isTrigger = true;
            newCollider.radius = 0.8f;
            
            if (enableDetailedDebug)
                Debug.Log($"[COLLIDER ADDED] {gameObject.name} - Added SphereCollider with radius 0.8");
        }
    }
    
    void Update()
    {
        // Aplicar efecto magnético si es necesario
        if (isMagnetic && playerFollower != null && !isCollected)
        {
            ApplyMagneticEffect();
        }
    }
    
    void ConfigureDefaultValues()
    {
        switch (collectibleType)
        {
            case CollectibleType.Coin:
                pointValue = 1;
                isMagnetic = false;
                break;
                
            case CollectibleType.Gem:
                pointValue = 5;
                isMagnetic = true;
                magnetRange = 3f;
                break;
                
            case CollectibleType.PowerCoin:
                pointValue = 10;
                isMagnetic = true;
                magnetRange = 4f;
                break;
                
            case CollectibleType.BonusItem:
                pointValue = 25;
                isMagnetic = true;
                magnetRange = 6f;
                break;
                
            case CollectibleType.PowerUp:
                pointValue = 0; // Los power-ups no dan puntos directos
                isMagnetic = true;
                magnetRange = 5f;
                break;
        }
    }
    
    void ApplyMagneticEffect()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerFollower.transform.position);
        
        if (distanceToPlayer <= magnetRange)
        {
            // Mover hacia el jugador
            Vector3 direction = (playerFollower.transform.position - transform.position).normalized;
            float magnetStrength = Mathf.Lerp(0f, 8f, 1f - (distanceToPlayer / magnetRange));
            
            transform.position += direction * magnetStrength * Time.deltaTime;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        triggerCallCount++;
        
        if (enableDetailedDebug)
        {
            Debug.Log($"[TRIGGER ENTER] {gameObject.name} - Call #{triggerCallCount} - Other: '{other.gameObject.name}' (Tag: {other.tag}) - IsCollected: {isCollected}");
        }
        
        if (other.CompareTag("Player") && !isCollected)
        {
            if (enableDetailedDebug)
                Debug.Log($"[TRIGGER VALID] {gameObject.name} - Valid player collision detected, processing collection...");
            
            HandlePlayerCollection(other.gameObject);
        }
        else
        {
            if (enableDetailedDebug)
            {
                string reason = "";
                if (!other.CompareTag("Player")) reason += "Not player tag";
                if (isCollected) reason += (reason.Length > 0 ? ", " : "") + "Already collected";
                Debug.Log($"[TRIGGER IGNORED] {gameObject.name} - Reason: {reason}");
            }
        }
    }
    
    public void HandlePlayerCollection(GameObject player)
    {
        if (isCollected) 
        {
            if (enableDetailedDebug)
                Debug.LogWarning($"[COLLECTION BLOCKED] {gameObject.name} - Already collected!");
            return;
        }
        
        if (enableDetailedDebug)
        {
            Debug.Log($"[COLLECTION START] {gameObject.name} - Processing collection...");
            Debug.Log($"[COLLECTION STATE] {gameObject.name} - Active: {gameObject.activeInHierarchy}, Enabled: {enabled}");
            Debug.Log($"[COLLECTION DATA] {gameObject.name} - Type: {collectibleType}, Value: {pointValue}, Distance: {splineDistance:F1}");
        }
        
        isCollected = true;
        
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTION FLAG] {gameObject.name} - isCollected set to TRUE");
        
        // Aplicar efectos según el tipo
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTION EFFECTS] {gameObject.name} - Applying effects for type: {collectibleType}");
        
        switch (collectibleType)
        {
            case CollectibleType.Coin:
            case CollectibleType.Gem:
            case CollectibleType.PowerCoin:
            case CollectibleType.BonusItem:
                ApplyPointCollection();
                break;
                
            case CollectibleType.PowerUp:
                ApplyPowerUp();
                break;
        }
        
        // Efectos visuales y audio
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTION VISUAL] {gameObject.name} - Playing visual/audio effects...");
        PlayCollectionEffects();
        
        // Notificar al manager
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTION NOTIFY] {gameObject.name} - Notifying manager...");
        NotifyCollectionManager();
        
        // Destruir o desactivar
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTION CLEANUP] {gameObject.name} - DestroyOnCollect: {destroyOnCollect}");
        
        if (destroyOnCollect)
        {
            if (enableDetailedDebug)
                Debug.Log($"[COLLECTION DESTROY] {gameObject.name} - Calling Destroy(gameObject)...");
            
            // Verificar si hay componentes que puedan interferir
            CheckForInterferingComponents();
            
            Destroy(gameObject);
            
            if (enableDetailedDebug)
                Debug.Log($"[COLLECTION DESTROYED] {gameObject.name} - Destroy() called successfully");
        }
        else
        {
            if (enableDetailedDebug)
                Debug.Log($"[COLLECTION DEACTIVATE] {gameObject.name} - Calling SetActive(false)...");
            
            gameObject.SetActive(false);
            
            if (enableDetailedDebug)
                Debug.Log($"[COLLECTION DEACTIVATED] {gameObject.name} - SetActive(false) called");
        }
        
        if (enableDetailedDebug)
            Debug.Log($"[COLLECTION COMPLETE] {gameObject.name} - Collection process finished");
    }
    
    void CheckForInterferingComponents()
    {
        if (!enableDetailedDebug) return;
        
        Component[] allComponents = GetComponents<Component>();
        Debug.Log($"[COMPONENT CHECK] {gameObject.name} - Has {allComponents.Length} components:");
        
        foreach (Component comp in allComponents)
        {
            if (comp != null)
            {
                Debug.Log($"[COMPONENT DETAIL] {gameObject.name} - Component: {comp.GetType().Name}");
            }
        }
        
        // Verificar objetos hijos
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Debug.Log($"[CHILD CHECK] {gameObject.name} - Child {i}: '{child.name}' (Active: {child.gameObject.activeInHierarchy})");
        }
    }
    
    void ApplyPointCollection()
    {
        if (enableDetailedDebug)
            Debug.Log($"[POINTS] {gameObject.name} - Applying {pointValue} points for {collectibleType}");
        
        if (collectibleManager != null)
        {
            collectibleManager.AddPoints(pointValue, collectibleType);
            if (enableDetailedDebug)
                Debug.Log($"[POINTS SUCCESS] {gameObject.name} - Points added successfully");
        }
        else
        {
            Debug.LogError($"[POINTS ERROR] {gameObject.name} - No CollectibleManager found! Points not added.");
        }
    }
    
    void ApplyPowerUp()
    {
        if (enableDetailedDebug)
            Debug.Log($"[POWERUP] {gameObject.name} - Applying power-up: {powerUpType}");
        
        if (collectibleManager != null)
        {
            collectibleManager.ActivatePowerUp(powerUpType, powerUpDuration, powerUpStrength);
        }
        
        // Aplicar efectos específicos del power-up
        switch (powerUpType)
        {
            case PowerUpType.SpeedBoost:
                ApplySpeedBoost();
                break;
                
            case PowerUpType.Magnet:
                ApplyMagnetPowerUp();
                break;
                
            case PowerUpType.DoublePoints:
                ApplyDoublePoints();
                break;
                
            case PowerUpType.Shield:
                ApplyShield();
                break;
                
            case PowerUpType.SlowMotion:
                ApplySlowMotion();
                break;
        }
    }
    
    void ApplySpeedBoost()
    {
        if (playerFollower != null)
        {
            float currentSpeed = playerFollower.GetSpeed();
            playerFollower.SetSpeed(currentSpeed * powerUpStrength);
            
            if (enableDetailedDebug)
                Debug.Log($"[SPEEDBOOST] {gameObject.name} - Speed boost applied! New speed: {currentSpeed * powerUpStrength:F1}");
        }
    }
    
    void ApplyMagnetPowerUp()
    {
        // Activar magnetismo en todos los coleccionables cercanos
        CollectibleCollision[] nearbyCollectibles = FindObjectsOfType<CollectibleCollision>();
        
        foreach (var collectible in nearbyCollectibles)
        {
            if (collectible != this && !collectible.isCollected)
            {
                collectible.isMagnetic = true;
                collectible.magnetRange = 8f; // Rango aumentado
            }
        }
        
        if (enableDetailedDebug)
            Debug.Log($"[MAGNET] {gameObject.name} - Magnet power-up activated! All collectibles are now magnetic.");
    }
    
    void ApplyDoublePoints()
    {
        if (collectibleManager != null)
        {
            collectibleManager.SetPointMultiplier(2f);
            if (enableDetailedDebug)
                Debug.Log($"[DOUBLEPOINTS] {gameObject.name} - Double points activated!");
        }
    }
    
    void ApplyShield()
    {
        if (playerFollower != null)
        {
            // Intentar usar PlayerShield si existe
            var shield = playerFollower.GetComponent<MonoBehaviour>();
            bool shieldFound = false;
            
            // Buscar componente PlayerShield usando reflection para evitar errores de compilación
            var shieldComponents = playerFollower.GetComponents<MonoBehaviour>();
            foreach (var component in shieldComponents)
            {
                if (component.GetType().Name == "PlayerShield")
                {
                    // Usar reflection para llamar ActivateShield si existe
                    var method = component.GetType().GetMethod("ActivateShield");
                    if (method != null)
                    {
                        method.Invoke(component, new object[] { powerUpDuration });
                        shieldFound = true;
                        break;
                    }
                }
            }
            
            if (!shieldFound && enableDetailedDebug)
            {
                Debug.LogWarning($"[SHIELD] {gameObject.name} - PlayerShield component not found - implement shield system for full functionality!");
            }
            
            if (enableDetailedDebug)
                Debug.Log($"[SHIELD] {gameObject.name} - Shield activated!");
        }
    }
    
    void ApplySlowMotion()
    {
        Time.timeScale = 0.5f; // Ralentizar el tiempo
        
        // Programar restauración de tiempo normal
        Invoke(nameof(RestoreNormalTime), powerUpDuration);
        
        if (enableDetailedDebug)
            Debug.Log($"[SLOWMOTION] {gameObject.name} - Slow motion activated!");
    }
    
    void RestoreNormalTime()
    {
        Time.timeScale = 1f;
        if (enableDetailedDebug)
            Debug.Log("Normal time restored.");
    }
    
    void PlayCollectionEffects()
    {
        if (enableDetailedDebug)
            Debug.Log($"[EFFECTS START] {gameObject.name} - Playing collection effects...");
        
        // Audio
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
            if (enableDetailedDebug)
                Debug.Log($"[EFFECTS AUDIO] {gameObject.name} - Audio played");
        }
        else if (enableDetailedDebug)
        {
            Debug.Log($"[EFFECTS AUDIO] {gameObject.name} - No audio clip assigned");
        }
        
        // Efectos visuales
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, transform.rotation);
            Destroy(effect, 3f); // Limpiar después de 3 segundos
            if (enableDetailedDebug)
                Debug.Log($"[EFFECTS VISUAL] {gameObject.name} - Visual effect instantiated");
        }
        else if (enableDetailedDebug)
        {
            Debug.Log($"[EFFECTS VISUAL] {gameObject.name} - No visual effect assigned");
        }
        
        if (enableDetailedDebug)
            Debug.Log($"[EFFECTS COMPLETE] {gameObject.name} - Effects finished");
    }
    
    void NotifyCollectionManager()
    {
        if (collectibleManager != null)
        {
            collectibleManager.OnCollectibleCollected(this);
            if (enableDetailedDebug)
                Debug.Log($"[MANAGER NOTIFY] {gameObject.name} - Manager notified successfully");
        }
        else if (enableDetailedDebug)
        {
            Debug.LogWarning($"[MANAGER NOTIFY] {gameObject.name} - No manager to notify");
        }
    }
    
    // Método para configurar el coleccionable desde el generator
    public void SetupCollectible(CollectibleType type, int value, float distance)
    {
        collectibleType = type;
        pointValue = value;
        splineDistance = distance;
        
        if (enableDetailedDebug)
            Debug.Log($"[SETUP] {gameObject.name} - Configured as {type} with value {value} at distance {distance:F1}");
        
        ConfigureDefaultValues();
    }
    
    // Método para activar manualmente el magnetismo (usado por power-ups)
    public void SetMagnetic(bool magnetic, float range = 5f)
    {
        isMagnetic = magnetic;
        magnetRange = range;
        
        if (enableDetailedDebug)
            Debug.Log($"[MAGNETIC] {gameObject.name} - Magnetic set to {magnetic} with range {range}");
    }
    
    // Método para obtener información del coleccionable
    public CollectibleInfo GetCollectibleInfo()
    {
        return new CollectibleInfo
        {
            type = collectibleType,
            value = pointValue,
            distance = splineDistance,
            isCollected = isCollected
        };
    }
    
    // Método para forzar destrucción (debug)
    [ContextMenu("Force Destroy")]
    void ForceDestroy()
    {
        if (enableDetailedDebug)
            Debug.Log($"[FORCE DESTROY] {gameObject.name} - Forcing destruction...");
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        if (enableDetailedDebug)
            Debug.Log($"[ON DESTROY] {gameObject.name} - OnDestroy() called - Object is being destroyed");
    }
    void LateUpdate()
    {
        if (isCollected && enableDetailedDebug)
        {
            Debug.LogWarning($"[LATE UPDATE] {gameObject.name} - Object still exists after collection! Active: {gameObject.activeInHierarchy}");
        }
    }
}

// ============================================
// COLLECTIBLE INFO - Estructura de datos
// ============================================
[System.Serializable]
public class CollectibleInfo
{
    public CollectibleCollision.CollectibleType type;
    public int value;
    public float distance;
    public bool isCollected;
}