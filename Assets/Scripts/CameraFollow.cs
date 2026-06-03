using UnityEngine;

/// <summary>
/// Top-down camera that smoothly follows the player.
/// Attach to the Main Camera. Assign playerTransform in Inspector or auto-finds by tag.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTransform;

    [Header("Camera Settings")]
    [SerializeField] private float height    = 12f;
    [SerializeField] private float tiltAngle = 55f;   // degrees from horizontal (55=top-down-ish)
    [SerializeField] private float smoothSpeed = 6f;

    private Vector3 offset;

    private void Start()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("CameraFollow: No player found!");
            return;
        }

        // Calculate offset so camera sits above+behind the player at correct angle
        float backDistance = height / Mathf.Tan(tiltAngle * Mathf.Deg2Rad);
        offset = new Vector3(0f, height, -backDistance);

        // Snap to initial position
        transform.position = playerTransform.position + offset;
        transform.LookAt(playerTransform.position);
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        Vector3 desiredPos = playerTransform.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(playerTransform.position);
    }
}
