using UnityEngine;

public class ImprovedSplineFollower : MonoBehaviour
{
    [Header("Movement")]
    public float velocidadBase = 15f;        // Velocidad constante
    public float velocidadActual = 15f;      // Velocidad usada realmente
    public float brakeStrength = 30f;        // Intensidad de frenado

    public float lateralSpeed = 5f;
    public float maxLateralOffset = 2f;

    [Header("Smoothing")]
    public float positionSmoothing = 10f;
    public float rotationSmoothing = 8f;
    public float heightOffset = 0.2f;

    [Header("Input")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("Touch/Mouse Controls")]
    public float touchSensitivity = 0.01f;
    public float mouseSensitivity = 0.005f;
    public bool enableTouchControls = true;
    public bool enableMouseDrag = true;

    [Header("Physics")]
    public bool useRigidbody = false;

    private SplineMathGenerator splineGenerator;
    private Rigidbody rb;

    private float currentDistance = 0f;
    private float currentLateralOffset = 0f;
    private float lateralInput = 0f;

    private Vector2? lastTouchPosition = null;
    private Vector2? initialMousePosition = null;
    private bool isMouseDragging = false;

    public float GetLateralInput()
    {
        return lateralInput;
    }

    void Start()
    {
        Debug.Log("=== IMPROVED SPLINE FOLLOWER STARTING ===");

        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        if (splineGenerator == null)
        {
            Debug.LogError("ImprovedSplineFollower requires SplineMathGenerator in the scene!");
            return;
        }

        if (useRigidbody)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            ConfigureRigidbody();
        }
        else
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null) DestroyImmediate(rb);
        }

        SetupCollider();
        InitializePosition();
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
        if (GetComponent<Collider>() == null)
        {
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.8f, 1.8f, 0.8f);
        }

        if (!gameObject.CompareTag("Player"))
            gameObject.tag = "Player";
    }

    void InitializePosition()
    {
        if (splineGenerator != null && splineGenerator.HasValidSpline())
        {
            currentDistance = 0f;
            currentLateralOffset = 0f;
            velocidadActual = velocidadBase;

            Vector3 startPosition = splineGenerator.GetSplinePosition(0f);
            transform.position = startPosition + Vector3.up * heightOffset;

            Vector3 startDirection = splineGenerator.GetSplineDirection(0f);
            transform.rotation = Quaternion.LookRotation(startDirection);

            Debug.Log($"Player initialized at spline position {startPosition}");
        }
    }

    void Update()
    {
        HandleInput();
        FollowSpline();
    }

    // -----------------------------------------------------
    //               INPUT MANEJO A/D + MOUSE + TOUCH
    // -----------------------------------------------------
    void HandleInput()
    {
        lateralInput = 0f;

        if (Input.GetKey(leftKey)) lateralInput = -1f;
        else if (Input.GetKey(rightKey)) lateralInput = 1f;

        if (lateralInput == 0f)
            lateralInput = Input.GetAxis("Horizontal");

        if (lateralInput == 0f && enableTouchControls && Input.touchCount > 0)
            HandleTouchInput();

        if (lateralInput == 0f && enableMouseDrag && Input.GetMouseButton(0))
            HandleMouseInput();
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
                    Vector2 delta = touch.position - lastTouchPosition.Value;
                    lateralInput = Mathf.Clamp(delta.x * touchSensitivity, -1f, 1f);
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
            Vector2 delta = mousePosition - initialMousePosition.Value;
            lateralInput = Mathf.Clamp(delta.x * mouseSensitivity, -1f, 1f);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseDragging = false;
            initialMousePosition = null;
        }
    }

    // -----------------------------------------------------
    //                    MOVIMIENTO PRINCIPAL
    // -----------------------------------------------------
    void FollowSpline()
    {
        if (splineGenerator == null || !splineGenerator.HasValidSpline())
            return;

        // -------- VELOCIDAD CONSTANTE + FRENO --------
        bool isBraking = Input.GetKey(KeyCode.Space);

        if (isBraking)
        {
            velocidadActual -= brakeStrength * Time.deltaTime;
        }
        else
        {
            velocidadActual = Mathf.MoveTowards(
                velocidadActual,
                velocidadBase,
                brakeStrength * Time.deltaTime
            );
        }

        velocidadActual = Mathf.Clamp(velocidadActual, 0f, velocidadBase);

        currentDistance += velocidadActual * Time.deltaTime;

        // ------------ MOVIMIENTO LATERAL ------------
        float lateralChange = lateralInput * lateralSpeed * Time.deltaTime;
        currentLateralOffset += lateralChange;
        currentLateralOffset = Mathf.Clamp(currentLateralOffset, -maxLateralOffset, maxLateralOffset);

        // ------------ POSICIÓN SOBRE EL SPLINE ------------
        Vector3 splinePosition = splineGenerator.GetSplinePosition(currentDistance);
        Vector3 splineDirection = splineGenerator.GetSplineDirection(currentDistance);
        Vector3 splineRight = splineGenerator.GetSplineRight(currentDistance);

        Vector3 lateralOffset = splineRight * currentLateralOffset;

        Vector3 targetPosition =
            splinePosition +
            lateralOffset +
            (Vector3.up * heightOffset);

        // ------------ APLICAR MOVIMIENTO SUAVE ------------
        ApplyMovement(targetPosition, splineDirection);

        // Trigger para generar más spline
        splineGenerator.TriggerNextSegment(currentDistance);
    }

    void ApplyMovement(Vector3 targetPosition, Vector3 targetDirection)
    {
        if (useRigidbody && rb != null)
        {
            Vector3 smoothPos = Vector3.Lerp(transform.position, targetPosition, positionSmoothing * Time.deltaTime);
            rb.MovePosition(smoothPos);
        }
        else
        {
            float smooth = Mathf.Clamp01(positionSmoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, smooth);
        }

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDirection, Vector3.up);
            float rotSmooth = Mathf.Clamp01(rotationSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSmooth);
        }
    }
    

    // -----------------------------------------------------
    //     MÉTODOS PÚBLICOS (por si necesitas)
    // -----------------------------------------------------
    public float GetCurrentDistance() => currentDistance;
    public float GetSpeed() => velocidadActual;
}
