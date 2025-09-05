using UnityEngine;
using UnityEngine.Splines;

public class BallSplineFollower : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private KeyCode forwardKey = KeyCode.W;
    
    [Header("Transition")]
    [SerializeField] private float triggerThreshold = 0.25f; // Trigger at 25% of spline
    
    private SplineContainer currentSpline;
    private float splineT = 0f;
    private EndlessRoadSpawner roadSpawner;
    private bool hasTriggeredSpawn = false;
    
    void Start()
    {
        roadSpawner = FindObjectOfType<EndlessRoadSpawner>();
        if (roadSpawner == null)
        {
            Debug.LogError("EndlessRoadSpawner not found in scene!");
        }
    }
    
    void Update()
    {
        if (currentSpline == null) return;
        
        // press w 
        if (Input.GetKey(forwardKey))
        {
            
            float splineLength = currentSpline.CalculateLength();
            float moveAmount = (moveSpeed / splineLength) * Time.deltaTime;
            splineT += moveAmount;
            
            // Check if we should spawn next platform (at 25% progress)
            if (!hasTriggeredSpawn && splineT >= triggerThreshold)
            {
                hasTriggeredSpawn = true;
                if (roadSpawner != null)
                {
                    Debug.Log($"Triggering spawn at {splineT * 100f:F1}% of spline");
                    roadSpawner.SpawnNextPlatform();
                }
            }
            
            // Keep moving until end of spline
            if (splineT >= 1f)
            {
                // Wait for next spline to be assigned
                splineT = 0.99f; 
            }
            UpdateTransform();
        }
    }
    
    void UpdateTransform()
    {
        if (currentSpline == null) return;
        
        
        Vector3 pos = currentSpline.EvaluatePosition(splineT);
        transform.position = pos;
        
        
        Vector3 tangent = currentSpline.EvaluateTangent(splineT);
        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
        }
    }
    
    public void TransitionToSpline(SplineContainer newSpline)
    {
        if (newSpline == null) return;
        
        
        Vector3 currentWorldPos = transform.position;
        
        
        currentSpline = newSpline;
       
        float closestT = 0f;
        float minDist = float.MaxValue;
        
        // Sample points to find closest
        for (float t = 0f; t <= 0.1f; t += 0.01f)
        {
            Vector3 samplePos = newSpline.EvaluatePosition(t);
            float dist = Vector3.Distance(currentWorldPos, samplePos);
            if (dist < minDist)
            {
                minDist = dist;
                closestT = t;
            }
        }
        
        // Start from beginning of new spline
        splineT = closestT;
        hasTriggeredSpawn = false;
        
        
        UpdateTransform();
        
        Debug.Log($"Transitioned to spline: {newSpline.name} at t={closestT:F3}");
    }
    
    public void SetSpline(SplineContainer spline)
    {
        currentSpline = spline;
        splineT = 0f;
        hasTriggeredSpawn = false;
        UpdateTransform();
        Debug.Log($"Ball set to spline: {spline.name}");
    }
    
    public float GetProgress()
    {
        return splineT;
    }
    
    public bool IsNearEnd()
    {
        return splineT >= 0.9f;
    }
    
    public SplineContainer GetCurrentSpline()
    {
        return currentSpline;
    }
}