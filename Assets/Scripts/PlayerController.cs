using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed        = 12f;   // was 5 — faster
    [SerializeField] private float stoppingDistance =  0.2f;
    [SerializeField] private float rotateSpeed      = 900f;

    private Vector3   targetPosition;
    private bool      isMoving = false;
    private Camera    mainCamera;
    private Rigidbody rb;
    private float     lockedY;

    // Reusable buffer — avoids alloc every click
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private void Awake()
    {
        mainCamera     = Camera.main;
        rb             = GetComponent<Rigidbody>();
        targetPosition = transform.position;
        lockedY        = transform.position.y;
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
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            { TrySetTarget(mouse.position.ReadValue()); return; }
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
            TrySetTarget(ts.primaryTouch.position.ReadValue());
#else
        if (Input.GetMouseButtonDown(0))
            { TrySetTarget(Input.mousePosition); return; }
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TrySetTarget(Input.GetTouch(0).position);
#endif
    }

    // ── Raycast ───────────────────────────────────────────────────────────────

    private void TrySetTarget(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // Non-alloc raycast — much cheaper than RaycastAll
        int count = Physics.RaycastNonAlloc(ray, hitBuffer, 200f);

        float   bestDist = float.MaxValue;
        Vector3 bestPt   = Vector3.zero;
        bool    found    = false;

        for (int i = 0; i < count; i++)
        {
            var hit = hitBuffer[i];
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.isTrigger) continue;
            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                bestPt   = hit.point;
                found    = true;
            }
        }

        if (found)
        {
            SetTarget(new Vector3(bestPt.x, lockedY, bestPt.z));
            return;
        }

        // Fallback: math intersect with Y=lockedY plane
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, lockedY, 0));
        if (groundPlane.Raycast(ray, out float dist))
        {
            Vector3 pt = ray.GetPoint(dist);
            SetTarget(new Vector3(pt.x, lockedY, pt.z));
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
        // Hard-lock Y
        Vector3 p = transform.position;
        if (Mathf.Abs(p.y - lockedY) > 0.01f)
            rb.MovePosition(new Vector3(p.x, lockedY, p.z));

        if (!isMoving) { rb.linearVelocity = Vector3.zero; return; }

        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;

        if (dir.magnitude <= stoppingDistance)
        {
            rb.linearVelocity = Vector3.zero;
            isMoving = false;
            return;
        }

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
