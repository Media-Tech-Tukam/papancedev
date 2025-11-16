using UnityEngine;

public class ChivaController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 15f; // 54 km/h como en el GDD
    [SerializeField] private float maxSpeed = 25f;
    [SerializeField] private float lateralSpeed = 8f;
    [SerializeField] private float maxLateralOffset = 3.5f; // Ancho de carretera
    
    [Header("Braking System")]
    [SerializeField] private KeyCode brakeKey = KeyCode.Space;
    [SerializeField] private float brakeForce = 0.7f; // Multiplicador de velocidad al frenar
    [SerializeField] private float minSpeedWhileBraking = 2f;
    [SerializeField] private float brakeRelease = 3f; // Velocidad de soltar freno
    
    [Header("Smoothing & Response")]
    [SerializeField] private float positionSmoothing = 12f;
    [SerializeField] private float rotationSmoothing = 8f;
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private float inputResponseTime = 0.1f; // Base, se modificará con borrachera
    
    [Header("Input Controls")]
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private bool enableTouchControls = true;
    [SerializeField] private bool enableMouseDrag = true;
    [SerializeField] private float touchSensitivity = 0.015f;
    [SerializeField] private float mouseSensitivity = 0.008f;
    
    [Header("Passenger Pickup")]
    [SerializeField] private float pickupSpeedThreshold = 8f; // Velocidad máxima para recoger
    [SerializeField] private float pickupRadius = 2f; // Radio de recogida
    [SerializeField] private LayerMask passengerLayer = 1; // Layer de pasajeros
    
    [Header("Physics")]
    [SerializeField] private bool useRigidbody = false;
    [SerializeField] private float physicsSmoothing = 5f;
    
    [Header("Drunkenness Effects - PREPARACIÓN")]
    [SerializeField] private bool enableDrunkennessEffects = false;
    [SerializeField] private float maxInputDelay = 0.3f; // Máximo delay por borrachera
    [SerializeField] private float maxDriftIntensity = 2f; // Máximo drift lateral
    [SerializeField] private float drunkSpeedVariation = 0.3f; // Variación de velocidad
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color debugColor = Color.cyan;
    
    // Referencias
    private ChivaSplineGenerator splineGenerator;
    private Rigidbody rb;
    
    // Estado del movimiento
    private float currentDistance = 0f;
    private float currentLateralOffset = 0f;
    private float currentSpeed = 0f;
    private bool isBraking = false;
    
    // Input handling
    private float rawLateralInput = 0f;
    private float smoothedLateralInput = 0f;
    private float inputSmoothVelocity = 0f;
    
    // Touch/Mouse input
    private Vector2? lastTouchPosition = null;
    private Vector2? initialMousePosition = null;
    private bool isMouseDragging = false;
    
    // Drunkenness state (preparación para futuro sistema)
    private float currentDrunkenness = 0f; // 0-100
    private float inputDelay = 0f;
    private float driftNoise = 0f;
    private float speedVariation = 1f;
    
    // Eventos
    public System.Action<float> OnSpeedChanged; // Nueva velocidad
    public System.Action<float> OnProgressChanged; // 0-1 progreso
    public System.Action<float> OnLateralOffsetChanged; // Offset lateral
    public System.Action<bool> OnBrakingStateChanged; // Estado del freno
    
    // Getters públicos
    public float CurrentSpeed => currentSpeed;
    public float CurrentDistance => currentDistance;
    public float RouteProgress => splineGenerator?.GetRouteProgress(currentDistance) ?? 0f;
    public bool IsBraking => isBraking;
    public bool CanPickupPassengers => currentSpeed <= pickupSpeedThreshold;
    public float LateralOffset => currentLateralOffset;
    
    void Start()
    {
        Debug.Log("=== CHIVA CONTROLLER STARTING ===");
        
        InitializeReferences();
        SetupPhysics();
        InitializePosition();
        
        currentSpeed = baseSpeed;
        
        Debug.Log($"Chiva initialized - Base speed: {baseSpeed}, Max speed: {maxSpeed}");
        Debug.Log($"Controls: {leftKey}/{rightKey} + {brakeKey} (brake)");
    }
    
    void InitializeReferences()
    {
        splineGenerator = FindObjectOfType<ChivaSplineGenerator>();
        if (splineGenerator == null)
        {
            Debug.LogError("ChivaController requires ChivaSplineGenerator in scene!");
            return;
        }
        
        if (useRigidbody)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        }
    }
    
    void SetupPhysics()
    {
        if (useRigidbody && rb != null)
        {
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.linearDamping = physicsSmoothing;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        // Asegurar collider para pickup detection
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(1.5f, 2f, 3f); // Tamaño de chiva
            boxCol.isTrigger = true; // Para detectar pasajeros
        }
        
        // Tag de jugador
        gameObject.tag = "Player";
    }
    
    void InitializePosition()
    {
        if (splineGenerator != null && splineGenerator.HasValidSpline())
        {
            currentDistance = 0f;
            currentLateralOffset = 0f;
            
            Vector3 startPos = splineGenerator.GetSplinePosition(0f);
            transform.position = startPos + Vector3.up * heightOffset;
            
            Vector3 startDir = splineGenerator.GetSplineDirection(0f);
            if (startDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(startDir);
            }
        }
    }
    
    void Update()
    {
        if (!splineGenerator?.HasValidSpline() ?? true) return;
        
        HandleInput();
        UpdateMovement();
        UpdateDrunkennessEffects();
        CheckPassengerPickup();
        NotifyEvents();
    }
    
    void HandleInput()
    {
        // Reset input
        rawLateralInput = 0f;
        
        // 1. PRIORIDAD: Input de teclado
        if (Input.GetKey(leftKey))
            rawLateralInput = -1f;
        else if (Input.GetKey(rightKey))
            rawLateralInput = 1f;
        
        // 2. Input System (gamepad)
        if (rawLateralInput == 0f)
            rawLateralInput = Input.GetAxis("Horizontal");
        
        // 3. Touch controls
        if (rawLateralInput == 0f && enableTouchControls && Input.touchCount > 0)
            HandleTouchInput();
        
        // 4. Mouse drag
        if (rawLateralInput == 0f && enableMouseDrag && Input.GetMouseButton(0))
            HandleMouseInput();
        
        // Aplicar suavizado al input lateral con delay por borrachera
        float targetResponseTime = inputResponseTime + inputDelay;
        smoothedLateralInput = Mathf.SmoothDamp(smoothedLateralInput, rawLateralInput, 
            ref inputSmoothVelocity, targetResponseTime);
        
        // Detectar frenado
        bool previousBraking = isBraking;
        isBraking = Input.GetKey(brakeKey);
        
        if (previousBraking != isBraking)
        {
            OnBrakingStateChanged?.Invoke(isBraking);
        }
    }
    
    void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);
        
        switch (touch.phase)
        {
            case TouchPhase.Began:
                lastTouchPosition = touch.position;
                break;
                
            case TouchPhase.Moved:
                if (lastTouchPosition.HasValue)
                {
                    Vector2 deltaPosition = touch.position - lastTouchPosition.Value;
                    rawLateralInput = deltaPosition.x * touchSensitivity;
                    rawLateralInput = Mathf.Clamp(rawLateralInput, -1f, 1f);
                    lastTouchPosition = touch.position;
                }
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                lastTouchPosition = null;
                break;
        }
    }
    
    void HandleMouseInput()
    {
        Vector2 mousePosition = Input.mousePosition;
        
        if (Input.GetMouseButtonDown(0))
        {
            initialMousePosition = mousePosition;
            isMouseDragging = true;
        }
        else if (isMouseDragging && initialMousePosition.HasValue)
        {
            Vector2 deltaPosition = mousePosition - initialMousePosition.Value;
            rawLateralInput = deltaPosition.x * mouseSensitivity;
            rawLateralInput = Mathf.Clamp(rawLateralInput, -1f, 1f);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            initialMousePosition = null;
            isMouseDragging = false;
        }
    }
    
    void UpdateMovement()
    {
        // Calcular velocidad con efectos de borrachera y frenado
        float targetSpeed = CalculateTargetSpeed();
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 
            (isBraking ? brakeRelease * 2f : brakeRelease) * Time.deltaTime);
        
        // Avanzar por el spline
        currentDistance += currentSpeed * Time.deltaTime;
        
        // Aplicar movimiento lateral con drift por borrachera
        float lateralMovement = (smoothedLateralInput + driftNoise) * lateralSpeed * Time.deltaTime;
        currentLateralOffset += lateralMovement;
        currentLateralOffset = Mathf.Clamp(currentLateralOffset, -maxLateralOffset, maxLateralOffset);
        
        // Obtener posición objetivo
        Vector3 splinePosition = splineGenerator.GetSplinePosition(currentDistance);
        Vector3 splineDirection = splineGenerator.GetSplineDirection(currentDistance);
        Vector3 splineRight = splineGenerator.GetSplineRight(currentDistance);
        
        // Calcular posición final
        Vector3 lateralOffsetVector = splineRight * currentLateralOffset;
        Vector3 targetPosition = splinePosition + lateralOffsetVector + Vector3.up * heightOffset;
        
        // Aplicar movimiento
        ApplyMovement(targetPosition, splineDirection);
    }
    
    float CalculateTargetSpeed()
    {
        float speed = baseSpeed * speedVariation; // Variación por borrachera
        
        if (isBraking)
        {
            speed *= brakeForce;
            speed = Mathf.Max(speed, minSpeedWhileBraking);
        }
        
        return Mathf.Clamp(speed, 0f, maxSpeed);
    }
    
    void ApplyMovement(Vector3 targetPosition, Vector3 targetDirection)
    {
        if (useRigidbody && rb != null)
        {
            // Movimiento con física
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, 
                positionSmoothing * Time.deltaTime);
            rb.MovePosition(smoothedPosition);
        }
        else
        {
            // Movimiento directo
            transform.position = Vector3.Lerp(transform.position, targetPosition, 
                positionSmoothing * Time.deltaTime);
        }
        
        // Rotación suave
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                rotationSmoothing * Time.deltaTime);
        }
    }
    
    void UpdateDrunkennessEffects()
    {
        if (!enableDrunkennessEffects) return;
        
        // Aplicar efectos de borrachera (se completará con DrunkennessSystem)
        float drunknessFactor = currentDrunkenness / 100f;
        
        // Input delay
        inputDelay = drunknessFactor * maxInputDelay;
        
        // Drift lateral aleatorio
        float driftFrequency = 0.5f + drunknessFactor * 2f;
        driftNoise = Mathf.PerlinNoise(Time.time * driftFrequency, 0f) * 2f - 1f;
        driftNoise *= drunknessFactor * maxDriftIntensity;
        
        // Variación de velocidad
        speedVariation = 1f + Mathf.Sin(Time.time * 0.3f) * drunknessFactor * drunkSpeedVariation;
    }
    
    void CheckPassengerPickup()
    {
        if (!CanPickupPassengers) return;
        
        // Buscar pasajeros en rango (se completará con PassengerManager)
        Collider[] passengers = Physics.OverlapSphere(transform.position, pickupRadius, passengerLayer);
        
        foreach (Collider passenger in passengers)
        {
            // TODO: Implementar pickup logic cuando tengamos PassengerManager
            Debug.Log($"Passenger detected for pickup: {passenger.name}");
        }
    }
    
    void NotifyEvents()
    {
        OnSpeedChanged?.Invoke(currentSpeed);
        OnProgressChanged?.Invoke(RouteProgress);
        OnLateralOffsetChanged?.Invoke(currentLateralOffset);
    }
    
    // ========== MÉTODOS PÚBLICOS DE CONTROL ==========
    
    public void SetDrunkenness(float drunkenness)
    {
        currentDrunkenness = Mathf.Clamp(drunkenness, 0f, 100f);
        Debug.Log($"Chiva drunkenness set to: {currentDrunkenness:F1}%");
    }
    
    public void SetSpeed(float newSpeed)
    {
        baseSpeed = Mathf.Clamp(newSpeed, 1f, maxSpeed);
        Debug.Log($"Chiva base speed set to: {baseSpeed:F1}");
    }
    
    public void ResetPosition()
    {
        currentDistance = 0f;
        currentLateralOffset = 0f;
        currentSpeed = baseSpeed;
        InitializePosition();
        Debug.Log("Chiva position reset to start");
    }
    
    public bool IsNearRouteEnd(float threshold = 1000f)
    {
        if (splineGenerator == null) return false;
        return (splineGenerator.GetTotalDistance() - currentDistance) <= threshold;
    }
    
    public void ForceBrake(bool enable)
    {
        isBraking = enable;
        OnBrakingStateChanged?.Invoke(isBraking);
    }
    
    // ========== MÉTODOS DE INFORMACIÓN ==========
    
    public float GetSpeedKmh()
    {
        return currentSpeed * 3.6f; // Conversión m/s a km/h
    }
    
    public float GetDistanceKm()
    {
        return currentDistance / 1000f;
    }
    
    public Vector3 GetCurrentSplinePosition()
    {
        return splineGenerator?.GetSplinePosition(currentDistance) ?? Vector3.zero;
    }
    
    public float GetDifficultyAtCurrentPosition()
    {
        return splineGenerator?.GetDifficultyAtDistance(currentDistance) ?? 0f;
    }
    
    // ========== DEBUG Y GIZMOS ==========
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Posición actual
        Gizmos.color = debugColor;
        Gizmos.DrawSphere(transform.position, 0.5f);
        
        // Radio de pickup
        Gizmos.color = CanPickupPassengers ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        
        // Dirección de movimiento
        if (splineGenerator?.HasValidSpline() ?? false)
        {
            Vector3 direction = splineGenerator.GetSplineDirection(currentDistance);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, direction * 3f);
            
            // Offset lateral
            Vector3 splinePos = splineGenerator.GetSplinePosition(currentDistance);
            Vector3 right = splineGenerator.GetSplineRight(currentDistance);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(splinePos, splinePos + right * currentLateralOffset);
            
            // Límites laterales
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(splinePos + right * maxLateralOffset, 0.3f);
            Gizmos.DrawWireSphere(splinePos + right * -maxLateralOffset, 0.3f);
        }
        
        #if UNITY_EDITOR
        // Información de debug
        Vector3 infoPos = transform.position + Vector3.up * 3f;
        string info = $"CHIVA STATUS\n";
        info += $"Speed: {GetSpeedKmh():F1} km/h\n";
        info += $"Distance: {GetDistanceKm():F1} km\n";
        info += $"Progress: {RouteProgress*100f:F1}%\n";
        info += $"Braking: {(isBraking ? "YES" : "NO")}\n";
        info += $"Can Pickup: {(CanPickupPassengers ? "YES" : "NO")}\n";
        info += $"Drunkenness: {currentDrunkenness:F1}%";
        
        UnityEditor.Handles.Label(infoPos, info);
        #endif
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Para detectar pasajeros y otros elementos
        if (other.CompareTag("Passenger"))
        {
            Debug.Log($"Chiva entered passenger trigger: {other.name}");
        }
    }
}
