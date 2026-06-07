using UnityEngine;

/// <summary>
/// Attached to decorative background objects on the Menu scene.
/// Makes them bob up/down and rotate slowly for visual interest.
/// </summary>
public class DecoAnimator : MonoBehaviour
{
    private float rotateSpeed;
    private float bobSpeed;
    private float bobAmplitude = 0.3f;
    private Vector3 startPos;
    private float timeOffset;

    public void Init(float speed)
    {
        rotateSpeed = speed * 60f;
        bobSpeed    = speed * 1.5f;
        timeOffset  = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        startPos    = transform.position;
        timeOffset  = timeOffset == 0f ? Random.Range(0f, Mathf.PI * 2f) : timeOffset;
    }

    private void Update()
    {
        // Rotate on random axis
        transform.Rotate(Vector3.up,     rotateSpeed * 0.6f * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right,  rotateSpeed * 0.3f * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.forward,rotateSpeed * 0.2f * Time.deltaTime, Space.World);

        // Bob
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed + timeOffset) * bobAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
