using System.Collections.Generic;
using UnityEngine;

public class ChivaSplineGenerator : MonoBehaviour
{
    [Header("Route Configuration")]
    [SerializeField] private float totalRouteDistance = 25000f; // 25 kilómetros
    [SerializeField] private int totalSegments = 250; // 250 segmentos de ~100m cada uno
    [SerializeField] private float segmentLength = 100f; // Longitud por segmento
    [SerializeField] private int pointsPerSegment = 15; // Resolución del spline
    
    [Header("Road Variation")]
    [SerializeField] private float maxHeightVariation = 3f; // Menos variación que el original
    [SerializeField] private float maxWidthVariation = 8f; // Curvas horizontales
    [SerializeField] private float curveSmoothness = 2.5f;
    [SerializeField] private AnimationCurve difficultyProgression = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float maxSlopeAngle = 15f; // Más suave para WebGL
    
    [Header("Passenger Zones")]
    [SerializeField] private float roadWidth = 6f; // Ancho de la carretera
    [SerializeField] private float passengerSpawnWidth = 3f; // Hasta dónde pueden aparecer pasajeros
    [SerializeField] private float minPassengerDistance = 50f; // Distancia mínima entre posibles spawns
    
    [Header("Performance")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private int maxPointsPerFrame = 500; // Evitar lag en WebGL
    [SerializeField] private bool showGenerationProgress = true;
    
    [Header("Debug")]
    [SerializeField] private bool showSpline = true;
    [SerializeField] private bool showPassengerZones = false;
    [SerializeField] private Color splineColor = Color.yellow;
    [SerializeField] private Color passengerZoneColor = Color.green;
    [SerializeField] private float debugSphereSize = 0.5f;
    
    // Datos del spline
    private List<Vector3> splinePoints = new List<Vector3>();
    private List<Vector3> splineDirections = new List<Vector3>();
    private List<Vector3> splineRightVectors = new List<Vector3>();
    private List<float> splineDistances = new List<float>();
    private List<ChivaSplineSegment> splineSegments = new List<ChivaSplineSegment>();
    
    // Estado de generación
    private bool isGenerationComplete = false;
    private int generatedSegments = 0;
    private float currentTotalDistance = 0f;
    private Vector3 lastSegmentPosition = Vector3.zero;
    private Quaternion lastSegmentRotation = Quaternion.identity;
    
    // Sistema de eventos
    public System.Action<float> OnGenerationProgress; // 0-1 progress
    public System.Action OnGenerationComplete;
    public System.Action<ChivaSplineSegment> OnSegmentGenerated;
    
    [System.Serializable]
    public class ChivaSplineSegment
    {
        public List<Vector3> points = new List<Vector3>();
        public List<Vector3> directions = new List<Vector3>();
        public List<Vector3> rightVectors = new List<Vector3>();
        public Vector3 startPosition;
        public Vector3 endPosition;
        public int segmentIndex;
        public float startDistance;
        public float endDistance;
        public float difficultyFactor; // 0-1, aumenta con la distancia
        public List<Vector3> leftPassengerZone = new List<Vector3>();
        public List<Vector3> rightPassengerZone = new List<Vector3>();
    }
    
    void Start()
    {
        Debug.Log("=== CHIVA SPLINE GENERATOR STARTING ===");
        Debug.Log($"Route configuration: {totalRouteDistance/1000f}km, {totalSegments} segments");
        
        if (generateOnStart)
        {
            StartCoroutine(GenerateCompleteRoute());
        }
    }
    
    private System.Collections.IEnumerator GenerateCompleteRoute()
    {
        Debug.Log("Starting complete route generation...");
        OnGenerationProgress?.Invoke(0f);
        
        // Inicializar posición de inicio
        lastSegmentPosition = transform.position;
        lastSegmentRotation = transform.rotation;
        
        int pointsGeneratedThisFrame = 0;
        
        for (int i = 0; i < totalSegments; i++)
        {
            // Generar segmento
            ChivaSplineSegment newSegment = GenerateChivaSegment(i);
            splineSegments.Add(newSegment);
            generatedSegments++;
            
            // Actualizar progreso
            float progress = (float)i / totalSegments;
            OnGenerationProgress?.Invoke(progress);
            
            OnSegmentGenerated?.Invoke(newSegment);
            
            // Control de performance - no generar demasiados puntos por frame
            pointsGeneratedThisFrame += newSegment.points.Count;
            if (pointsGeneratedThisFrame >= maxPointsPerFrame)
            {
                pointsGeneratedThisFrame = 0;
                if (showGenerationProgress)
                {
                    Debug.Log($"Generated segment {i+1}/{totalSegments} ({progress*100f:F1}%)");
                }
                yield return null; // Esperar siguiente frame
            }
        }
        
        // Finalizar generación
        isGenerationComplete = true;
        OnGenerationProgress?.Invoke(1f);
        OnGenerationComplete?.Invoke();
        
        Debug.Log($"Route generation complete! Total distance: {currentTotalDistance:F1}m");
        Debug.Log($"Generated {splinePoints.Count} points, {splineSegments.Count} segments");
    }
    
    private ChivaSplineSegment GenerateChivaSegment(int segmentIndex)
    {
        ChivaSplineSegment segment = new ChivaSplineSegment();
        segment.segmentIndex = segmentIndex;
        segment.startPosition = lastSegmentPosition;
        segment.startDistance = currentTotalDistance;
        
        // Calcular factor de dificultad basado en progreso
        float routeProgress = (float)segmentIndex / totalSegments;
        segment.difficultyFactor = difficultyProgression.Evaluate(routeProgress);
        
        // Generar puntos de control con dificultad creciente
        Vector3 controlPoint1 = GenerateControlPoint(segment.difficultyFactor);
        Vector3 controlPoint2 = GenerateControlPoint(segment.difficultyFactor);
        
        // Generar puntos del spline para este segmento
        List<Vector3> segmentPoints = GenerateSegmentPoints(
            segment.startPosition, 
            controlPoint1, 
            controlPoint2, 
            segmentLength
        );
        
        // Procesar puntos y calcular direcciones/distancias
        ProcessSegmentPoints(segment, segmentPoints);
        
        // Generar zonas de pasajeros a los lados
        GeneratePassengerZones(segment);
        
        // Actualizar estado para próximo segmento
        segment.endPosition = segment.points[segment.points.Count - 1];
        segment.endDistance = currentTotalDistance;
        lastSegmentPosition = segment.endPosition;
        
        if (segment.directions.Count > 0)
        {
            lastSegmentRotation = Quaternion.LookRotation(segment.directions[segment.directions.Count - 1]);
        }
        
        return segment;
    }
    
    private Vector3 GenerateControlPoint(float difficultyFactor)
    {
        // Las curvas se hacen más pronunciadas con la dificultad
        float maxVariation = maxWidthVariation * (0.5f + difficultyFactor);
        float heightVariation = maxHeightVariation * difficultyFactor;
        
        Vector3 forward = lastSegmentRotation * Vector3.forward * segmentLength;
        Vector3 lateral = lastSegmentRotation * Vector3.right * 
            Random.Range(-maxVariation, maxVariation) * widthCurve.Evaluate(Random.value);
        Vector3 vertical = Vector3.up * 
            Random.Range(-heightVariation, heightVariation) * heightCurve.Evaluate(Random.value);
        
        return lastSegmentPosition + forward + lateral + vertical;
    }
    
    private List<Vector3> GenerateSegmentPoints(Vector3 start, Vector3 control1, Vector3 control2, float length)
    {
        List<Vector3> points = new List<Vector3>();
        Vector3 end = control2;
        
        // Generar curva bezier cúbica suavizada
        for (int i = 0; i <= pointsPerSegment; i++)
        {
            float t = (float)i / pointsPerSegment;
            Vector3 point = CubicBezier(start, control1, control2, end, t);
            points.Add(point);
        }
        
        return points;
    }
    
    private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;
        
        return p;
    }
    
    private void ProcessSegmentPoints(ChivaSplineSegment segment, List<Vector3> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            segment.points.Add(points[i]);
            splinePoints.Add(points[i]);
            
            // Calcular dirección
            Vector3 direction = Vector3.forward;
            if (i > 0)
            {
                direction = (points[i] - points[i - 1]).normalized;
                float distance = Vector3.Distance(points[i], points[i - 1]);
                currentTotalDistance += distance;
            }
            else if (splineDirections.Count > 0)
            {
                direction = splineDirections[splineDirections.Count - 1];
            }
            
            segment.directions.Add(direction);
            splineDirections.Add(direction);
            splineDistances.Add(currentTotalDistance);
            
            // Calcular vector derecha
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            segment.rightVectors.Add(right);
            splineRightVectors.Add(right);
        }
    }
    
    private void GeneratePassengerZones(ChivaSplineSegment segment)
    {
        // Generar zonas donde pueden aparecer pasajeros a los lados
        for (int i = 0; i < segment.points.Count; i++)
        {
            Vector3 point = segment.points[i];
            Vector3 right = segment.rightVectors[i];
            
            // Zona izquierda (offset negativo)
            Vector3 leftZone = point + right * (-roadWidth * 0.5f - passengerSpawnWidth * 0.5f);
            segment.leftPassengerZone.Add(leftZone);
            
            // Zona derecha (offset positivo)  
            Vector3 rightZone = point + right * (roadWidth * 0.5f + passengerSpawnWidth * 0.5f);
            segment.rightPassengerZone.Add(rightZone);
        }
    }
    
    // ========== MÉTODOS PÚBLICOS DEL SPLINE ==========
    
    public Vector3 GetSplinePosition(float distance)
    {
        if (!isGenerationComplete || splinePoints.Count < 2) 
        {
            Debug.LogWarning("Spline not ready for queries");
            return Vector3.zero;
        }
        
        // Clamp distance to valid range
        distance = Mathf.Clamp(distance, 0f, currentTotalDistance);
        
        int segmentIndex = GetSegmentIndexAtDistance(distance);
        if (segmentIndex >= splinePoints.Count - 1) 
            return splinePoints[splinePoints.Count - 1];
        
        float segmentDistance = distance - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        float segmentLength = splineDistances[segmentIndex] - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        
        if (segmentLength == 0) return splinePoints[segmentIndex];
        
        float t = Mathf.Clamp01(segmentDistance / segmentLength);
        return Vector3.Lerp(splinePoints[segmentIndex], splinePoints[segmentIndex + 1], t);
    }
    
    public Vector3 GetSplineDirection(float distance)
    {
        if (!isGenerationComplete || splineDirections.Count < 2) return Vector3.forward;
        
        distance = Mathf.Clamp(distance, 0f, currentTotalDistance);
        int segmentIndex = GetSegmentIndexAtDistance(distance);
        
        if (segmentIndex >= splineDirections.Count - 1) 
            return splineDirections[splineDirections.Count - 1];
        
        float segmentDistance = distance - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        float segmentLength = splineDistances[segmentIndex] - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        
        if (segmentLength == 0) return splineDirections[segmentIndex];
        
        float t = Mathf.Clamp01(segmentDistance / segmentLength);
        return Vector3.Slerp(splineDirections[segmentIndex], splineDirections[segmentIndex + 1], t);
    }
    
    public Vector3 GetSplineRight(float distance)
    {
        if (!isGenerationComplete || splineRightVectors.Count < 2)
        {
            Vector3 forward = GetSplineDirection(distance);
            return Vector3.Cross(Vector3.up, forward).normalized;
        }
        
        distance = Mathf.Clamp(distance, 0f, currentTotalDistance);
        int segmentIndex = GetSegmentIndexAtDistance(distance);
        
        if (segmentIndex >= splineRightVectors.Count - 1) 
            return splineRightVectors[splineRightVectors.Count - 1];
        
        float segmentDistance = distance - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        float segmentLength = splineDistances[segmentIndex] - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        
        if (segmentLength == 0) return splineRightVectors[segmentIndex];
        
        float t = Mathf.Clamp01(segmentDistance / segmentLength);
        return Vector3.Slerp(splineRightVectors[segmentIndex], splineRightVectors[segmentIndex + 1], t);
    }
    
    // ========== MÉTODOS PARA PASAJEROS ==========
    
    public Vector3 GetPassengerSpawnPosition(float distance, bool isRightSide)
    {
        Vector3 roadPosition = GetSplinePosition(distance);
        Vector3 rightVector = GetSplineRight(distance);
        
        float spawnDistance = roadWidth * 0.5f + Random.Range(0.5f, passengerSpawnWidth);
        if (!isRightSide) spawnDistance = -spawnDistance;
        
        return roadPosition + rightVector * spawnDistance;
    }
    
    public bool IsValidPassengerDistance(float distance)
    {
        return distance >= 0f && distance <= currentTotalDistance - minPassengerDistance;
    }
    
    public float GetRouteProgress(float distance)
    {
        if (currentTotalDistance <= 0) return 0f;
        return Mathf.Clamp01(distance / currentTotalDistance);
    }
    
    public float GetDifficultyAtDistance(float distance)
    {
        float progress = GetRouteProgress(distance);
        return difficultyProgression.Evaluate(progress);
    }
    
    // ========== GETTERS ==========
    
    public float GetTotalDistance() => currentTotalDistance;
    public float GetTotalRouteDistance() => totalRouteDistance;
    public bool IsGenerationComplete() => isGenerationComplete;
    public int GetGeneratedSegments() => generatedSegments;
    public int GetTotalSegments() => totalSegments;
    public bool HasValidSpline() => isGenerationComplete && splinePoints.Count >= 2;
    
    // ========== MÉTODOS PRIVADOS ==========
    
    private int GetSegmentIndexAtDistance(float distance)
    {
        if (splineDistances.Count == 0) return 0;
        
        // Búsqueda binaria optimizada
        int left = 0;
        int right = splineDistances.Count - 1;
        
        while (left <= right)
        {
            int mid = (left + right) / 2;
            if (splineDistances[mid] >= distance)
            {
                if (mid == 0 || splineDistances[mid - 1] < distance)
                    return mid;
                right = mid - 1;
            }
            else
            {
                left = mid + 1;
            }
        }
        
        return splineDistances.Count - 1;
    }
    
    // ========== MÉTODOS PÚBLICOS DE CONTROL ==========
    
    public void ForceRegenerate()
    {
        Debug.Log("Forcing route regeneration...");
        ClearCurrentRoute();
        StartCoroutine(GenerateCompleteRoute());
    }
    
    private void ClearCurrentRoute()
    {
        splinePoints.Clear();
        splineDirections.Clear();
        splineRightVectors.Clear();
        splineDistances.Clear();
        splineSegments.Clear();
        
        isGenerationComplete = false;
        generatedSegments = 0;
        currentTotalDistance = 0f;
        lastSegmentPosition = transform.position;
        lastSegmentRotation = transform.rotation;
    }
    
    // ========== GIZMOS Y DEBUG ==========
    
    void OnDrawGizmos()
    {
        if (!showSpline || !isGenerationComplete || splinePoints.Count < 2) return;
        
        // Dibujar spline principal
        Gizmos.color = splineColor;
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
        }
        
        // Dibujar puntos de segmento
        Gizmos.color = Color.red;
        foreach (var segment in splineSegments)
        {
            if (segment.segmentIndex % 10 == 0) // Solo cada 10 segmentos
            {
                Gizmos.DrawSphere(segment.startPosition, debugSphereSize);
            }
        }
        
        // Dibujar direcciones
        Gizmos.color = Color.blue;
        for (int i = 0; i < splinePoints.Count; i += 20) // Cada 20 puntos
        {
            if (i < splineDirections.Count)
            {
                Gizmos.DrawRay(splinePoints[i], splineDirections[i] * 3f);
            }
        }
        
        // Dibujar zonas de pasajeros si está habilitado
        if (showPassengerZones)
        {
            Gizmos.color = passengerZoneColor;
            foreach (var segment in splineSegments)
            {
                if (segment.segmentIndex % 5 == 0) // Solo cada 5 segmentos
                {
                    for (int i = 0; i < segment.leftPassengerZone.Count; i += 5)
                    {
                        Gizmos.DrawWireSphere(segment.leftPassengerZone[i], 0.3f);
                        Gizmos.DrawWireSphere(segment.rightPassengerZone[i], 0.3f);
                    }
                }
            }
        }
        
        // Información de debug en editor
        #if UNITY_EDITOR
        if (isGenerationComplete)
        {
            Vector3 infoPos = transform.position + Vector3.up * 10f;
            string info = $"CHIVA ROUTE\n{currentTotalDistance/1000f:F1}km / {totalRouteDistance/1000f:F1}km\n{generatedSegments} segments";
            UnityEditor.Handles.Label(infoPos, info);
        }
        #endif
    }
}
