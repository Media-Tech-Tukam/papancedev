using UnityEngine;

public class ImprovedSplineFollower : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;
    public float lateralSpeed = 5f;
    public float maxLateralOffset = 2f;
    
    [Header("Smoothing")]
    public float positionSmoothing = 10f;
    public float rotationSmoothing = 8f;
    public float heightOffset = 0.2f;
    
    [Header("Auto Speed")]
    public bool autoIncreaseSpeed = true;
    public float speedIncrement = 0.1f;
    public float maxSpeed = 20f;
    
    [Header("Input")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    
    [Header("Touch/Mouse Controls")]
    public float touchSensitivity = 0.01f;
    public float mouseSensitivity = 0.005f;
    public bool enableTouchControls = true;
    public bool enableMouseDrag = true;
    
    [Header("Physics")]
    public bool useRigidbody = false; // Opción para usar o no Rigidbody
    
    private SplineMathGenerator splineGenerator;
    private Rigidbody rb;
    
    // Estado del seguimiento
    private float currentDistance = 0f;
    private float currentLateralOffset = 0f;
    private float lateralInput = 0f;
    
    // Variables para input táctil y mouse
    private Vector2? lastTouchPosition = null;
    private Vector2? initialMousePosition = null;
    private bool isMouseDragging = false;
    
    void Start()
    {
        Debug.Log("=== IMPROVED SPLINE FOLLOWER STARTING ===");
        
        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        if (splineGenerator == null)
        {
            Debug.LogError("ImprovedSplineFollower requires SplineMathGenerator in the scene!");
            return;
        }
        
        // Configurar Rigidbody solo si se va a usar
        if (useRigidbody)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            ConfigureRigidbody();
        }
        else
        {
            // Si no usamos Rigidbody, eliminarlo si existe
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                DestroyImmediate(rb);
                rb = null;
            }
        }
        
        // Configurar collider
        SetupCollider();
        
        // Encontrar posición inicial en el spline
        InitializePosition();
        
        Debug.Log($"Spline follower initialized. Using Rigidbody: {useRigidbody}");
        Debug.Log($"Touch controls: {enableTouchControls}, Mouse drag: {enableMouseDrag}");
    }
    
    void ConfigureRigidbody()
    {
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.linearDamping = 8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }
    
    void SetupCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.8f, 1.8f, 0.8f);
        }
        
        // Configurar tag
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }
    }
    
    void InitializePosition()
    {
        if (splineGenerator != null && splineGenerator.HasValidSpline())
        {
            // Empezar desde el inicio del spline
            currentDistance = 0f;
            currentLateralOffset = 0f;
            
            // Posicionar en el inicio del spline
            Vector3 startPosition = splineGenerator.GetSplinePosition(0f);
            transform.position = startPosition + Vector3.up * heightOffset;
            
            Vector3 startDirection = splineGenerator.GetSplineDirection(0f);
            if (startDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(startDirection);
            }
            
            Debug.Log($"Player initialized at spline position {startPosition}");
        }
    }
    
    void Update()
    {
        HandleInput();
        UpdateSpeed();
        FollowSpline();
    }
    
    void HandleInput()
    {
        lateralInput = 0f;
        
        // 1. PRIORIDAD: Input de teclado (PC/WebGL con teclado)
        if (Input.GetKey(leftKey))
            lateralInput = -1f;
        else if (Input.GetKey(rightKey))
            lateralInput = 1f;
        
        // 2. Si no hay input de teclado: Input System tradicional (gamepad, etc.)
        if (lateralInput == 0f)
            lateralInput = Input.GetAxis("Horizontal");
        
        // 3. Si no hay input anterior: Touch (móviles)
        if (lateralInput == 0f && enableTouchControls && Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        
        // 4. Si no hay input anterior: Mouse drag (WebGL desktop como alternativa)
        if (lateralInput == 0f && enableMouseDrag && Input.GetMouseButton(0))
        {
            HandleMouseInput();
        }
    }
    
    void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0); // Primer dedo
        
        switch (touch.phase)
        {
            case TouchPhase.Began:
                lastTouchPosition = touch.position;
                break;
                
            case TouchPhase.Moved:
                if (lastTouchPosition.HasValue)
                {
                    Vector2 deltaPosition = touch.position - lastTouchPosition.Value;
                    
                    // Convertir movimiento horizontal a input lateral
                    lateralInput = deltaPosition.x * touchSensitivity;
                    lateralInput = Mathf.Clamp(lateralInput, -1f, 1f);
                    
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
        else if (Input.GetMouseButton(0) && isMouseDragging && initialMousePosition.HasValue)
        {
            Vector2 deltaPosition = mousePosition - initialMousePosition.Value;
            
            // Convertir movimiento horizontal a input lateral
            lateralInput = deltaPosition.x * mouseSensitivity;
            lateralInput = Mathf.Clamp(lateralInput, -1f, 1f);
            
            // Opcional: Actualizar posición inicial para movimiento continuo
            // initialMousePosition = mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            initialMousePosition = null;
            isMouseDragging = false;
        }
    }
    
    void UpdateSpeed()
    {
        if (autoIncreaseSpeed && speed < maxSpeed)
        {
            speed += speedIncrement * Time.deltaTime;
            speed = Mathf.Clamp(speed, 0f, maxSpeed);
        }
    }
    
    void FollowSpline()
    {
        if (splineGenerator == null || !splineGenerator.HasValidSpline())
        {
            return;
        }
        
        // Avanzar a lo largo del spline
        currentDistance += speed * Time.deltaTime;
        
        // Aplicar movimiento lateral
        float lateralChange = lateralInput * lateralSpeed * Time.deltaTime;
        currentLateralOffset += lateralChange;
        currentLateralOffset = Mathf.Clamp(currentLateralOffset, -maxLateralOffset, maxLateralOffset);
        
        // Obtener posición y orientación del spline
        Vector3 splinePosition = splineGenerator.GetSplinePosition(currentDistance);
        Vector3 splineDirection = splineGenerator.GetSplineDirection(currentDistance);
        Vector3 splineRight = splineGenerator.GetSplineRight(currentDistance);
        
        // Calcular posición final con offset lateral
        Vector3 lateralOffset = splineRight * currentLateralOffset;
        Vector3 targetPosition = splinePosition + lateralOffset + (Vector3.up * heightOffset);
        
        // Aplicar movimiento suave
        ApplyMovement(targetPosition, splineDirection);
        
        // Notificar al generador para triggers
        splineGenerator.TriggerNextSegment(currentDistance);
    }
    
    void ApplyMovement(Vector3 targetPosition, Vector3 targetDirection)
    {
        if (useRigidbody && rb != null)
        {
            // Movimiento con Rigidbody (más físico)
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, positionSmoothing * Time.deltaTime);
            rb.MovePosition(smoothedPosition);
        }
        else
        {
            // Movimiento directo con Transform (más suave, menos físico)
            float smoothTime = Mathf.Clamp01(positionSmoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothTime);
        }
        
        // Rotación suave
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            float rotationTime = Mathf.Clamp01(rotationSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationTime);
        }
    }
    
    // Métodos públicos para control externo
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    public float GetSpeed()
    {
        return speed;
    }
    
    public void IncreaseSpeed(float increment)
    {
        speed += increment;
        speed = Mathf.Clamp(speed, 0f, maxSpeed);
    }
    
    public float GetCurrentDistance()
    {
        return currentDistance;
    }
    
    public float GetLateralOffset()
    {
        return currentLateralOffset;
    }
    
    public Vector3 GetSplinePosition()
    {
        if (splineGenerator != null)
            return splineGenerator.GetSplinePosition(currentDistance);
        return Vector3.zero;
    }
    
    public Vector3 GetSplineDirection()
    {
        if (splineGenerator != null)
            return splineGenerator.GetSplineDirection(currentDistance);
        return Vector3.forward;
    }
    
    public bool IsOnValidSpline()
    {
        return splineGenerator != null && splineGenerator.HasValidSpline();
    }
    
    public void RepositionOnSpline(float distance = -1f)
    {
        if (distance >= 0f)
        {
            currentDistance = distance;
        }
        else
        {
            currentDistance = 0f;
        }
        
        currentLateralOffset = 0f;
        
        if (IsOnValidSpline())
        {
            Vector3 newPosition = splineGenerator.GetSplinePosition(currentDistance);
            transform.position = newPosition + Vector3.up * heightOffset;
        }
    }
    
    // Métodos públicos para configurar controles táctiles
    public void SetTouchSensitivity(float sensitivity)
    {
        touchSensitivity = sensitivity;
    }
    
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    public void EnableTouchControls(bool enable)
    {
        enableTouchControls = enable;
    }
    
    public void EnableMouseDrag(bool enable)
    {
        enableMouseDrag = enable;
    }
    
    void OnDrawGizmosSelected()
    {
        if (splineGenerator == null || !splineGenerator.HasValidSpline()) return;
        
        // Mostrar posición actual en el spline
        Gizmos.color = Color.green;
        Vector3 splinePos = splineGenerator.GetSplinePosition(currentDistance);
        Gizmos.DrawSphere(splinePos, 0.3f);
        
        // Mostrar dirección
        Gizmos.color = Color.blue;
        Vector3 direction = splineGenerator.GetSplineDirection(currentDistance);
        Gizmos.DrawRay(splinePos, direction * 2f);
        
        // Mostrar offset lateral
        if (currentLateralOffset != 0f)
        {
            Gizmos.color = Color.red;
            Vector3 right = splineGenerator.GetSplineRight(currentDistance);
            Vector3 offsetPos = splinePos + (right * currentLateralOffset);
            Gizmos.DrawSphere(offsetPos, 0.2f);
            Gizmos.DrawLine(splinePos, offsetPos);
        }
        
        // Mostrar límites laterales
        Gizmos.color = Color.yellow;
        Vector3 rightLimit = splinePos + (splineGenerator.GetSplineRight(currentDistance) * maxLateralOffset);
        Vector3 leftLimit = splinePos + (splineGenerator.GetSplineRight(currentDistance) * -maxLateralOffset);
        Gizmos.DrawWireSphere(rightLimit, 0.1f);
        Gizmos.DrawWireSphere(leftLimit, 0.1f);
        
        // Mostrar distancia actual
        Gizmos.color = Color.white;
        Vector3 textPos = transform.position + Vector3.up * 2f;
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(textPos, $"Distance: {currentDistance:F1}\nLateral: {currentLateralOffset:F2}");
        #endif
    }
}