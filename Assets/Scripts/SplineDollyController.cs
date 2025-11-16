using UnityEngine;

// ============================================
// SIMPLE DOLLY CART - GameObject que sigue al jugador por el spline
// ============================================
public class SimpleDollyCart : MonoBehaviour
{
    [Header("Player Reference")]
    public ImprovedSplineFollower playerFollower;
    public bool autoFindPlayer = true;
    
    [Header("Sync Settings")]
    public bool copyPlayerSmoothing = true; // Usar el mismo smoothing que el player
    public float customSmoothing = 10f; // Smoothing manual si no se copia del player
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public Color dollyColor = Color.cyan;
    public float debugSphereSize = 0.5f;
    
    private SplineMathGenerator splineGenerator;
    private float currentDollyDistance = 0f;
    
    // Variables para movimiento suave (igual que ImprovedSplineFollower)
    private Vector3 currentDollyPosition;
    private Vector3 targetDollyPosition;
    
    void Start()
    {
        Debug.Log("=== SIMPLE DOLLY CART STARTING ===");
        
        // Encontrar referencias automáticamente
        if (autoFindPlayer && playerFollower == null)
        {
            playerFollower = FindObjectOfType<ImprovedSplineFollower>();
        }
        
        splineGenerator = FindObjectOfType<SplineMathGenerator>();
        
        if (!ValidateComponents())
        {
            return;
        }
        
        InitializeDollyCart();
        
        Debug.Log($"Dolly cart initialized. Copy player smoothing: {copyPlayerSmoothing}, Custom smoothing: {customSmoothing}");
    }
    
    bool ValidateComponents()
    {
        if (playerFollower == null)
        {
            Debug.LogError("SimpleDollyCart: No ImprovedSplineFollower found! Assign one or enable autoFindPlayer.");
            return false;
        }
        
        if (splineGenerator == null)
        {
            Debug.LogError("SimpleDollyCart: No SplineMathGenerator found in scene!");
            return false;
        }
        
        return true;
    }
    
    void InitializeDollyCart()
    {
        // Inicializar posición del dolly cart
        if (playerFollower != null)
        {
            currentDollyDistance = playerFollower.GetCurrentDistance();
            
            // Inicializar posiciones para movimiento suave
            Vector3 initialSplinePosition = splineGenerator.GetSplinePosition(currentDollyDistance);
            currentDollyPosition = initialSplinePosition;
            targetDollyPosition = initialSplinePosition;
            transform.position = initialSplinePosition;
            
            Debug.Log($"Dolly cart positioned at distance {currentDollyDistance:F1}, position {initialSplinePosition}");
        }
    }
    
    // CAMBIADO: Usar LateUpdate para moverse después del player
    void LateUpdate()
    {
        if (playerFollower != null && splineGenerator != null && splineGenerator.HasValidSpline())
        {
            UpdateDollyMovement();
        }
    }
    
    void UpdateDollyMovement()
    {
        // Obtener distancia actual del jugador (sincronización perfecta)
        currentDollyDistance = playerFollower.GetCurrentDistance();
        
        // Calcular posición objetivo en el spline
        targetDollyPosition = splineGenerator.GetSplinePosition(currentDollyDistance);
        
        // NUEVO: Aplicar movimiento suave igual que ImprovedSplineFollower
        ApplySmoothMovement();
    }
    
    void ApplySmoothMovement()
    {
        float smoothingValue;
        
        // Usar el mismo smoothing que el player o valor personalizado
        if (copyPlayerSmoothing && playerFollower != null)
        {
            // Acceder al valor de smoothing del player mediante reflexión o valor típico
            smoothingValue = 10f; // Valor por defecto que usa ImprovedSplineFollower
        }
        else
        {
            smoothingValue = customSmoothing;
        }
        
        // COPIADO DE ImprovedSplineFollower: Movimiento suave con Transform
        float smoothTime = Mathf.Clamp01(smoothingValue * Time.deltaTime);
        currentDollyPosition = Vector3.Lerp(currentDollyPosition, targetDollyPosition, smoothTime);
        transform.position = currentDollyPosition;
        
        // También suavizar la rotación (igual que el player)
        Vector3 splineDirection = splineGenerator.GetSplineDirection(currentDollyDistance);
        if (splineDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(splineDirection, Vector3.up);
            float rotationSmoothing = 8f; // Mismo valor que usa ImprovedSplineFollower
            float rotationTime = Mathf.Clamp01(rotationSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationTime);
        }
    }
    
    // Métodos públicos de información
    public float GetCurrentDistance()
    {
        return currentDollyDistance;
    }
    
    public float GetPlayerDistance()
    {
        return playerFollower != null ? playerFollower.GetCurrentDistance() : 0f;
    }
    
    public bool IsInSync()
    {
        float playerDistance = GetPlayerDistance();
        return Mathf.Abs(currentDollyDistance - playerDistance) < 0.1f;
    }
    
    public Vector3 GetSplinePosition()
    {
        return currentDollyPosition; // Devolver la posición suavizada actual
    }
    
    public Vector3 GetSplineDirection()
    {
        return splineGenerator != null ? splineGenerator.GetSplineDirection(currentDollyDistance) : Vector3.forward;
    }
    
    // Métodos de configuración
    public void SetCopyPlayerSmoothing(bool enable)
    {
        copyPlayerSmoothing = enable;
        Debug.Log($"Dolly cart copy player smoothing set to: {enable}");
    }
    
    public void SetCustomSmoothing(float smoothValue)
    {
        customSmoothing = Mathf.Max(0f, smoothValue);
        Debug.Log($"Dolly cart custom smoothing set to: {customSmoothing}");
    }
    
    public void ResetToPlayerPosition()
    {
        if (playerFollower != null)
        {
            currentDollyDistance = playerFollower.GetCurrentDistance();
            Vector3 resetPosition = splineGenerator.GetSplinePosition(currentDollyDistance);
            currentDollyPosition = resetPosition;
            targetDollyPosition = resetPosition;
            transform.position = resetPosition;
            Debug.Log($"Dolly cart reset to player position: {currentDollyDistance:F1}");
        }
    }
    
    // Método de debugging
    public void LogDollyInfo()
    {
        Debug.Log("=== SIMPLE DOLLY CART INFO ===");
        Debug.Log($"Current distance: {currentDollyDistance:F1}");
        Debug.Log($"Player distance: {GetPlayerDistance():F1}");
        Debug.Log($"Copy player smoothing: {copyPlayerSmoothing}");
        Debug.Log($"Custom smoothing: {customSmoothing}");
        Debug.Log($"In sync: {IsInSync()}");
        Debug.Log($"Current position: {currentDollyPosition}");
        Debug.Log($"Target position: {targetDollyPosition}");
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;
        
        // Mostrar posición actual del dolly cart
        Gizmos.color = dollyColor;
        Gizmos.DrawSphere(transform.position, debugSphereSize);
        
        // Mostrar dirección del dolly
        if (splineGenerator != null && splineGenerator.HasValidSpline())
        {
            Vector3 direction = GetSplineDirection();
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
        
        // Mostrar conexión con el jugador
        if (playerFollower != null)
        {
            Vector3 playerSplinePos = splineGenerator.GetSplinePosition(playerFollower.GetCurrentDistance());
            
            if (IsInSync())
            {
                // Si están sincronizados, mostrar línea verde
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, playerSplinePos + Vector3.up * 0.5f);
            }
            else
            {
                // Si no están sincronizados, mostrar línea roja
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, playerSplinePos);
            }
            
            // Mostrar posición del jugador en el spline
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerSplinePos, debugSphereSize * 0.7f);
        }
        
        // Información de debug en editor
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Vector3 textPos = transform.position + Vector3.up * 2f;
            string info = $"DOLLY CART\n";
            info += $"Distance: {currentDollyDistance:F1}\n";
            info += $"Player: {GetPlayerDistance():F1}\n";
            info += $"Sync: {(IsInSync() ? "✓" : "✗")}\n";
            info += $"Mode: {(copyPlayerSmoothing ? "Copy Player" : "Custom")}";
            
            UnityEditor.Handles.Label(textPos, info);
        }
        #endif
    }
}