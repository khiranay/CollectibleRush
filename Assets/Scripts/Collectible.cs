using UnityEngine;

/// <summary>
/// Represents a collectible item in the scene.
/// Different item types give different point values.
/// Plays a particle effect and triggers score on collection.
/// </summary>
public class Collectible : MonoBehaviour
{
    public enum ItemType
    {
        Common,     // 1 point  - Yellow sphere
        Rare,       // 3 points - Blue cube
        Epic        // 5 points - Red gem (scaled cube)
    }

    [Header("Item Configuration")]
    [SerializeField] private ItemType itemType = ItemType.Common;

    [Header("Visual")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Effects")]
    [SerializeField] private GameObject collectParticlePrefab;

    private Vector3 startPosition;
    private bool isCollected = false;

    // Point values per type
    public int PointValue
    {
        get
        {
            switch (itemType)
            {
                case ItemType.Common: return 1;
                case ItemType.Rare:   return 3;
                case ItemType.Epic:   return 5;
                default:              return 1;
            }
        }
    }

    public ItemType Type => itemType;

    private void Start()
    {
        startPosition = transform.position;
        ApplyVisualStyle();
    }

    private void Update()
{
    if (isCollected) return;

    // ✅ Gunakan startPosition.x/z — tidak bisa di-drift oleh physics
    float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
    transform.position = new Vector3(startPosition.x, newY, startPosition.z);

    transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
}

    /// <summary>
    /// Applies color and scale based on item type using primitive materials.
    /// </summary>
    private void ApplyVisualStyle()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        Material mat = new Material(GetLitShader());

        switch (itemType)
        {
            case ItemType.Common:
                mat.color = new Color(1f, 0.85f, 0f);
                mat.SetFloat("_Metallic", 0.3f);
                mat.SetFloat("_Smoothness", 0.6f);
                mat.SetFloat("_Glossiness", 0.6f);
                transform.localScale = Vector3.one * 0.5f;
                break;

            case ItemType.Rare:
                mat.color = new Color(0.2f, 0.5f, 1f);
                mat.SetFloat("_Metallic", 0.7f);
                mat.SetFloat("_Smoothness", 0.9f);
                mat.SetFloat("_Glossiness", 0.9f);
                transform.localScale = Vector3.one * 0.6f;
                break;

            case ItemType.Epic:
                mat.color = new Color(1f, 0.2f, 0.2f);
                mat.SetFloat("_Metallic", 0.9f);
                mat.SetFloat("_Smoothness", 1f);
                mat.SetFloat("_Glossiness", 1f);
                // Emission: URP uses _EmissionColor directly; Built-in needs keyword
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.1f) * 0.5f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
                break;
        }

        rend.material = mat;
    }

    /// <summary>
    /// Returns the correct lit shader for the active render pipeline.
    /// Handles cross-platform compatibility for Android/iOS.
    /// </summary>
    private static Shader GetLitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s != null) return s;
        s = Shader.Find("Standard");
        if (s != null) return s;
        return Shader.Find("Mobile/Diffuse");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        // Notify game manager
        GameManager.Instance?.OnItemCollected(this);

        // Spawn particle effect
        if (collectParticlePrefab != null)
        {
            Instantiate(collectParticlePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            // Runtime fallback particle
            SpawnFallbackParticle();
        }

        // Destroy this object
        Destroy(gameObject);
    }
    public void SetParticlePrefab(GameObject prefab)
{
    collectParticlePrefab = prefab;
}

    /// <summary>
    /// Creates a simple runtime particle system if no prefab is assigned.
    /// </summary>
    private void SpawnFallbackParticle()
    {
        GameObject particleObj = new GameObject("CollectParticle");
        particleObj.transform.position = transform.position;

        // AddComponent auto-plays the PS — configure BEFORE it gets a chance to tick.
        // Key: do NOT set main.duration (read-only while playing).
        // Instead, control lifetime purely via startLifetime + Destroy().
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        // duration is intentionally NOT set — leave at default (5s).
        // The GameObject is destroyed after 1.5s anyway, which is all we need.
        main.loop = false;
        main.startLifetime = 0.8f;
        main.startSpeed = 4f;
        main.startSize = 0.25f;
        main.maxParticles = 20;
        main.playOnAwake = false;   // prevent double-play

        // Color based on item type
        switch (itemType)
        {
            case ItemType.Common: main.startColor = new Color(1f, 0.85f, 0f); break;
            case ItemType.Rare:   main.startColor = new Color(0.2f, 0.5f, 1f); break;
            case ItemType.Epic:   main.startColor = new Color(1f, 0.2f, 0.2f); break;
        }

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ps.Play();
        Destroy(particleObj, 1.5f);
    }
}
