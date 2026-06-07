using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns collectibles during gameplay.
/// Uses pure-math bounds check (same as SceneBuilder) — no OverlapSphere layer dependency.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval    = 3f;
    [SerializeField] private int   targetItemCount  = 5;
    [SerializeField] private float minDistFromPlayer = 5f;
    [SerializeField] private float arenaHalfW       = 8.5f;
    [SerializeField] private float arenaHalfH       = 8.5f;

    [Header("Item Weights")]
    [SerializeField] private int weightCommon = 6;
    [SerializeField] private int weightRare   = 3;
    [SerializeField] private int weightEpic   = 1;

    [Header("Effects")]
    [SerializeField] private GameObject collectParticlePrefab;

    private Transform playerTransform;
    private List<Bounds> obstacleBounds = new List<Bounds>();
    private System.Reflection.FieldInfo itemTypeField;

    // Called by SceneBuilder to pass obstacle bounds
    public void SetObstacleBounds(List<Bounds> bounds)
    {
        obstacleBounds = bounds;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        itemTypeField = typeof(Collectible).GetField("itemType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.GameActive)
            {
                int alive   = FindObjectsByType<Collectible>(FindObjectsSortMode.None).Length;
                int toSpawn = targetItemCount - alive;
                for (int i = 0; i < toSpawn; i++)
                {
                    TrySpawnItem();
                    yield return new WaitForSeconds(0.3f);
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void TrySpawnItem()
    {
        Vector3 pos;
        if (FindSpawnPosition(out pos))
            SpawnAt(PickRandomType(), pos);
    }

    private bool FindSpawnPosition(out Vector3 result)
    {
        result = Vector3.zero;
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            float x = Random.Range(-arenaHalfW, arenaHalfW);
            float z = Random.Range(-arenaHalfH, arenaHalfH);
            Vector3 candidate = new Vector3(x, 0.5f, z);

            if (IsClearMath(candidate, playerPos))
            {
                result = candidate;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Pure math bounds check — identical behavior in Editor and Android IL2CPP.
    /// No OverlapSphere, no layer dependency.
    /// </summary>
    private bool IsClearMath(Vector3 pos, Vector3 playerPos)
    {
        // Min distance from player
        if (Vector2.Distance(new Vector2(pos.x, pos.z),
                             new Vector2(playerPos.x, playerPos.z)) < minDistFromPlayer)
            return false;

        // Inside obstacle bounds (with margin)
        foreach (var b in obstacleBounds)
        {
            if (Mathf.Abs(pos.x - b.center.x) < b.extents.x + 1.0f &&
                Mathf.Abs(pos.z - b.center.z) < b.extents.z + 1.0f)
                return false;
        }

        // Near arena edge
        if (Mathf.Abs(pos.x) > arenaHalfW - 1f || Mathf.Abs(pos.z) > arenaHalfH - 1f)
            return false;

        // Existing collectibles
        Collectible[] existing = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        foreach (var c in existing)
        {
            if (Vector2.Distance(new Vector2(pos.x, pos.z),
                                 new Vector2(c.transform.position.x, c.transform.position.z)) < 1.5f)
                return false;
        }

        return true;
    }

    private Collectible.ItemType PickRandomType()
    {
        int total = weightCommon + weightRare + weightEpic;
        int roll  = Random.Range(0, total);
        if (roll < weightCommon)              return Collectible.ItemType.Common;
        if (roll < weightCommon + weightRare) return Collectible.ItemType.Rare;
        return Collectible.ItemType.Epic;
    }

    private void SpawnAt(Collectible.ItemType type, Vector3 pos)
    {
        PrimitiveType prim = type == Collectible.ItemType.Common
            ? PrimitiveType.Sphere : PrimitiveType.Cube;

        GameObject obj = GameObject.CreatePrimitive(prim);
        obj.name = $"Collectible_{type}";
        obj.transform.position = pos;
        obj.GetComponent<Collider>().isTrigger = true;

        Collectible c = obj.AddComponent<Collectible>();
        itemTypeField?.SetValue(c, type);

        if (collectParticlePrefab != null)
            c.SetParticlePrefab(collectParticlePrefab);

        obj.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleIn(obj.transform, 0.35f));
    }

    private IEnumerator ScaleIn(Transform t, float duration)
    {
        yield return null;
        if (t == null) yield break;
        yield return null;
        if (t == null) yield break;

        Vector3 target = t.localScale == Vector3.zero ? Vector3.one * 0.5f : t.localScale;
        t.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            t.localScale = target * Mathf.Sin((elapsed / duration) * Mathf.PI * 0.5f);
            yield return null;
        }
        if (t != null) t.localScale = target;
    }
}
