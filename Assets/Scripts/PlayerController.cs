using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed        = 15f;
    [SerializeField] private float stoppingDistance = 0.15f;
    [SerializeField] private float rotateSpeed      = 720f;
    
private float arenaWidth  = 25f;
private float arenaHeight = 25f;

    private Vector3   targetPosition;
    private bool      isMoving = false;
    private Camera    mainCamera;
    private Rigidbody rb;
    private float     lockedY;

    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private void Awake()
    {
        mainCamera     = Camera.main;
        rb             = GetComponent<Rigidbody>();
        targetPosition = transform.position;
        lockedY        = transform.position.y;

        // Must be set here (not in SceneBuilder) because Awake order is guaranteed
        // to run before Start, ensuring these are set before first FixedUpdate.
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;
    }

    private void Update()
    {
        ReadInput();
        RotateTowardsTarget();
    }

    private void FixedUpdate()
    {
        MoveTowardsTarget();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

private void ReadInput()
{
    var mouse = Mouse.current;
    if (mouse != null && mouse.leftButton.wasPressedThisFrame)
    {
        TrySetTarget(mouse.position.ReadValue());
        return;
    }

    var ts = Touchscreen.current;
    if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
    {
        TrySetTarget(ts.primaryTouch.position.ReadValue());
    }
}

    // ── Raycast ───────────────────────────────────────────────────────────────

    private void TrySetTarget(Vector2 screenPos)
{
    if (mainCamera == null) { mainCamera = Camera.main; if (mainCamera == null) return; }

    Ray ray = mainCamera.ScreenPointToRay(screenPos);
    int count = Physics.RaycastNonAlloc(ray, hitBuffer, 300f);

    float   bestDist = float.MaxValue;
    Vector3 bestPt   = Vector3.zero;
    bool    found    = false;

    for (int i = 0; i < count; i++)
    {
        var hit = hitBuffer[i];
        if (hit.collider.gameObject == gameObject) continue;
        if (hit.collider.isTrigger) continue;
        if (hit.collider.gameObject.name != "Ground") continue;

        if (hit.distance < bestDist)
        {
            bestDist = hit.distance;
            bestPt   = hit.point;
            found    = true;
        }
    }

    // ✅ Clamp wajib di KEDUA path — raycast maupun fallback
    float hw = arenaWidth  / 2f - 1.2f;
    float hh = arenaHeight / 2f - 1.2f;

    if (found)
    {
        Vector3 clamped = new Vector3(
            Mathf.Clamp(bestPt.x, -hw, hw),
            lockedY,
            Mathf.Clamp(bestPt.z, -hh, hh));
        SetTarget(clamped);
        return;
    }

    // Fallback plane — JUGA di-clamp
    Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, lockedY, 0f));
    if (groundPlane.Raycast(ray, out float dist))
    {
        Vector3 pt = ray.GetPoint(dist);
        SetTarget(new Vector3(
            Mathf.Clamp(pt.x, -hw, hw),
            lockedY,
            Mathf.Clamp(pt.z, -hh, hh)));
    }
}

    private void SetTarget(Vector3 pos)
    {
        targetPosition = pos;
        isMoving       = true;
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void MoveTowardsTarget()
    {
        if (!isMoving)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;

        if (dir.magnitude <= stoppingDistance)
        {
            rb.linearVelocity = Vector3.zero;
            isMoving = false;
            return;
        }

        // Set velocity — Continuous collision detection (set in Awake) sweeps
        // the collider each frame so walls and obstacles block correctly on Android.
        rb.linearVelocity = new Vector3(
            dir.normalized.x * moveSpeed,
            0f,
            dir.normalized.z * moveSpeed);
    }

    private void RotateTowardsTarget()
    {
        if (!isMoving) return;
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(dir),
            rotateSpeed * Time.deltaTime);
    }

    public void StopMovement()
    {
        isMoving = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}