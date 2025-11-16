using UnityEngine;

// ============================================
// FLOATING COLLECTIBLE - Movimiento vertical
// ============================================
public class FloatingCollectible : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatHeight = 1f;
    public float floatSpeed = 2f;
    public bool randomStartOffset = true;
    public AnimationCurve floatCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    private Vector3 startPosition;
    private float timeOffset;
    
    void Start()
    {
        startPosition = transform.position;
        
        if (randomStartOffset)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }
    
    void Update()
    {
        UpdateFloatingMovement();
    }
    
    void UpdateFloatingMovement()
    {
        float time = Time.time * floatSpeed + timeOffset;
        float curveValue = floatCurve.Evaluate((Mathf.Sin(time) + 1f) * 0.5f);
        
        Vector3 newPosition = startPosition;
        newPosition.y += curveValue * floatHeight;
        
        transform.position = newPosition;
    }
}

// ============================================
// ROTATING COLLECTIBLE - Rotación continua
// ============================================
public class RotatingCollectible : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 90f;
    public bool randomDirection = true;
    public bool wobbleRotation = false;
    
    [Header("Wobble Settings")]
    public float wobbleAmount = 15f;
    public float wobbleSpeed = 3f;
    
    private float direction = 1f;
    private Vector3 baseRotationAxis;
    
    void Start()
    {
        baseRotationAxis = rotationAxis.normalized;
        
        if (randomDirection)
        {
            direction = Random.value > 0.5f ? 1f : -1f;
        }
        
        // Velocidad ligeramente aleatoria para variedad
        rotationSpeed += Random.Range(-10f, 10f);
    }
    
    void Update()
    {
        UpdateRotation();
    }
    
    void UpdateRotation()
    {
        Vector3 currentAxis = baseRotationAxis;
        
        if (wobbleRotation)
        {
            // Agregar wobble al eje de rotación
            float wobbleX = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;
            float wobbleZ = Mathf.Cos(Time.time * wobbleSpeed * 0.7f) * wobbleAmount;
            
            currentAxis += new Vector3(wobbleX, 0f, wobbleZ) * 0.01f;
            currentAxis = currentAxis.normalized;
        }
        
        transform.Rotate(currentAxis, rotationSpeed * direction * Time.deltaTime, Space.World);
    }
}

// ============================================
// PULSING COLLECTIBLE - Escala pulsante
// ============================================
public class PulsingCollectible : MonoBehaviour
{
    [Header("Pulsing Settings")]
    public float pulseScale = 0.3f;
    public float pulseSpeed = 2f;
    public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool randomPhase = true;
    
    [Header("Color Pulsing")]
    public bool pulseColor = false;
    public Color baseColor = Color.white;
    public Color pulseColorTarget = Color.yellow;
    
    private Vector3 baseScale;
    private float timeOffset;
    private Renderer objectRenderer;
    private Material originalMaterial;
    
    void Start()
    {
        baseScale = transform.localScale;
        
        if (randomPhase)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
        
        if (pulseColor)
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                originalMaterial = objectRenderer.material;
            }
        }
    }
    
    void Update()
    {
        UpdatePulsing();
    }
    
    void UpdatePulsing()
    {
        float time = Time.time * pulseSpeed + timeOffset;
        float curveValue = pulseCurve.Evaluate((Mathf.Sin(time) + 1f) * 0.5f);
        
        // Escala pulsante
        Vector3 newScale = baseScale + (baseScale * pulseScale * curveValue);
        transform.localScale = newScale;
        
        // Color pulsante (opcional)
        if (pulseColor && objectRenderer != null)
        {
            Color currentColor = Color.Lerp(baseColor, pulseColorTarget, curveValue);
            objectRenderer.material.color = currentColor;
        }
    }
    
    void OnDestroy()
    {
        // Limpiar material si se creó una copia
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }
}

// ============================================
// ORBITING COLLECTIBLE - Orbita alrededor de un punto
// ============================================
public class OrbitingCollectible : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float orbitRadius = 2f;
    public float orbitSpeed = 1f;
    public Vector3 orbitAxis = Vector3.up;
    public bool clockwise = true;
    
    [Header("Orbit Variation")]
    public bool ellipticalOrbit = false;
    public float ellipseRatio = 0.6f; // Ratio entre ejes mayor y menor
    public bool varyingSpeed = false;
    public float speedVariation = 0.5f;
    
    private Vector3 centerPoint;
    private float currentAngle;
    private float baseSpeed;
    
    void Start()
    {
        centerPoint = transform.position;
        baseSpeed = orbitSpeed;
        
        // Posición inicial aleatoria en la órbita
        currentAngle = Random.Range(0f, Mathf.PI * 2f);
        
        // Velocidad ligeramente aleatoria
        orbitSpeed += Random.Range(-speedVariation, speedVariation);
        
        UpdateOrbitPosition();
    }
    
    void Update()
    {
        UpdateOrbitMovement();
    }
    
    void UpdateOrbitMovement()
    {
        float deltaAngle = orbitSpeed * Time.deltaTime;
        if (!clockwise) deltaAngle = -deltaAngle;
        
        // Velocidad variable opcional
        if (varyingSpeed)
        {
            float speedMod = 1f + Mathf.Sin(Time.time * 2f) * 0.3f;
            deltaAngle *= speedMod;
        }
        
        currentAngle += deltaAngle;
        UpdateOrbitPosition();
    }
    
    void UpdateOrbitPosition()
    {
        Vector3 offset;
        
        if (ellipticalOrbit)
        {
            // Órbita elíptica
            float x = Mathf.Cos(currentAngle) * orbitRadius;
            float z = Mathf.Sin(currentAngle) * orbitRadius * ellipseRatio;
            offset = new Vector3(x, 0f, z);
        }
        else
        {
            // Órbita circular
            float x = Mathf.Cos(currentAngle) * orbitRadius;
            float z = Mathf.Sin(currentAngle) * orbitRadius;
            offset = new Vector3(x, 0f, z);
        }
        
        // Aplicar eje de órbita
        if (orbitAxis != Vector3.up)
        {
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, orbitAxis);
            offset = rotation * offset;
        }
        
        transform.position = centerPoint + offset;
    }
    
    // Método para configurar la órbita desde el generator
    public void SetOrbitCenter(Vector3 center)
    {
        centerPoint = center;
        UpdateOrbitPosition();
    }
}

// ============================================
// TRAIL COLLECTIBLE - Deja rastro visual
// ============================================
public class TrailCollectible : MonoBehaviour
{
    [Header("Trail Settings")]
    public bool enableTrail = true;
    public float trailTime = 1f;
    public float trailWidth = 0.2f;
    public Color trailColor = Color.yellow;
    public Material trailMaterial;
    
    [Header("Particle Trail")]
    public bool enableParticles = false;
    public GameObject particlePrefab;
    public float particleSpawnRate = 10f;
    
    private TrailRenderer trailRenderer;
    private float lastParticleTime;
    
    void Start()
    {
        SetupTrail();
    }
    
    void Update()
    {
        if (enableParticles)
        {
            UpdateParticleTrail();
        }
    }
    
    void SetupTrail()
    {
        if (enableTrail)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
            
            trailRenderer.time = trailTime;
            trailRenderer.startWidth = trailWidth;
            trailRenderer.endWidth = 0f;
            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            
            if (trailMaterial != null)
            {
                trailRenderer.material = trailMaterial;
            }
            
            trailRenderer.autodestruct = false;
        }
    }
    
    void UpdateParticleTrail()
    {
        if (particlePrefab != null && Time.time - lastParticleTime >= 1f / particleSpawnRate)
        {
            GameObject particle = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            Destroy(particle, 2f); // Limpiar partículas después de 2 segundos
            lastParticleTime = Time.time;
        }
    }
}

// ============================================
// MAGNETIC COLLECTIBLE - Atrae otros coleccionables
// ============================================
public class MagneticCollectible : MonoBehaviour
{
    [Header("Magnetic Settings")]
    public float magneticRange = 5f;
    public float magneticStrength = 3f;
    public LayerMask collectibleLayer = -1;
    public bool affectOtherMagnetics = false;
    
    [Header("Visual Effects")]
    public bool showMagneticField = false;
    public Color fieldColor = Color.cyan;
    
    void Update()
    {
        ApplyMagneticForce();
    }
    
    void ApplyMagneticForce()
    {
        Collider[] nearbyCollectibles = Physics.OverlapSphere(transform.position, magneticRange, collectibleLayer);
        
        foreach (Collider col in nearbyCollectibles)
        {
            if (col.gameObject == gameObject) continue;
            
            CollectibleCollision collectible = col.GetComponent<CollectibleCollision>();
            if (collectible == null) continue;
            
            // No afectar otros magnéticos si está deshabilitado
            if (!affectOtherMagnetics && col.GetComponent<MagneticCollectible>() != null) continue;
            
            // Calcular fuerza magnética
            Vector3 direction = (transform.position - col.transform.position);
            float distance = direction.magnitude;
            
            if (distance > 0.1f && distance <= magneticRange)
            {
                direction.Normalize();
                float force = magneticStrength * (1f - distance / magneticRange);
                
                col.transform.position += direction * force * Time.deltaTime;
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (showMagneticField)
        {
            Gizmos.color = fieldColor;
            Gizmos.DrawWireSphere(transform.position, magneticRange);
        }
    }
}

// ============================================
// COMBO COLLECTIBLE - Múltiples comportamientos
// ============================================
public class ComboCollectible : MonoBehaviour
{
    [Header("Combo Behaviors")]
    public bool enableFloating = true;
    public bool enableRotation = true;
    public bool enablePulsing = false;
    public bool enableTrail = false;
    
    private FloatingCollectible floatingComponent;
    private RotatingCollectible rotatingComponent;
    private PulsingCollectible pulsingComponent;
    private TrailCollectible trailComponent;
    
    void Start()
    {
        SetupComboComponents();
    }
    
    void SetupComboComponents()
    {
        if (enableFloating)
        {
            floatingComponent = gameObject.AddComponent<FloatingCollectible>();
            floatingComponent.floatHeight = 0.5f;
            floatingComponent.floatSpeed = 1.5f;
        }
        
        if (enableRotation)
        {
            rotatingComponent = gameObject.AddComponent<RotatingCollectible>();
            rotatingComponent.rotationSpeed = 60f;
            rotatingComponent.wobbleRotation = true;
        }
        
        if (enablePulsing)
        {
            pulsingComponent = gameObject.AddComponent<PulsingCollectible>();
            pulsingComponent.pulseScale = 0.2f;
            pulsingComponent.pulseSpeed = 3f;
        }
        
        if (enableTrail)
        {
            trailComponent = gameObject.AddComponent<TrailCollectible>();
            trailComponent.trailTime = 0.5f;
            trailComponent.trailWidth = 0.1f;
        }
    }
    
    // Método para configurar qué comportamientos usar
    public void ConfigureCombo(bool floating, bool rotation, bool pulsing, bool trail)
    {
        enableFloating = floating;
        enableRotation = rotation;
        enablePulsing = pulsing;
        enableTrail = trail;
        
        SetupComboComponents();
    }
}