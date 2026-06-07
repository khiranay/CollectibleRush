using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns collectible items periodically during gameplay.
/// Items always spawn at minimum distance from the player.
/// Keeps a target count of items alive in the scene at all times.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval   = 3f;    // seconds between spawn attempts
    [SerializeField] private int   targetItemCount  = 5;    // try to keep this many items alive
    [SerializeField] private float minDistFromPlayer = 6f;  // minimum distance from player
    [SerializeField] private float arenaHalfW       = 8.5f; // half arena width minus wall
    [SerializeField] private float arenaHalfH       = 8.5f;

    [Header("Item Weights (relative chance)")]
    [SerializeField] private int weightCommon = 6;
    [SerializeField] private int weightRare   = 3;
    [SerializeField] private int weightEpic   = 1;

    private Transform   playerTransform;
    private float       spawnTimer = 0f;
    private List<Vector3> reusedList = new List<Vector3>();

    // Reflection field info cached once
    private System.Reflection.FieldInfo itemTypeField;

    private void Start()
    {
        // Find player
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        // Cache reflection field
        itemTypeField = typeof(Collectible).GetField("itemType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Start spawn loop
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        // Small initial delay so scene finishes building
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            // Only spawn while game is active
            if (GameManager.Instance != null && GameManager.Instance.GameActive)
            {
                int alive = CountAliveItems();
                int toSpawn = targetItemCount - alive;

                for (int i = 0; i < toSpawn; i++)
                {
                    TrySpawnItem();
                    // Small gap between each spawn so they don't pile up
                    yield return new WaitForSeconds(0.3f);
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private int CountAliveItems()
    {
        // Count active collectibles in scene
        Collectible[] items = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        return items.Length;
    }

    private void TrySpawnItem()
    {
        Vector3 spawnPos;
        if (!FindSpawnPosition(out spawnPos)) return;

        Collectible.ItemType type = PickRandomType();
        SpawnAt(type, spawnPos);
    }

    /// <summary>
    /// Find a valid spawn position: far from player, not inside obstacles.
    /// </summary>
    private bool FindSpawnPosition(out Vector3 result)
    {
        result = Vector3.zero;

        Vector3 playerPos = playerTransform != null
            ? playerTransform.position
            : Vector3.zero;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float x = Random.Range(-arenaHalfW, arenaHalfW);
            float z = Random.Range(-arenaHalfH, arenaHalfH);
            Vector3 candidate = new Vector3(x, 0.5f, z);

            // Must be far enough from player
            float distToPlayer = Vector2.Distance(
                new Vector2(candidate.x, candidate.z),
                new Vector2(playerPos.x,  playerPos.z));
            if (distToPlayer < minDistFromPlayer) continue;

            // Must not overlap obstacles (layer 9) or other items
            Collider[] hits = Physics.OverlapSphere(candidate, 1.0f);
            bool blocked = false;
            foreach (var h in hits)
            {
                if (h.gameObject.layer == 9) { blocked = true; break; }
                if (h.GetComponent<Collectible>() != null) { blocked = true; break; }
            }
            if (blocked) continue;

            // Avoid dead-center obstacle cluster
            if (Mathf.Abs(candidate.x) < 3f && Mathf.Abs(candidate.z) < 3f) continue;

            result = candidate;
            return true;
        }

        return false; // no valid spot found this attempt
    }

    /// <summary>
    /// Weighted random pick among Common / Rare / Epic.
    /// </summary>
    private Collectible.ItemType PickRandomType()
    {
        int total = weightCommon + weightRare + weightEpic;
        int roll  = Random.Range(0, total);

        if (roll < weightCommon)             return Collectible.ItemType.Common;
        if (roll < weightCommon + weightRare) return Collectible.ItemType.Rare;
        return Collectible.ItemType.Epic;
    }

    private void SpawnAt(Collectible.ItemType type, Vector3 pos)
    {
        PrimitiveType prim = (type == Collectible.ItemType.Common)
            ? PrimitiveType.Sphere : PrimitiveType.Cube;

        GameObject obj = GameObject.CreatePrimitive(prim);
        obj.name = $"Collectible_{type}";
        obj.transform.position = pos;

        // Make collider a trigger
        obj.GetComponent<Collider>().isTrigger = true;

        // Add and configure Collectible
        Collectible c = obj.AddComponent<Collectible>();
        itemTypeField?.SetValue(c, type);

        // Spawn animation: scale from 0 → normal
        obj.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleIn(obj.transform, 0.35f));
    }

    /// <summary>
    /// Animate item popping into existence.
    /// </summary>
    private IEnumerator ScaleIn(Transform t, float duration)
    {
        // Target scale is set by Collectible.ApplyVisualStyle() in Start()
        // Wait one frame for Start() to run first
        yield return null;
        if (t == null) yield break;

        Vector3 targetScale = t.localScale == Vector3.zero
            ? Vector3.one * 0.5f   // fallback if Collectible hasn't run yet
            : t.localScale;

        // Re-read after Collectible.Start() ran
        yield return null;
        if (t == null) yield break;
        targetScale = t.localScale;
        if (targetScale == Vector3.zero) targetScale = Vector3.one * 0.5f;

        t.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            // Overshoot spring feel
            float s = Mathf.Sin(progress * Mathf.PI * 0.5f);
            t.localScale = targetScale * s;
            yield return null;
        }
        if (t != null) t.localScale = targetScale;
    }
}
