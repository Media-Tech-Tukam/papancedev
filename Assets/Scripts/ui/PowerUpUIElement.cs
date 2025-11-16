using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ============================================
// POWER-UP UI ELEMENT - Elemento individual de power-up
// ============================================
[System.Serializable]
public class PowerUpUIElement : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public Image fillImage;
    public TextMeshProUGUI timerText;
    public GameObject container;
    
    [Header("Power-Up Settings")]
    public CollectibleCollision.PowerUpType powerUpType;
    public Sprite powerUpIcon;
    public Color powerUpColor = Color.white;
    public Color fillColor = Color.green;
    
    [Header("Animation")]
    public bool animateActivation = true;
    public float activationAnimationDuration = 0.5f;
    public AnimationCurve activationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Effects")]
    public ParticleSystem activationParticles;
    public AudioClip activationSound;
    public bool pulseWhenActive = true;
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.1f;
    
    private bool isActive = false;
    private float timeLeft = 0f;
    private float totalDuration = 0f;
    private Vector3 originalScale;
    private Color originalIconColor;
    private Coroutine pulseCoroutine;
    private AudioSource audioSource;
    
    void Start()
    {
        // Guardar valores originales
        originalScale = transform.localScale;
        if (iconImage != null)
            originalIconColor = iconImage.color;
        
        // Configurar audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Configurar componentes iniciales
        SetupUI();
        
        // Desactivar inicialmente
        ResetPowerUp();
    }
    
    void SetupUI()
    {
        // Configurar icono
        if (iconImage != null && powerUpIcon != null)
        {
            iconImage.sprite = powerUpIcon;
            iconImage.color = powerUpColor;
        }
        
        // Configurar barra de progreso
        if (fillImage != null)
        {
            fillImage.color = fillColor;
            fillImage.fillAmount = 0f;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
        }
        
        // Configurar texto de timer
        if (timerText != null)
        {
            timerText.text = "";
        }
        
        // Configurar container
        if (container != null)
        {
            container.SetActive(false);
        }
    }
    
    public void UpdatePowerUp(bool active, float timeRemaining, float duration)
    {
        bool wasActive = isActive;
        isActive = active;
        timeLeft = timeRemaining;
        totalDuration = duration;
        
        // Si se activó por primera vez
        if (active && !wasActive)
        {
            ActivatePowerUp();
        }
        // Si se desactivó
        else if (!active && wasActive)
        {
            DeactivatePowerUp();
        }
        
        // Actualizar UI si está activo
        if (isActive)
        {
            UpdateActiveUI();
        }
    }
    
    void ActivatePowerUp()
    {
        Debug.Log($"🎨 Activating UI for power-up: {powerUpType}");
        
        // Mostrar container
        if (container != null)
            container.SetActive(true);
        
        // Animar activación
        if (animateActivation)
        {
            StartCoroutine(PlayActivationAnimation());
        }
        
        // Iniciar efecto de pulso
        if (pulseWhenActive)
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseEffect());
        }
        
        // Efectos de activación
        PlayActivationEffect();
    }
    
    void DeactivatePowerUp()
    {
        Debug.Log($"🎨 Deactivating UI for power-up: {powerUpType}");
        
        // Detener efectos
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Restaurar escala original
        transform.localScale = originalScale;
        
        // Ocultar container
        if (container != null)
            container.SetActive(false);
        
        // Resetear valores
        if (fillImage != null)
            fillImage.fillAmount = 0f;
        
        if (timerText != null)
            timerText.text = "";
    }
    
    void UpdateActiveUI()
    {
        // Actualizar barra de progreso
        if (fillImage != null && totalDuration > 0f)
        {
            float fillAmount = timeLeft / totalDuration;
            fillImage.fillAmount = fillAmount;
            
            // Cambiar color según el tiempo restante
            Color currentFillColor = fillColor;
            if (fillAmount < 0.3f)
            {
                // Parpadear en rojo cuando queda poco tiempo
                float blinkSpeed = 5f;
                float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                currentFillColor = Color.Lerp(Color.red, fillColor, blink);
            }
            fillImage.color = currentFillColor;
        }
        
        // Actualizar texto de timer
        if (timerText != null)
        {
            if (timeLeft > 0f)
            {
                timerText.text = timeLeft.ToString("F1") + "s";
            }
            else
            {
                timerText.text = "";
            }
        }
    }
    
    public void PlayActivationEffect()
    {
        // Reproducir sonido
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Reproducir partículas
        if (activationParticles != null)
        {
            activationParticles.Play();
        }
        
        // Efecto visual adicional en el icono
        if (iconImage != null)
        {
            StartCoroutine(IconFlashEffect());
        }
    }
    
    IEnumerator PlayActivationAnimation()
    {
        Vector3 targetScale = originalScale * 1.2f;
        float elapsed = 0f;
        
        // Animar escala hacia arriba
        while (elapsed < activationAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = activationCurve.Evaluate(elapsed / (activationAnimationDuration * 0.5f));
            
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }
        
        // Animar escala hacia abajo
        elapsed = 0f;
        while (elapsed < activationAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / (activationAnimationDuration * 0.5f);
            
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    IEnumerator IconFlashEffect()
    {
        if (iconImage == null) yield break;
        
        Color flashColor = Color.white;
        float flashDuration = 0.2f;
        
        // Flash blanco
        iconImage.color = flashColor;
        yield return new WaitForSecondsRealtime(flashDuration);
        
        // Volver al color original
        iconImage.color = originalIconColor;
    }
    
    IEnumerator PulseEffect()
    {
        while (isActive)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseIntensity;
            Vector3 pulseScale = originalScale * (1f + pulse);
            transform.localScale = pulseScale;
            
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    public void ResetPowerUp()
    {
        isActive = false;
        timeLeft = 0f;
        totalDuration = 0f;
        
        if (container != null)
            container.SetActive(false);
        
        if (fillImage != null)
            fillImage.fillAmount = 0f;
        
        if (timerText != null)
            timerText.text = "";
        
        transform.localScale = originalScale;
        
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }
    
    // ============================================
    // MÉTODOS PÚBLICOS PARA CONFIGURACIÓN
    // ============================================
    
    public void SetPowerUpData(CollectibleCollision.PowerUpType type, Sprite icon, Color color)
    {
        powerUpType = type;
        powerUpIcon = icon;
        powerUpColor = color;
        
        SetupUI();
    }
    
    public void SetActivationSound(AudioClip sound)
    {
        activationSound = sound;
    }
    
    public void SetFillColor(Color color)
    {
        fillColor = color;
        if (fillImage != null)
            fillImage.color = color;
    }
    
    public void EnablePulse(bool enable)
    {
        pulseWhenActive = enable;
        
        if (!enable && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            transform.localScale = originalScale;
        }
    }
    
    public void SetPulseSettings(float speed, float intensity)
    {
        pulseSpeed = speed;
        pulseIntensity = intensity;
    }
    
    public void SetAnimationSettings(float duration, AnimationCurve curve)
    {
        activationAnimationDuration = duration;
        activationCurve = curve;
    }
    
    // ============================================
    // MÉTODOS DE CONSULTA
    // ============================================
    
    public bool IsActive()
    {
        return isActive;
    }
    
    public float GetTimeLeft()
    {
        return timeLeft;
    }
    
    public float GetProgress()
    {
        return totalDuration > 0f ? timeLeft / totalDuration : 0f;
    }
    
    public CollectibleCollision.PowerUpType GetPowerUpType()
    {
        return powerUpType;
    }
    
    public string GetFormattedTimeLeft()
    {
        if (timeLeft <= 0f)
            return "0.0s";
        
        return timeLeft.ToString("F1") + "s";
    }
    
    public string GetPowerUpName()
    {
        switch (powerUpType)
        {
            case CollectibleCollision.PowerUpType.SpeedBoost:
                return "Speed Boost";
            case CollectibleCollision.PowerUpType.Magnet:
                return "Magnet";
            case CollectibleCollision.PowerUpType.DoublePoints:
                return "Double Points";
            case CollectibleCollision.PowerUpType.Shield:
                return "Shield";
            case CollectibleCollision.PowerUpType.SlowMotion:
                return "Slow Motion";
            default:
                return "Unknown";
        }
    }
    
    // ============================================
    // MÉTODOS DE EFECTOS ADICIONALES
    // ============================================
    
    public void PlayCustomEffect(Color effectColor, float duration = 0.5f)
    {
        StartCoroutine(CustomColorEffect(effectColor, duration));
    }
    
    IEnumerator CustomColorEffect(Color targetColor, float duration)
    {
        if (iconImage == null) yield break;
        
        Color startColor = iconImage.color;
        float elapsed = 0f;
        
        // Fade to target color
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / (duration * 0.5f);
            iconImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        // Fade back to original
        elapsed = 0f;
        while (elapsed < duration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / (duration * 0.5f);
            iconImage.color = Color.Lerp(targetColor, startColor, progress);
            yield return null;
        }
        
        iconImage.color = startColor;
    }
    
    public void SetWarningMode(bool warning)
    {
        if (fillImage == null) return;
        
        if (warning)
        {
            StartCoroutine(WarningBlink());
        }
        else
        {
            StopAllCoroutines();
            fillImage.color = fillColor;
        }
    }
    
    IEnumerator WarningBlink()
    {
        while (isActive && timeLeft <= totalDuration * 0.3f)
        {
            // Blink between red and original color
            fillImage.color = Color.red;
            yield return new WaitForSecondsRealtime(0.2f);
            fillImage.color = fillColor;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
    
    // ============================================
    // EVENTOS UNITY
    // ============================================
    
    void OnDestroy()
    {
        // Cleanup coroutines
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        StopAllCoroutines();
    }
    
    void OnDisable()
    {
        // Stop effects when disabled
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        transform.localScale = originalScale;
    }
    
    // ============================================
    // CONFIGURACIÓN POR DEFECTO PARA POWER-UPS
    // ============================================
    
    public void ConfigureForSpeedBoost()
    {
        powerUpType = CollectibleCollision.PowerUpType.SpeedBoost;
        powerUpColor = Color.cyan;
        fillColor = Color.blue;
        pulseSpeed = 3f;
        pulseIntensity = 0.15f;
        SetupUI();
    }
    
    public void ConfigureForMagnet()
    {
        powerUpType = CollectibleCollision.PowerUpType.Magnet;
        powerUpColor = Color.yellow;
        fillColor = new Color(1f, 0.5f, 0f, 1f); // Orange color
        pulseSpeed = 2f;
        pulseIntensity = 0.1f;
        SetupUI();
    }
    
    public void ConfigureForDoublePoints()
    {
        powerUpType = CollectibleCollision.PowerUpType.DoublePoints;
        powerUpColor = Color.green;
        fillColor = new Color(0.5f, 1f, 0f, 1f); // Lime color
        pulseSpeed = 2.5f;
        pulseIntensity = 0.12f;
        SetupUI();
    }
    
    public void ConfigureForShield()
    {
        powerUpType = CollectibleCollision.PowerUpType.Shield;
        powerUpColor = Color.white;
        fillColor = Color.gray;
        pulseSpeed = 1.5f;
        pulseIntensity = 0.08f;
        SetupUI();
    }
    
    // ============================================
    // INTEGRATION HELPERS
    // ============================================
    
    public void IntegrateWithAudioManager()
    {
        if (AudioManager.Instance != null && activationSound == null)
        {
            // Try to get appropriate sound from AudioManager
            switch (powerUpType)
            {
                case CollectibleCollision.PowerUpType.SpeedBoost:
                    // AudioManager will have specific power-up sounds
                    break;
                case CollectibleCollision.PowerUpType.Magnet:
                    // Implementation depends on your AudioManager setup
                    break;
                // Add other cases as needed
            }
        }
    }
    
    public void SyncWithCollectibleManager(CollectibleManager manager)
    {
        if (manager == null) return;
        
        bool shouldBeActive = manager.HasActivePowerUp(powerUpType);
        float timeRemaining = manager.GetPowerUpTimeLeft(powerUpType);
        
        // Sync UI state with actual game state
        if (shouldBeActive != isActive)
        {
            UpdatePowerUp(shouldBeActive, timeRemaining, timeRemaining);
        }
    }
    
    // ============================================
    // MÉTODOS DE DEBUG Y TESTING
    // ============================================
    
    [ContextMenu("Test Activation")]
    void TestActivation()
    {
        Debug.Log($"🧪 Testing activation for {powerUpType}");
        UpdatePowerUp(true, 5f, 5f);
        Invoke(nameof(TestDeactivation), 5f);
    }
    
    void TestDeactivation()
    {
        Debug.Log($"🧪 Testing deactivation for {powerUpType}");
        UpdatePowerUp(false, 0f, 0f);
    }
    
    [ContextMenu("Play Activation Effect")]
    void TestActivationEffect()
    {
        Debug.Log($"🧪 Testing activation effect for {powerUpType}");
        PlayActivationEffect();
    }
    
    [ContextMenu("Test Custom Effect")]
    void TestCustomEffect()
    {
        PlayCustomEffect(Color.cyan, 1f);
    }
    
    [ContextMenu("Test Warning Mode")]
    void TestWarningMode()
    {
        UpdatePowerUp(true, 1f, 5f); // Low time to trigger warning
    }
    
    [ContextMenu("Print Power-Up Info")]
    void PrintPowerUpInfo()
    {
        Debug.Log("=== POWER-UP UI INFO ===");
        Debug.Log($"Type: {powerUpType}");
        Debug.Log($"Name: {GetPowerUpName()}");
        Debug.Log($"Active: {isActive}");
        Debug.Log($"Time Left: {GetFormattedTimeLeft()}");
        Debug.Log($"Progress: {GetProgress():P}");
        Debug.Log($"Container Active: {(container != null ? container.activeInHierarchy : "null")}");
        Debug.Log("========================");
    }
}