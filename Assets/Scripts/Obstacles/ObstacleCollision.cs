using UnityEngine;

// ============================================
// OBSTACLE COLLISION - Maneja colisiones
// ============================================
public class ObstacleCollision : MonoBehaviour
{
    [Header("Collision Settings")]
    public ObstacleEffect effectType = ObstacleEffect.SlowDown;
    public float effectStrength = 0.5f;
    public float effectDuration = 2f;
    
    [Header("Audio & Effects")]
    public AudioClip collisionSound;
    public GameObject collisionEffect;
    public bool destroyOnCollision = false;
    
    [Header("Fatal Obstacle Settings")]
    public AudioClip fatalSound; // Sonido específico para obstáculos letales
    public GameObject fatalEffect; // Efecto visual específico para muerte
    
    [HideInInspector]
    public float splineDistance;
    
    public enum ObstacleEffect
    {
        SlowDown,
        Stop,
        PushBack,
        Damage,
        Bounce,
        GameOver  // ⭐ NUEVO: Obstáculo letal
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerCollision(collision.gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision(other.gameObject);
        }
    }
    
    public void HandlePlayerCollision(GameObject player)
    {
        Debug.Log($"Player hit obstacle at distance {splineDistance:F1}!");
        
        // Obtener el spline follower
        ImprovedSplineFollower follower = player.GetComponent<ImprovedSplineFollower>();
        if (follower == null) return;
        
        // Aplicar efecto según el tipo
        switch (effectType)
        {
            case ObstacleEffect.SlowDown:
                ApplySlowDown(follower);
                break;
                
            case ObstacleEffect.Stop:
                ApplyStop(follower);
                break;
                
            case ObstacleEffect.PushBack:
                ApplyPushBack(follower);
                break;
                
            case ObstacleEffect.Damage:
                ApplyDamage(follower);
                break;
                
            case ObstacleEffect.Bounce:
                ApplyBounce(follower);
                break;
                
            case ObstacleEffect.GameOver:  // ⭐ NUEVO CASO
                ApplyGameOver(follower);
                break;
        }
        
        // Efectos visuales y audio (solo si no es game over, ya que tiene sus propios efectos)
        if (effectType != ObstacleEffect.GameOver)
        {
            PlayEffects();
        }
        
        // Destruir si es necesario
        if (destroyOnCollision)
        {
            Destroy(gameObject);
        }
    }
    
    void ApplySlowDown(ImprovedSplineFollower follower)
    {
        float newSpeed = follower.GetSpeed() * effectStrength;
        follower.SetSpeed(newSpeed);
        
        // Restaurar velocidad después del tiempo
        Invoke(nameof(RestoreSpeed), effectDuration);
    }
    
    void ApplyStop(ImprovedSplineFollower follower)
    {
        follower.SetSpeed(0f);
        Invoke(nameof(RestoreSpeed), effectDuration);
    }
    
    void ApplyPushBack(ImprovedSplineFollower follower)
    {
        // Repositionar el jugador hacia atrás
        float currentDistance = follower.GetCurrentDistance();
        follower.RepositionOnSpline(currentDistance - effectStrength);
    }
    
    void ApplyDamage(ImprovedSplineFollower follower)
    {
        Debug.Log($"Player took damage! Strength: {effectStrength}");
        // Aquí podrías integrar con un sistema de vida
    }
    
    void ApplyBounce(ImprovedSplineFollower follower)
    {
        // Aplicar un impulso lateral
        Vector3 bounceDirection = (follower.transform.position - transform.position).normalized;
        bounceDirection.y = 0; // Solo en horizontal
        
        Rigidbody rb = follower.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(bounceDirection * effectStrength, ForceMode.Impulse);
        }
    }
    
    // ⭐ NUEVO MÉTODO: Terminar el juego
    void ApplyGameOver(ImprovedSplineFollower follower)
    {
        Debug.Log("💀 GAME OVER! Player hit fatal obstacle!");
        
        // Reproducir efectos específicos de muerte
        PlayFatalEffects();
        
        // Buscar el GameUIManager y terminar el juego
        GameUIManager gameUIManager = FindObjectOfType<GameUIManager>();
        if (gameUIManager != null)
        {
            gameUIManager.GameOver();
        }
        else
        {
            Debug.LogError("GameUIManager not found! Cannot trigger game over.");
            // Fallback: pausar el juego directamente
            Time.timeScale = 0f;
        }
        
        // Detener el movimiento del jugador inmediatamente
        follower.SetSpeed(0f);
        
        // Destruir el obstáculo inmediatamente
        Destroy(gameObject);
    }
    
    void RestoreSpeed()
    {
        ImprovedSplineFollower follower = FindObjectOfType<ImprovedSplineFollower>();
        if (follower != null)
        {
            follower.SetSpeed(10f); // Velocidad base - podrías hacer esto configurable
        }
    }
    
    void PlayEffects()
    {
        // Audio
        if (collisionSound != null)
        {
            AudioSource.PlayClipAtPoint(collisionSound, transform.position);
        }
        
        // Efectos visuales
        if (collisionEffect != null)
        {
            GameObject effect = Instantiate(collisionEffect, transform.position, transform.rotation);
            Destroy(effect, 3f); // Limpiar después de 3 segundos
        }
    }
    
    // ⭐ NUEVO MÉTODO: Efectos específicos para obstáculos letales
    void PlayFatalEffects()
    {
        // Audio específico para muerte
        if (fatalSound != null)
        {
            AudioSource.PlayClipAtPoint(fatalSound, transform.position, 1f);
        }
        else if (collisionSound != null)
        {
            // Fallback al sonido normal si no hay sonido específico
            AudioSource.PlayClipAtPoint(collisionSound, transform.position, 1f);
        }
        
        // Efectos visuales específicos para muerte
        if (fatalEffect != null)
        {
            GameObject effect = Instantiate(fatalEffect, transform.position, transform.rotation);
            Destroy(effect, 5f); // Mantener más tiempo para efectos dramáticos
        }
        else if (collisionEffect != null)
        {
            // Fallback al efecto normal
            GameObject effect = Instantiate(collisionEffect, transform.position, transform.rotation);
            Destroy(effect, 3f);
        }
        
        // Vibración si está disponible (puedes expandir esto)
        #if UNITY_ANDROID || UNITY_IOS
        if (PlayerPrefs.GetInt("Vibration", 1) == 1)
        {
            Handheld.Vibrate();
        }
        #endif
    }
}

// ============================================
// MOVING OBSTACLE - Obstáculo que se mueve
// ============================================
public class MovingObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right;
    public float moveSpeed = 2f;
    public float moveRange = 5f;
    public bool oscillate = true;
    
    [Header("Movement Type")]
    public MovementType movementType = MovementType.Lateral;
    
    public enum MovementType
    {
        Lateral,        // Izquierda-derecha
        Vertical,       // Arriba-abajo
        Forward,        // Adelante-atrás
        Circular        // Movimiento circular
    }
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingToTarget = true;
    private float circularTime = 0f;
    
    void Start()
    {
        startPosition = transform.position;
        
        switch (movementType)
        {
            case MovementType.Lateral:
                moveDirection = transform.right;
                break;
                
            case MovementType.Vertical:
                moveDirection = Vector3.up;
                break;
                
            case MovementType.Forward:
                moveDirection = transform.forward;
                break;
        }
        
        targetPosition = startPosition + (moveDirection * moveRange);
    }
    
    void Update()
    {
        switch (movementType)
        {
            case MovementType.Circular:
                UpdateCircularMovement();
                break;
                
            default:
                UpdateLinearMovement();
                break;
        }
    }
    
    void UpdateLinearMovement()
    {
        if (oscillate)
        {
            // Movimiento de ida y vuelta
            Vector3 currentTarget = movingToTarget ? targetPosition : startPosition;
            transform.position = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, currentTarget) < 0.1f)
            {
                movingToTarget = !movingToTarget;
            }
        }
        else
        {
            // Movimiento continuo en una dirección
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }
    }
    
    void UpdateCircularMovement()
    {
        circularTime += moveSpeed * Time.deltaTime;
        
        Vector3 offset = new Vector3(
            Mathf.Cos(circularTime) * moveRange,
            0f,
            Mathf.Sin(circularTime) * moveRange
        );
        
        transform.position = startPosition + offset;
    }
}

// ============================================
// ROTATING OBSTACLE - Obstáculo que rota
// ============================================
public class RotatingObstacle : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 90f;
    public bool randomRotationDirection = true;
    
    [Header("Rotation Type")]
    public RotationType rotationType = RotationType.Continuous;
    public float oscillationAngle = 180f;
    
    public enum RotationType
    {
        Continuous,     // Rotación continua
        Oscillating     // Oscilación entre ángulos
    }
    
    private float currentAngle = 0f;
    private bool rotatingForward = true;
    private Quaternion startRotation;
    
    void Start()
    {
        startRotation = transform.rotation;
        
        if (randomRotationDirection)
        {
            rotationSpeed *= Random.value > 0.5f ? 1f : -1f;
        }
    }
    
    void Update()
    {
        switch (rotationType)
        {
            case RotationType.Continuous:
                UpdateContinuousRotation();
                break;
                
            case RotationType.Oscillating:
                UpdateOscillatingRotation();
                break;
        }
    }
    
    void UpdateContinuousRotation()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
    
    void UpdateOscillatingRotation()
    {
        float deltaAngle = rotationSpeed * Time.deltaTime;
        
        if (rotatingForward)
        {
            currentAngle += deltaAngle;
            if (currentAngle >= oscillationAngle)
            {
                currentAngle = oscillationAngle;
                rotatingForward = false;
            }
        }
        else
        {
            currentAngle -= deltaAngle;
            if (currentAngle <= -oscillationAngle)
            {
                currentAngle = -oscillationAngle;
                rotatingForward = true;
            }
        }
        
        transform.rotation = startRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }
}

// ============================================
// TEMPORAL OBSTACLE - Obstáculo temporal
// ============================================
public class TemporalObstacle : MonoBehaviour
{
    [Header("Temporal Settings")]
    public float visibleTime = 2f;
    public float hiddenTime = 1f;
    public bool startVisible = true;
    
    [Header("Transition")]
    public float transitionSpeed = 5f;
    public bool useScaling = true;
    public bool useFading = false;
    
    private bool isVisible;
    private float timer;
    private Vector3 originalScale;
    private Renderer[] renderers;
    private Collider obstacleCollider;
    
    void Start()
    {
        isVisible = startVisible;
        originalScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
        obstacleCollider = GetComponent<Collider>();
        
        timer = isVisible ? visibleTime : hiddenTime;
        
        if (!isVisible)
        {
            SetVisibility(false);
        }
    }
    
    void Update()
    {
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            ToggleVisibility();
            timer = isVisible ? visibleTime : hiddenTime;
        }
        
        UpdateTransition();
    }
    
    void ToggleVisibility()
    {
        isVisible = !isVisible;
        SetVisibility(isVisible);
    }
    
    void SetVisibility(bool visible)
    {
        if (obstacleCollider != null)
        {
            obstacleCollider.enabled = visible;
        }
        
        if (!useScaling && !useFading)
        {
            // Cambio instantáneo
            gameObject.SetActive(visible);
        }
    }
    
    void UpdateTransition()
    {
        if (useScaling)
        {
            Vector3 targetScale = isVisible ? originalScale : Vector3.zero;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, transitionSpeed * Time.deltaTime);
        }
        
        if (useFading && renderers.Length > 0)
        {
            float targetAlpha = isVisible ? 1f : 0f;
            
            foreach (Renderer rend in renderers)
            {
                if (rend.material.HasProperty("_Color"))
                {
                    Color color = rend.material.color;
                    color.a = Mathf.Lerp(color.a, targetAlpha, transitionSpeed * Time.deltaTime);
                    rend.material.color = color;
                }
            }
        }
    }
}