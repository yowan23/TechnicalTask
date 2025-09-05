using UnityEngine;

public class BallRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationMultiplier = 1f; //spin amount

    private Transform playerTransform;
    private Vector3 lastPlayerPos;

    void Start()
    {
        // Get parent (Player) transform
        playerTransform = transform.parent;
        if (playerTransform == null)
        {
            Debug.LogError("BallRotator: Ball must be child of Player!");
            enabled = false;
            return;
        }

        lastPlayerPos = playerTransform.position;
    }

    void Update()
    {
        Vector3 moveDelta = playerTransform.position - lastPlayerPos;
        float distance = moveDelta.magnitude;

        if (distance > 0.0001f)
        {
            // Ball
            float radius = transform.localScale.x * 0.5f;

            // Rotation angle
            float rotationAngle = (distance / radius) * Mathf.Rad2Deg * rotationMultiplier;

            // Note : (Axis = perpendicular to up & movement)
            Vector3 rollAxis = Vector3.Cross(Vector3.up, moveDelta.normalized);

            transform.Rotate(rollAxis, rotationAngle, Space.World);
        }

        lastPlayerPos = playerTransform.position;
    }
}
