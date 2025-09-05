using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;

public class EndlessRoadSpawner : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject[] platformPrefabs;
    [SerializeField] private GameObject player;
    [SerializeField] private int maxActivePlatforms = 3;

    [Header("Connection Settings")]
    [SerializeField] private float connectionGap = 0f; // Gap between platforms
    [SerializeField] private bool debugMode = true;

    private List<GameObject> activePlatforms = new List<GameObject>();
    private BallSplineFollower ballFollower;
    private int lastPrefabIndex = -1; 
    private int platformIndex = 0;
    private bool isSpawning = false;

    void Start()
    {
        ballFollower = player.GetComponent<BallSplineFollower>();

        if (ballFollower == null)
        {
            Debug.LogError("Player must have BallSplineFollower script!");
            return;
        }

        // Spawn first platform (fixed: always first prefab)
        SpawnFirstPlatform();
    }

    void SpawnFirstPlatform()
    {
        if (platformPrefabs.Length == 0) return;

        GameObject firstPlatform = Instantiate(platformPrefabs[0]);
        firstPlatform.transform.position = Vector3.zero;
        firstPlatform.transform.rotation = Quaternion.identity;
        firstPlatform.name = "Platform_0";

        activePlatforms.Add(firstPlatform);

        // Assign spline to ball
        SplineContainer spline = firstPlatform.GetComponentInChildren<SplineContainer>();
        if (spline != null)
        {
            ballFollower.SetSpline(spline);
            Debug.Log("First platform created and assigned to ball");
        }

        lastPrefabIndex = 0; // remember first
        platformIndex++;
    }

    public void SpawnNextPlatform()
    {
        if (isSpawning) return;
        isSpawning = true;

        Debug.Log("=== SPAWNING NEXT PLATFORM ===");

        GameObject currentPlatform = GetCurrentPlatform();
        if (currentPlatform == null)
        {
            Debug.LogError("No current platform found!");
            isSpawning = false;
            return;
        }

        SplineContainer currentSpline = currentPlatform.GetComponentInChildren<SplineContainer>();
        if (currentSpline == null)
        {
            Debug.LogError("Current platform missing SplineContainer!");
            isSpawning = false;
            return;
        }

        // Pick next prefab (randomize)
        int newIndex;
        do
        {
            newIndex = Random.Range(0, platformPrefabs.Length);
        } 
        while (platformPrefabs.Length > 1 && newIndex == lastPrefabIndex);

        lastPrefabIndex = newIndex;
        GameObject prefab = platformPrefabs[newIndex];

        GameObject newPlatform = Instantiate(prefab);
        newPlatform.name = $"Platform_{platformIndex}";

        // Position/rotate new platform using spline knot alignment
        PositionNewPlatform(newPlatform, currentSpline);

        activePlatforms.Add(newPlatform);
        platformIndex++;

        // Get new spline and prepare for transition
        SplineContainer newSpline = newPlatform.GetComponentInChildren<SplineContainer>();
        if (newSpline != null)
        {
            StartCoroutine(TransitionWhenReady(newSpline));
        }

        // Clean up old ones
        CleanupOldPlatforms();

        isSpawning = false;
        Debug.Log($"Platform spawned! Active platforms: {activePlatforms.Count}");
    }

    System.Collections.IEnumerator TransitionWhenReady(SplineContainer newSpline)
    {
        while (ballFollower.GetProgress() < 0.98f)
        {
            yield return null;
        }

        ballFollower.TransitionToSpline(newSpline);
    }

    /// <summary>
    /// Aligns new platform using knot-to-knot connection
    /// </summary>
    void PositionNewPlatform(GameObject newPlatform, SplineContainer currentSpline)
    {
        var current = currentSpline.Spline;
        if (current == null || current.Count < 2)
        {
            Debug.LogError("Invalid current spline data!");
            return;
        }

        // Get last knot of current spline (end point) 
        BezierKnot lastKnot = current[current.Count - 1];
        Vector3 endWorldPos = currentSpline.transform.TransformPoint(lastKnot.Position);
        Vector3 endForward = currentSpline.transform.TransformDirection(lastKnot.TangentOut).normalized;

        // Reset new platform transform
        newPlatform.transform.position = Vector3.zero;
        newPlatform.transform.rotation = Quaternion.identity;

        // Get first knot of new spline (start point)
        SplineContainer newSpline = newPlatform.GetComponentInChildren<SplineContainer>();
        if (newSpline == null)
        {
            Debug.LogError("New platform missing SplineContainer!");
            return;
        }
        BezierKnot firstKnot = newSpline.Spline[0];
        Vector3 startLocalPos = firstKnot.Position;

        // Rotate to align with previous tangent 
        Quaternion targetRotation = Quaternion.LookRotation(endForward, Vector3.up);
        newPlatform.transform.rotation = targetRotation;

        // Get start world pos after rotation
        Vector3 startWorldPos = newPlatform.transform.TransformPoint(startLocalPos);

        // Compute offset so start lines up with end
        Vector3 offset = endWorldPos - startWorldPos;
        offset += endForward * connectionGap; 
        newPlatform.transform.position += offset;

        if (debugMode)
        {
            Debug.DrawLine(endWorldPos, startWorldPos, Color.magenta, 5f);
            Debug.DrawRay(endWorldPos, endForward * 5f, Color.green, 5f);
            Debug.Log($"Connected {newPlatform.name} to {currentSpline.name}");
        }
    }

    GameObject GetCurrentPlatform()
    {
        if (activePlatforms.Count == 0) return null;
        return activePlatforms[activePlatforms.Count - 1];
    }

    void CleanupOldPlatforms()
    {
        while (activePlatforms.Count > maxActivePlatforms)
        {
            GameObject oldPlatform = activePlatforms[0];
            activePlatforms.RemoveAt(0);

            if (oldPlatform != null)
            {
                Debug.Log($"Removing old platform: {oldPlatform.name}");
                Destroy(oldPlatform, 0.5f);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!debugMode) return;

        for (int i = 0; i < activePlatforms.Count - 1; i++)
        {
            if (activePlatforms[i] != null && activePlatforms[i + 1] != null)
            {
                var spline1 = activePlatforms[i].GetComponentInChildren<SplineContainer>();
                var spline2 = activePlatforms[i + 1].GetComponentInChildren<SplineContainer>();

                if (spline1 != null && spline2 != null)
                {
                    Vector3 end = spline1.EvaluatePosition(1f);
                    Vector3 start = spline2.EvaluatePosition(0f);

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(end, 0.5f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(start, 0.5f);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(end, start);
                }
            }
        }
    }
}
