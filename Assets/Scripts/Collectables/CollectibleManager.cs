using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ============================================
// COLLECTIBLE MANAGER - Sistema de puntuación global
// ============================================
public class CollectibleManager : MonoBehaviour
{
    [Header("Score Settings")]
    public int totalScore = 0;
    public float pointMultiplier = 1f;
    public bool saveHighScore = true;
    
    [Header("Collection Stats")]
    public int coinsCollected = 0;
    public int gemsCollected = 0;
    public int powerCoinsCollected = 0;
    public int bonusItemsCollected = 0;
    public int totalCollectibles = 0;
    
    [Header("Power-Up Management")]
    public bool hasActiveSpeedBoost = false;
    public bool hasActiveMagnet = false;
    public bool hasActiveDoublePoints = false;
    public bool hasActiveShield = false;
    
    [Header("Power-Up Timers")]
    public float speedBoostTimeLeft = 0f;
    public float magnetTimeLeft = 0f;
    public float doublePointsTimeLeft = 0f;
    public float shieldTimeLeft = 0f;
    
    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<CollectibleCollision.CollectibleType> OnCollectiblePickedUp;
    public UnityEvent<CollectibleCollision.PowerUpType> OnPowerUpActivated;
    public UnityEvent<CollectibleCollision.PowerUpType> OnPowerUpExpired;
    
    // Referencias del sistema
    private ImprovedSplineFollower playerFollower;
    private float originalPlayerSpeed;
    private List<CollectibleCollision> activeMagneticCollectibles = new List<CollectibleCollision>();
    
    // Datos de sesión
    private Dictionary<CollectibleCollision.CollectibleType, int> collectionStats;
    
    void Start()
    {
        Debug.Log("=== COLLECTIBLE MANAGER STARTING ===");
        
        // Inicializar referencias
        playerFollower = FindObjectOfType<ImprovedSplineFollower>();
        if (playerFollower != null)
        {
            originalPlayerSpeed = playerFollower.GetSpeed();
        }
        
        // Inicializar estadísticas
        InitializeCollectionStats();
        
        // Cargar high score si está habilitado
        if (saveHighScore)
        {
            LoadHighScore();
        }
        
        Debug.Log("CollectibleManager initialized successfully!");
    }
    
    void Update()
    {
        UpdatePowerUpTimers();
    }
    
    void InitializeCollectionStats()
    {
        collectionStats = new Dictionary<CollectibleCollision.CollectibleType, int>
        {
            { CollectibleCollision.CollectibleType.Coin, 0 },
            { CollectibleCollision.CollectibleType.Gem, 0 },
            { CollectibleCollision.CollectibleType.PowerCoin, 0 },
            { CollectibleCollision.CollectibleType.BonusItem, 0 },
            { CollectibleCollision.CollectibleType.PowerUp, 0 }
        };
    }
    
    void UpdatePowerUpTimers()
    {
        // Speed Boost
        if (hasActiveSpeedBoost)
        {
            speedBoostTimeLeft -= Time.deltaTime;
            if (speedBoostTimeLeft <= 0f)
            {
                DeactivateSpeedBoost();
            }
        }
        
        // Magnet
        if (hasActiveMagnet)
        {
            magnetTimeLeft -= Time.deltaTime;
            if (magnetTimeLeft <= 0f)
            {
                DeactivateMagnet();
            }
        }
        
        // Double Points
        if (hasActiveDoublePoints)
        {
            doublePointsTimeLeft -= Time.deltaTime;
            if (doublePointsTimeLeft <= 0f)
            {
                DeactivateDoublePoints();
            }
        }
        
        // Shield
        if (hasActiveShield)
        {
            shieldTimeLeft -= Time.deltaTime;
            if (shieldTimeLeft <= 0f)
            {
                DeactivateShield();
            }
        }
    }
    
    // ============================================
    // SISTEMA DE PUNTUACIÓN
    // ============================================
    
    public void AddPoints(int points, CollectibleCollision.CollectibleType type)
    {
        int finalPoints = Mathf.RoundToInt(points * pointMultiplier);
        totalScore += finalPoints;
        
        UpdateCollectionStats(type);
        
        Debug.Log($"Added {finalPoints} points ({points} base × {pointMultiplier:F1} multiplier) for {type}. Total: {totalScore}");
        
        // Disparar evento
        OnScoreChanged?.Invoke(totalScore);
    }
    
    void UpdateCollectionStats(CollectibleCollision.CollectibleType type)
    {
        collectionStats[type]++;
        totalCollectibles++;
        
        switch (type)
        {
            case CollectibleCollision.CollectibleType.Coin:
                coinsCollected++;
                break;
            case CollectibleCollision.CollectibleType.Gem:
                gemsCollected++;
                break;
            case CollectibleCollision.CollectibleType.PowerCoin:
                powerCoinsCollected++;
                break;
            case CollectibleCollision.CollectibleType.BonusItem:
                bonusItemsCollected++;
                break;
        }
    }
    
    public void SetPointMultiplier(float multiplier)
    {
        pointMultiplier = multiplier;
        Debug.Log($"Point multiplier set to {multiplier:F1}x");
    }
    
    // ============================================
    // SISTEMA DE POWER-UPS
    // ============================================
    
    public void ActivatePowerUp(CollectibleCollision.PowerUpType powerUpType, float duration, float strength)
    {
        Debug.Log($"Activating power-up: {powerUpType} for {duration:F1}s with strength {strength:F1}");
        
        switch (powerUpType)
        {
            case CollectibleCollision.PowerUpType.SpeedBoost:
                ActivateSpeedBoost(duration, strength);
                break;
                
            case CollectibleCollision.PowerUpType.Magnet:
                ActivateMagnet(duration);
                break;
                
            case CollectibleCollision.PowerUpType.DoublePoints:
                ActivateDoublePoints(duration);
                break;
                
            case CollectibleCollision.PowerUpType.Shield:
                ActivateShield(duration);
                break;
                
            case CollectibleCollision.PowerUpType.SlowMotion:
                ActivateSlowMotion(duration);
                break;
        }
        
        OnPowerUpActivated?.Invoke(powerUpType);
    }
    
    void ActivateSpeedBoost(float duration, float strength)
    {
        if (playerFollower != null)
        {
            if (!hasActiveSpeedBoost)
            {
                originalPlayerSpeed = playerFollower.GetSpeed();
            }
            
            hasActiveSpeedBoost = true;
            speedBoostTimeLeft = duration;
            playerFollower.SetSpeed(originalPlayerSpeed * strength);
        }
    }
    
    void DeactivateSpeedBoost()
    {
        if (playerFollower != null && hasActiveSpeedBoost)
        {
            playerFollower.SetSpeed(originalPlayerSpeed);
            hasActiveSpeedBoost = false;
            speedBoostTimeLeft = 0f;
            
            Debug.Log("Speed boost expired.");
            OnPowerUpExpired?.Invoke(CollectibleCollision.PowerUpType.SpeedBoost);
        }
    }
    
    void ActivateMagnet(float duration)
    {
        hasActiveMagnet = true;
        magnetTimeLeft = duration;
        
        // Activar magnetismo en todos los coleccionables activos
        CollectibleCollision[] allCollectibles = FindObjectsOfType<CollectibleCollision>();
        activeMagneticCollectibles.Clear();
        
        foreach (var collectible in allCollectibles)
        {
            if (!collectible.isMagnetic)
            {
                collectible.SetMagnetic(true, 8f);
                activeMagneticCollectibles.Add(collectible);
            }
        }
        
        Debug.Log($"Magnet activated for {duration:F1}s. Affected {activeMagneticCollectibles.Count} collectibles.");
    }
    
    void DeactivateMagnet()
    {
        hasActiveMagnet = false;
        magnetTimeLeft = 0f;
        
        // Desactivar magnetismo en coleccionables que lo activamos
        foreach (var collectible in activeMagneticCollectibles)
        {
            if (collectible != null)
            {
                collectible.SetMagnetic(false);
            }
        }
        
        activeMagneticCollectibles.Clear();
        
        Debug.Log("Magnet expired.");
        OnPowerUpExpired?.Invoke(CollectibleCollision.PowerUpType.Magnet);
    }
    
    void ActivateDoublePoints(float duration)
    {
        hasActiveDoublePoints = true;
        doublePointsTimeLeft = duration;
        SetPointMultiplier(2f);
    }
    
    void DeactivateDoublePoints()
    {
        hasActiveDoublePoints = false;
        doublePointsTimeLeft = 0f;
        SetPointMultiplier(1f);
        
        Debug.Log("Double points expired.");
        OnPowerUpExpired?.Invoke(CollectibleCollision.PowerUpType.DoublePoints);
    }
    
    void ActivateShield(float duration)
    {
        hasActiveShield = true;
        shieldTimeLeft = duration;
        
        // Activar escudo visual/lógico en el jugador
        if (playerFollower != null)
        {
            // Intentar usar PlayerShield si existe
            var shieldComponents = playerFollower.GetComponents<MonoBehaviour>();
            bool shieldFound = false;
            
            foreach (var component in shieldComponents)
            {
                if (component.GetType().Name == "PlayerShield")
                {
                    // Usar reflection para llamar ActivateShield si existe
                    var method = component.GetType().GetMethod("ActivateShield");
                    if (method != null)
                    {
                        method.Invoke(component, new object[] { duration });
                        shieldFound = true;
                        break;
                    }
                }
            }
            
            if (!shieldFound)
            {
                Debug.Log("Shield activated! (PlayerShield component not found - using basic protection)");
                // Aquí podrías integrar con tu sistema de obstáculos existente
                // Por ejemplo: playerFollower.SetInvulnerable(true, duration);
            }
            else
            {
                Debug.Log("Shield activated via PlayerShield component!");
            }
        }
    }
    
    void DeactivateShield()
    {
        hasActiveShield = false;
        shieldTimeLeft = 0f;
        
        Debug.Log("Shield expired.");
        OnPowerUpExpired?.Invoke(CollectibleCollision.PowerUpType.Shield);
    }
    
    void ActivateSlowMotion(float duration)
    {
        Time.timeScale = 0.5f;
        Invoke(nameof(DeactivateSlowMotion), duration);
    }
    
    void DeactivateSlowMotion()
    {
        Time.timeScale = 1f;
        
        Debug.Log("Slow motion expired.");
        OnPowerUpExpired?.Invoke(CollectibleCollision.PowerUpType.SlowMotion);
    }
    
    // ============================================
    // EVENTOS Y NOTIFICACIONES
    // ============================================
    
    public void OnCollectibleCollected(CollectibleCollision collectible)
    {
        CollectibleInfo info = collectible.GetCollectibleInfo();
        OnCollectiblePickedUp?.Invoke(info.type);
        
        Debug.Log($"Collection registered: {info.type} worth {info.value} points at distance {info.distance:F1}");
    }
    
    // ============================================
    // PERSISTENCIA DE DATOS
    // ============================================
    
    void LoadHighScore()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        Debug.Log($"High score loaded: {highScore}");
    }
    
    public void SaveHighScore()
    {
        if (saveHighScore)
        {
            int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
            if (totalScore > currentHighScore)
            {
                PlayerPrefs.SetInt("HighScore", totalScore);
                PlayerPrefs.Save();
                Debug.Log($"New high score saved: {totalScore}!");
            }
        }
    }
    
    // ============================================
    // MÉTODOS PÚBLICOS DE CONSULTA
    // ============================================
    
    public int GetTotalScore()
    {
        return totalScore;
    }
    
    public int GetHighScore()
    {
        return PlayerPrefs.GetInt("HighScore", 0);
    }
    
    public Dictionary<CollectibleCollision.CollectibleType, int> GetCollectionStats()
    {
        return new Dictionary<CollectibleCollision.CollectibleType, int>(collectionStats);
    }
    
    public float GetPowerUpTimeLeft(CollectibleCollision.PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case CollectibleCollision.PowerUpType.SpeedBoost:
                return speedBoostTimeLeft;
            case CollectibleCollision.PowerUpType.Magnet:
                return magnetTimeLeft;
            case CollectibleCollision.PowerUpType.DoublePoints:
                return doublePointsTimeLeft;
            case CollectibleCollision.PowerUpType.Shield:
                return shieldTimeLeft;
            default:
                return 0f;
        }
    }
    
    public bool HasActivePowerUp(CollectibleCollision.PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case CollectibleCollision.PowerUpType.SpeedBoost:
                return hasActiveSpeedBoost;
            case CollectibleCollision.PowerUpType.Magnet:
                return hasActiveMagnet;
            case CollectibleCollision.PowerUpType.DoublePoints:
                return hasActiveDoublePoints;
            case CollectibleCollision.PowerUpType.Shield:
                return hasActiveShield;
            default:
                return false;
        }
    }
    
    // ============================================
    // MÉTODOS DE RESET
    // ============================================
    
    public void ResetSession()
    {
        // Guardar high score antes de resetear
        SaveHighScore();
        
        // Reset de puntuación
        totalScore = 0;
        pointMultiplier = 1f;
        
        // Reset de estadísticas
        InitializeCollectionStats();
        coinsCollected = 0;
        gemsCollected = 0;
        powerCoinsCollected = 0;
        bonusItemsCollected = 0;
        totalCollectibles = 0;
        
        // Reset de power-ups
        DeactivateAllPowerUps();
        
        Debug.Log("Session reset!");
        OnScoreChanged?.Invoke(totalScore);
    }
    
    void DeactivateAllPowerUps()
    {
        if (hasActiveSpeedBoost) DeactivateSpeedBoost();
        if (hasActiveMagnet) DeactivateMagnet();
        if (hasActiveDoublePoints) DeactivateDoublePoints();
        if (hasActiveShield) DeactivateShield();
        
        // Restaurar tiempo normal por si acaso
        Time.timeScale = 1f;
    }
    
    // ============================================
    // DEBUG Y UTILIDADES
    // ============================================
    
    [ContextMenu("Add Test Points")]
    void AddTestPoints()
    {
        AddPoints(100, CollectibleCollision.CollectibleType.Coin);
    }
    
    [ContextMenu("Activate Test Speed Boost")]
    void ActivateTestSpeedBoost()
    {
        ActivatePowerUp(CollectibleCollision.PowerUpType.SpeedBoost, 5f, 2f);
    }
    
    [ContextMenu("Activate Test Magnet")]
    void ActivateTestMagnet()
    {
        ActivatePowerUp(CollectibleCollision.PowerUpType.Magnet, 10f, 1f);
    }
    
    [ContextMenu("Print Collection Stats")]
    void PrintCollectionStats()
    {
        Debug.Log("=== COLLECTION STATS ===");
        Debug.Log($"Total Score: {totalScore}");
        Debug.Log($"Total Collectibles: {totalCollectibles}");
        Debug.Log($"Coins: {coinsCollected}");
        Debug.Log($"Gems: {gemsCollected}");
        Debug.Log($"Power Coins: {powerCoinsCollected}");
        Debug.Log($"Bonus Items: {bonusItemsCollected}");
        Debug.Log($"Current Multiplier: {pointMultiplier:F1}x");
        Debug.Log("========================");
    }
    
    [ContextMenu("Reset All Data")]
    void ResetAllData()
    {
        ResetSession();
        PlayerPrefs.DeleteKey("HighScore");
        Debug.Log("All data reset including high score!");
    }
    
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Vector3 position = transform.position + Vector3.up * 3f;
            
            #if UNITY_EDITOR
            string info = $"Score: {totalScore}\n";
            info += $"Collectibles: {totalCollectibles}\n";
            info += $"Multiplier: {pointMultiplier:F1}x\n";
            
            if (hasActiveSpeedBoost) info += $"Speed: {speedBoostTimeLeft:F1}s\n";
            if (hasActiveMagnet) info += $"Magnet: {magnetTimeLeft:F1}s\n";
            if (hasActiveDoublePoints) info += $"2x Points: {doublePointsTimeLeft:F1}s\n";
            if (hasActiveShield) info += $"Shield: {shieldTimeLeft:F1}s\n";
            
            UnityEditor.Handles.Label(position, info);
            #endif
        }
    }
}