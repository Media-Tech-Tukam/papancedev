using System.Collections.Generic;
using UnityEngine;

public class SplineMathGenerator : MonoBehaviour
{
    [Header("Spline Generation")]
    public int segmentsPerTrack = 50;
    public float segmentLength = 2f;
    public int pointsPerSegment = 10;
    
    [Header("Track Variation")]
    public float maxHeightVariation = 10f;
    public float maxWidthVariation = 10f;
    public int curvePoint1 = 25;
    public int curvePoint2 = 50;
    public float curveSmoothness = 2f;
    public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve widthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float maxSlopeAngle = 25f;
    
    [Header("Infinite Generation - OPTIMIZADO")]
    public int maxTracksInMemory = 4;
    public float baseTriggerDistance = 100f;
    public float speedMultiplier = 2f;
    public float maxTriggerDistance = 300f;
    public float cleanupSafetyDistance = 150f;
    
    [Header("Performance")]
    public bool adaptiveGeneration = true;
    public float lowSpeedThreshold = 10f;
    public float highSpeedThreshold = 15f;
    
    [Header("Debug")]
    public bool showSpline = true;
    public Color splineColor = Color.yellow;
    public float debugSphereSize = 0.3f;
    public bool debugPerformance = true;
    
    private List<Vector3> splinePoints = new List<Vector3>();
    private List<Vector3> splineDirections = new List<Vector3>();
    private List<float> splineDistances = new List<float>();
    private List<SplineSegment> splineSegments = new List<SplineSegment>();
    
    private float totalSplineLength = 0f;
    private float removedSplineLength = 0f;
    private Vector3 lastTrackEndPosition = Vector3.zero;
    private Quaternion lastTrackEndRotation = Quaternion.identity;
    private int currentTrackIndex = 0;
    
    private ImprovedSplineFollower playerFollower;
    private float lastPlayerDistance = 0f;
    private float lastPlayerSpeed = 0f;
    
    private float currentTriggerDistance;
    private int segmentsGeneratedThisFrame = 0;
    private float lastGenerationTime = 0f;
    
    [System.Serializable]
    public class SplineSegment
    {
        public List<Vector3> points = new List<Vector3>();
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Vector3 startDirection;
        public Vector3 endDirection;
        public int segmentIndex;
        public bool triggerGenerated = false;
        public float startDistance;
        public float endDistance;
        public float generationTime;
    }
    
    void Start()
    {
        Debug.Log("=== SPLINE MATH GENERATOR STARTING (SPEED-OPTIMIZED) ===");
        
        playerFollower = FindObjectOfType<ImprovedSplineFollower>();
        currentTriggerDistance = baseTriggerDistance;
        
        GenerateInitialSplineSegments();
        
        Debug.Log($"Initial spline generated: {splinePoints.Count} points, length: {totalSplineLength:F1}");
        Debug.Log($"Adaptive generation: {adaptiveGeneration}, Base trigger: {baseTriggerDistance}");
    }
    
    void Update()
    {
        UpdatePlayerTracking();
        UpdateTriggerDistance();
        CleanupOldSplineSegments();
        
        segmentsGeneratedThisFrame = 0;
    }
    
    void UpdatePlayerTracking()
    {
        if (playerFollower != null)
        {
            float previousDistance = lastPlayerDistance;
            lastPlayerDistance = playerFollower.GetCurrentDistance();
            lastPlayerSpeed = playerFollower.GetSpeed();
            
            if (debugPerformance && Time.time - lastGenerationTime > 1f)
            {
                float distanceTraveled = lastPlayerDistance - previousDistance;
                float generationBuffer = GetTotalLength() - lastPlayerDistance;
                
                if (generationBuffer < currentTriggerDistance * 0.5f)
                {
                    Debug.LogWarning($"LOW GENERATION BUFFER! Buffer: {generationBuffer:F1}, Player speed: {lastPlayerSpeed:F1}");
                }
            }
        }
    }
    
    void UpdateTriggerDistance()
    {
        if (!adaptiveGeneration || playerFollower == null) return;
        
        float speedFactor = Mathf.Lerp(1f, speedMultiplier, 
            Mathf.InverseLerp(lowSpeedThreshold, highSpeedThreshold, lastPlayerSpeed));
        
        currentTriggerDistance = baseTriggerDistance * speedFactor;
        currentTriggerDistance = Mathf.Clamp(currentTriggerDistance, baseTriggerDistance, maxTriggerDistance);
    }
    
    void GenerateInitialSplineSegments()
    {
        int initialSegments = adaptiveGeneration ? 3 : 2;
        
        for (int i = 0; i < initialSegments; i++)
        {
            GenerateSplineSegment();
        }
    }
    
    public void GenerateSplineSegment()
    {
        if (segmentsGeneratedThisFrame >= 2)
        {
            Debug.Log("Max segments per frame reached, deferring generation");
            return;
        }
        
        float generationStartTime = Time.realtimeSinceStartup;
        
        Debug.Log($"=== GENERATING SPLINE SEGMENT {currentTrackIndex} (Speed: {lastPlayerSpeed:F1}) ===");
        
        SplineSegment newSegment = new SplineSegment();
        newSegment.segmentIndex = currentTrackIndex;
        newSegment.startPosition = lastTrackEndPosition;
        newSegment.startDirection = lastTrackEndRotation * Vector3.forward;
        newSegment.startDistance = totalSplineLength;
        newSegment.generationTime = Time.time;
        
        Vector3 controlPoint1 = GenerateControlPoint();
        Vector3 controlPoint2 = GenerateControlPoint();
        
        List<Vector3> segmentSplinePoints = GenerateSegmentSplinePoints(controlPoint1, controlPoint2);
        
        int startIndex = splinePoints.Count;
        splinePoints.AddRange(segmentSplinePoints);
        newSegment.points.AddRange(segmentSplinePoints);
        
        for (int i = 0; i < segmentSplinePoints.Count; i++)
        {
            Vector3 direction = Vector3.forward;
            if (startIndex + i > 0)
            {
                Vector3 prevPoint = splinePoints[startIndex + i - 1];
                Vector3 currentPoint = segmentSplinePoints[i];
                direction = (currentPoint - prevPoint).normalized;
                float distance = Vector3.Distance(currentPoint, prevPoint);
                totalSplineLength += distance;
            }
            
            splineDirections.Add(direction);
            splineDistances.Add(totalSplineLength);
        }
        
        newSegment.endPosition = segmentSplinePoints[segmentSplinePoints.Count - 1];
        newSegment.endDirection = splineDirections[splineDirections.Count - 1];
        newSegment.endDistance = totalSplineLength;
        
        lastTrackEndPosition = newSegment.endPosition;
        lastTrackEndRotation = Quaternion.LookRotation(newSegment.endDirection);
        
        splineSegments.Add(newSegment);
        currentTrackIndex++;
        segmentsGeneratedThisFrame++;
        lastGenerationTime = Time.time;
        
        float generationTime = (Time.realtimeSinceStartup - generationStartTime) * 1000f;
        
        Debug.Log($"Segment {newSegment.segmentIndex} generated in {generationTime:F2}ms");
        Debug.Log($"Distance range: {newSegment.startDistance:F1} -> {newSegment.endDistance:F1}");
        Debug.Log($"Total length: {totalSplineLength:F1}, Trigger distance: {currentTriggerDistance:F1}");
        
        BroadcastSplineUpdated();
    }
    
    Vector3 GenerateControlPoint()
    {
        float heightVariation = Random.Range(-maxHeightVariation, maxHeightVariation);
        float widthVariation = Random.Range(-maxWidthVariation, maxWidthVariation);
        
        heightVariation *= 0.7f;
        widthVariation *= 0.7f;
        
        return new Vector3(widthVariation, heightVariation, 0);
    }
    
    List<Vector3> GenerateSegmentSplinePoints(Vector3 controlPoint1, Vector3 controlPoint2)
    {
        List<Vector3> segmentPoints = new List<Vector3>();
        
        for (int i = 0; i < segmentsPerTrack; i++)
        {
            Vector3 basePosition = lastTrackEndPosition + Vector3.forward * (i * segmentLength);
            Vector3 curveOffset = CalculateSmoothCurveOffset(i, controlPoint1, controlPoint2);
            Vector3 segmentPosition = basePosition + curveOffset;
            
            if (i == 0 && segmentPoints.Count == 0)
            {
                segmentPoints.Add(segmentPosition);
            }
            
            if (i < segmentsPerTrack - 1)
            {
                Vector3 nextBasePosition = lastTrackEndPosition + Vector3.forward * ((i + 1) * segmentLength);
                Vector3 nextCurveOffset = CalculateSmoothCurveOffset(i + 1, controlPoint1, controlPoint2);
                Vector3 nextSegmentPosition = nextBasePosition + nextCurveOffset;
                
                for (int j = 0; j < pointsPerSegment; j++)
                {
                    float t = (float)j / pointsPerSegment;
                    Vector3 interpolatedPoint = Vector3.Lerp(segmentPosition, nextSegmentPosition, t);
                    segmentPoints.Add(interpolatedPoint);
                }
            }
        }
        
        return segmentPoints;
    }
    
    Vector3 CalculateSmoothCurveOffset(int segmentIndex, Vector3 controlPoint1, Vector3 controlPoint2)
    {
        float t = (float)segmentIndex / (segmentsPerTrack - 1);
        
        Vector3 p0 = Vector3.zero;
        Vector3 p1 = controlPoint1 * 0.5f;
        Vector3 p2 = controlPoint1 + (controlPoint2 - controlPoint1) * 0.5f;
        Vector3 p3 = controlPoint2;
        
        return CalculateCubicBezier(p0, p1, p2, p3, t);
    }
    
    Vector3 CalculateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float smoothT = heightCurve.Evaluate(t);
        
        float u = 1f - smoothT;
        float tt = smoothT * smoothT;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * smoothT;
        
        Vector3 result = uuu * p0;
        result += 3 * uu * smoothT * p1;
        result += 3 * u * tt * p2;
        result += ttt * p3;
        
        return result;
    }
    
    void CleanupOldSplineSegments()
    {
        if (splineSegments.Count > maxTracksInMemory)
        {
            SplineSegment oldestSegment = splineSegments[0];
            
            float dynamicSafetyDistance = cleanupSafetyDistance;
            if (lastPlayerSpeed > highSpeedThreshold)
                dynamicSafetyDistance *= 1.5f;
            
            float playerDistanceFromOldSegment = lastPlayerDistance - oldestSegment.startDistance;
            
            if (playerDistanceFromOldSegment < dynamicSafetyDistance)
            {
                if (debugPerformance)
                {
                    Debug.Log($"Skipping cleanup - player too close. Distance: {playerDistanceFromOldSegment:F1}, Required: {dynamicSafetyDistance:F1}");
                }
                return;
            }
            
            Debug.Log($"SAFE CLEANUP: Player at {lastPlayerDistance:F1}, removing segment {oldestSegment.startDistance:F1}-{oldestSegment.endDistance:F1}");
            
            int pointsToRemove = oldestSegment.points.Count;
            if (pointsToRemove <= splinePoints.Count)
            {
                float segmentLength = oldestSegment.endDistance - oldestSegment.startDistance;
                removedSplineLength += segmentLength;
                
                splinePoints.RemoveRange(0, pointsToRemove);
                splineDirections.RemoveRange(0, pointsToRemove);
                splineDistances.RemoveRange(0, pointsToRemove);
                
                for (int i = 1; i < splineSegments.Count; i++)
                {
                    splineSegments[i].startDistance -= segmentLength;
                    splineSegments[i].endDistance -= segmentLength;
                }
                
                for (int i = 0; i < splineDistances.Count; i++)
                {
                    splineDistances[i] -= segmentLength;
                }
                
                totalSplineLength -= segmentLength;
            }
            
            splineSegments.RemoveAt(0);
            Debug.Log($"Cleanup completed. Segments: {splineSegments.Count}, Length: {totalSplineLength:F1}");
        }
    }
    
    // 🔥 **VERSION LIMPIA SIN COLLECTIBLES NI OBSTACLES**
    void BroadcastSplineUpdated()
    {
        foreach (var component in FindObjectsOfType<MonoBehaviour>())
        {
            if (component is TrackVisualizer visualizer)
            {
                visualizer.OnSplineUpdated();
            }
        }
    }
    
    public Vector3 GetSplinePosition(float distance)
    {
        if (splinePoints.Count < 2) return Vector3.zero;
        
        float adjustedDistance = distance - removedSplineLength;
        int segmentIndex = GetSegmentAtDistance(adjustedDistance);
        
        if (segmentIndex >= splinePoints.Count - 1) 
            return splinePoints[splinePoints.Count - 1];
        
        if (segmentIndex < 0)
        {
            if (debugPerformance)
                Debug.LogWarning($"Negative segment index for distance {distance} (adjusted: {adjustedDistance})");
            return splinePoints.Count > 0 ? splinePoints[0] : Vector3.zero;
        }
        
        float segmentDistance = adjustedDistance - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        float segmentLength = splineDistances[segmentIndex] - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        
        if (segmentLength == 0) return splinePoints[segmentIndex];
        
        float t = Mathf.Clamp01(segmentDistance / segmentLength);
        return Vector3.Lerp(splinePoints[segmentIndex], splinePoints[segmentIndex + 1], t);
    }
    
    public Vector3 GetSplineDirection(float distance)
    {
        if (splineDirections.Count < 2) return Vector3.forward;
        
        float adjustedDistance = distance - removedSplineLength;
        int segmentIndex = GetSegmentAtDistance(adjustedDistance);
        
        if (segmentIndex >= splineDirections.Count - 1) 
            return splineDirections[splineDirections.Count - 1];
        
        if (segmentIndex < 0)
            return splineDirections.Count > 0 ? splineDirections[0] : Vector3.forward;
        
        float segmentDistance = adjustedDistance - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        float segmentLength = splineDistances[segmentIndex] - (segmentIndex > 0 ? splineDistances[segmentIndex - 1] : 0);
        
        if (segmentLength == 0) return splineDirections[segmentIndex];
        
        float t = Mathf.Clamp01(segmentDistance / segmentLength);
        return Vector3.Slerp(splineDirections[segmentIndex], splineDirections[segmentIndex + 1], t);
    }
    
    public Vector3 GetSplineRight(float distance)
    {
        Vector3 forward = GetSplineDirection(distance);
        return Vector3.Cross(Vector3.up, forward).normalized;
    }
    
    public float GetTotalLength()
    {
        return totalSplineLength + removedSplineLength;
    }
    
    public float GetPlayerDistance()
    {
        return lastPlayerDistance;
    }
    
    public bool HasValidSpline()
    {
        return splinePoints.Count >= 2;
    }
    
    int GetSegmentAtDistance(float distance)
    {
        if (splineDistances.Count == 0) return 0;
        
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
    
    public void TriggerNextSegment(float playerDistance)
    {
        float totalLength = GetTotalLength();
        float distanceToEnd = totalLength - playerDistance;
        
        if (distanceToEnd <= currentTriggerDistance)
        {
            Debug.Log($"Triggering segment: Player {playerDistance:F1}, Total {totalLength:F1}, Distance to end: {distanceToEnd:F1}, Trigger: {currentTriggerDistance:F1}");
            GenerateSplineSegment();
        }
    }
    
    public float GetCurrentTriggerDistance()
    {
        return currentTriggerDistance;
    }
    
    public float GetGenerationBuffer()
    {
        return GetTotalLength() - lastPlayerDistance;
    }
    
    public void LogSegmentInfo()
    {
        Debug.Log("=== SEGMENT INFO ===");
        for (int i = 0; i < splineSegments.Count; i++)
        {
            var segment = splineSegments[i];
            Debug.Log($"Segment {i}: Index {segment.segmentIndex}, Distance {segment.startDistance:F1}-{segment.endDistance:F1}");
        }
        Debug.Log($"Player: {lastPlayerDistance:F1} (speed: {lastPlayerSpeed:F1})");
        Debug.Log($"Removed: {removedSplineLength:F1}, Total: {totalSplineLength:F1}");
        Debug.Log($"Generation buffer: {GetGenerationBuffer():F1}, Trigger: {currentTriggerDistance:F1}");
    }
    
    void OnDrawGizmos()
    {
        if (!showSpline || splinePoints.Count < 2) return;
        
        Gizmos.color = splineColor;
        for (int i = 0; i < splinePoints.Count - 1; i++)
        {
            Gizmos.DrawLine(splinePoints[i], splinePoints[i + 1]);
        }
        
        Gizmos.color = Color.red;
        for (int i = 0; i < splinePoints.Count; i += pointsPerSegment * 5)
        {
            if (i < splinePoints.Count)
                Gizmos.DrawSphere(splinePoints[i], debugSphereSize);
        }
        
        Gizmos.color = Color.blue;
        for (int i = 0; i < splinePoints.Count; i += pointsPerSegment * 10)
        {
            if (i < splineDirections.Count)
            {
                Gizmos.DrawRay(splinePoints[i], splineDirections[i] * 2f);
            }
        }
        
        Gizmos.color = Color.magenta;
        foreach (var segment in splineSegments)
        {
            Gizmos.DrawWireSphere(segment.startPosition, 0.5f);
            Gizmos.DrawWireSphere(segment.endPosition, 0.5f);
        }
        
        if (Application.isPlaying && playerFollower != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 triggerPos = GetSplinePosition(GetTotalLength() - currentTriggerDistance);
            Gizmos.DrawWireSphere(triggerPos, 2f);
        }
    }
}
