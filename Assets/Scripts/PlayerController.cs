using UnityEngine;

/// <summary>
/// Handles player movement via tap-to-move on Android touch input.
/// Moves the player smoothly toward a target position on the XZ plane.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.15f;

    [Header("Layer Settings")]
    [SerializeField] private LayerMask groundLayerMask;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private Camera mainCamera;
    private Rigidbody rb;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();

        // Start target at current position
        targetPosition = transform.position;
    }

    private void Update()
    {
        HandleTouchInput();
        HandleMouseInput(); // For editor testing
    }

    private void FixedUpdate()
    {
        MoveTowardsTarget();
    }

    /// <summary>
    /// Reads Android touch input and casts a ray to the ground plane.
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // Only on tap begin to avoid dragging weirdness
            if (touch.phase == TouchPhase.Began)
            {
                TrySetTargetFromScreenPoint(touch.position);
            }
        }
    }

    /// <summary>
    /// Mouse input fallback for Unity Editor testing.
    /// Wrapped in try-catch in case the legacy Input module is disabled.
    /// </summary>
    private void HandleMouseInput()
    {
        try
        {
            if (Input.GetMouseButtonDown(0))
            {
                TrySetTargetFromScreenPoint(Input.mousePosition);
            }
        }
        catch (System.InvalidOperationException)
        {
            // Legacy Input not available when new Input System package is active.
            // Touch input (HandleTouchInput) handles all device input in that case.
        }
    }

    /// <summary>
    /// Raycast from camera through screen point to find world position on ground.
    /// </summary>
    private void TrySetTargetFromScreenPoint(Vector2 screenPoint)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);

        // Try hitting the ground layer first
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayerMask))
        {
            SetTarget(hit.point);
        }
        else
        {
            // Fallback: intersect with Y=0 plane
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                SetTarget(ray.GetPoint(distance));
            }
        }
    }

    private void SetTarget(Vector3 worldPosition)
    {
        // Keep player on the same Y level
        targetPosition = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);
        isMoving = true;
    }

    /// <summary>
    /// Smoothly moves the player toward the target using Rigidbody for physics collisions.
    /// </summary>
    private void MoveTowardsTarget()
    {
        if (!isMoving) return;

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance <= stoppingDistance)
        {
            rb.linearVelocity = Vector3.zero;
            isMoving = false;
            return;
        }

        Vector3 velocity = direction.normalized * moveSpeed;
        velocity.y = rb.linearVelocity.y; // preserve gravity
        rb.linearVelocity = velocity;

        // Rotate player to face movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }

    public void StopMovement()
    {
        isMoving = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}
