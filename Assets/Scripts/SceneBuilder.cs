using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Procedurally builds the entire game scene at runtime.
/// Android-safe: no OverlapSphere in Awake, no custom physics layers.
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    [Header("Arena Size")]
    [SerializeField] private float arenaWidth  = 20f;
    [SerializeField] private float arenaHeight = 20f;

    [Header("Collectibles - Initial spawn")]
    [SerializeField] private int commonCount = 3;
    [SerializeField] private int rareCount   = 1;
    [SerializeField] private int epicCount   = 0;

    [Header("Effects")]
    [SerializeField] private GameObject collectParticlePrefab;

    private Material groundMat;
    private Material wallMat;
    private Material obstacleMat;
    private Material playerMat;

    // Obstacle bounds stored for pure-math collision avoidance (no OverlapSphere in Awake)
    private List<Bounds> obstacleBounds = new List<Bounds>();

    private void Awake()
    {
        CreateMaterials();
        BuildArena();
        BuildObstacles();
        BuildPlayer();
        BuildCamera();
        BuildCollectibles();
        BuildLighting();
        BuildUI();
        BuildGameManager();
    }

    // ── Materials ─────────────────────────────────────────────────────────────

    private void CreateMaterials()
    {
        groundMat   = CreateMat(new Color(0.18f, 0.25f, 0.18f));
        wallMat     = CreateMat(new Color(0.4f,  0.4f,  0.45f));
        obstacleMat = CreateMat(new Color(0.55f, 0.35f, 0.2f));
        playerMat   = CreateMat(new Color(0.2f,  0.8f,  0.4f));
    }

    private Material CreateMat(Color color)
    {
        Material mat = new Material(GetLitShader());
        mat.color = color;
        return mat;
    }

    private static Shader GetLitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s != null) return s;
        s = Shader.Find("Standard");
        if (s != null) return s;
        return Shader.Find("Mobile/Diffuse");
    }

    // ── Arena ─────────────────────────────────────────────────────────────────

    private void BuildArena()
    {
        // Ground — name "Ground" so PlayerController raycast can detect it
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position   = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(arenaWidth, 1f, arenaHeight);
        ground.GetComponent<Renderer>().material = groundMat;

        float wt = 1f;   // wall thickness
        float wh = 2f;   // wall height
        float hw = arenaWidth  / 2f;
        float hh = arenaHeight / 2f;
        float wy = wh / 2f - 0.5f;

        CreateWall("Wall_North", new Vector3(0,  wy,  hh + wt/2f), new Vector3(arenaWidth + wt*2f, wh, wt));
        CreateWall("Wall_South", new Vector3(0,  wy, -hh - wt/2f), new Vector3(arenaWidth + wt*2f, wh, wt));
        CreateWall("Wall_East",  new Vector3( hw + wt/2f, wy, 0),   new Vector3(wt, wh, arenaHeight));
        CreateWall("Wall_West",  new Vector3(-hw - wt/2f, wy, 0),   new Vector3(wt, wh, arenaHeight));
    }

    private void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.position   = pos;
        w.transform.localScale = scale;
        w.GetComponent<Renderer>().material = wallMat;
        // Layer 0 (Default) — do NOT assign custom layers at runtime.
        // Custom layers must be pre-registered in Project Settings → Tags & Layers
        // to work in Android builds. BoxCollider from CreatePrimitive handles collision.
    }

    // ── Obstacles ─────────────────────────────────────────────────────────────

    private void BuildObstacles()
    {
        obstacleBounds.Clear();

        // Center cross
        CreateObstacle("Obstacle_Center_H", Vector3.zero, new Vector3(6f, 1.5f, 1f));
        CreateObstacle("Obstacle_Center_V", Vector3.zero, new Vector3(1f, 1.5f, 6f));

        // Corner blocks
        CreateObstacle("Obstacle_NE", new Vector3( 6f, 0f,  6f), new Vector3(2f, 2f, 2f));
        CreateObstacle("Obstacle_NW", new Vector3(-6f, 0f,  6f), new Vector3(2f, 2f, 2f));
        CreateObstacle("Obstacle_SE", new Vector3( 6f, 0f, -6f), new Vector3(2f, 2f, 2f));
        CreateObstacle("Obstacle_SW", new Vector3(-6f, 0f, -6f), new Vector3(2f, 2f, 2f));

        // Dividers
        CreateObstacle("Obstacle_Divider_1", new Vector3( 3f, 0f, -3f), new Vector3(1f, 1.5f, 4f));
        CreateObstacle("Obstacle_Divider_2", new Vector3(-3f, 0f,  3f), new Vector3(1f, 1.5f, 4f));
    }

    private void CreateObstacle(string name, Vector3 pos, Vector3 scale)
    {
        float finalY = pos.y + scale.y / 2f - 0.5f;
        Vector3 finalPos = new Vector3(pos.x, finalY, pos.z);

        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.name = name;
        obs.transform.position   = finalPos;
        obs.transform.localScale = scale;
        obs.GetComponent<Renderer>().material = obstacleMat;

        // Store XZ bounds for pure-math spawn validation (no OverlapSphere needed)
        obstacleBounds.Add(new Bounds(
            new Vector3(pos.x, 0f, pos.z),
            new Vector3(scale.x, 10f, scale.z)));
    }

    // ── Collectibles ──────────────────────────────────────────────────────────

    private void BuildCollectibles()
    {
        List<Vector3> used = new List<Vector3>();
        float hw = arenaWidth  / 2f - 1.5f;
        float hh = arenaHeight / 2f - 1.5f;

        SpawnGroup(Collectible.ItemType.Common, commonCount, used, hw, hh);
        SpawnGroup(Collectible.ItemType.Rare,   rareCount,   used, hw, hh);
        SpawnGroup(Collectible.ItemType.Epic,   epicCount,   used, hw, hh);
    }

    private void SpawnGroup(Collectible.ItemType type, int count,
                            List<Vector3> used, float hw, float hh)
    {
        int spawned = 0, attempts = 0;
        while (spawned < count && attempts < 300)
        {
            attempts++;
            float x = Random.Range(-hw, hw);
            float z = Random.Range(-hh, hh);
            Vector3 pos = new Vector3(x, 0.5f, z);

            if (IsClearMath(pos, used))
            {
                DoSpawnCollectible(type, pos);
                used.Add(pos);
                spawned++;
            }
        }
    }

    /// <summary>
    /// Pure math bounds check — no Physics queries needed.
    /// Works identically in Editor and Android IL2CPP builds.
    /// </summary>
    private bool IsClearMath(Vector3 pos, List<Vector3> used)
    {
        // Too close to existing items
        foreach (var p in used)
            if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(p.x, p.z)) < 2f)
                return false;

        // Inside any obstacle (XZ only, with 1.0 margin)
        foreach (var b in obstacleBounds)
        {
            if (Mathf.Abs(pos.x - b.center.x) < b.extents.x + 1.0f &&
                Mathf.Abs(pos.z - b.center.z) < b.extents.z + 1.0f)
                return false;
        }

        // Too close to arena boundary walls
        float hw = arenaWidth  / 2f - 1.5f;
        float hh = arenaHeight / 2f - 1.5f;
        if (Mathf.Abs(pos.x) > hw || Mathf.Abs(pos.z) > hh) return false;

        return true;
    }

    private void DoSpawnCollectible(Collectible.ItemType type, Vector3 pos)
    {
        PrimitiveType prim = (type == Collectible.ItemType.Common)
            ? PrimitiveType.Sphere : PrimitiveType.Cube;

        GameObject obj = GameObject.CreatePrimitive(prim);
        obj.name = $"Collectible_{type}";
        obj.transform.position = pos;
        obj.GetComponent<Collider>().isTrigger = true;

        Collectible c = obj.AddComponent<Collectible>();

        var field = typeof(Collectible).GetField("itemType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(c, type);

        if (collectParticlePrefab != null)
            c.SetParticlePrefab(collectParticlePrefab);
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private void BuildPlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag  = "Player";
        // Spawn in a corridor that is clear of all obstacles
        player.transform.position = new Vector3(-4f, 0.5f, -4f);
        player.GetComponent<Renderer>().material = playerMat;

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;
        // Continuous sweep prevents tunneling through walls on Android
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        player.AddComponent<PlayerController>();

        // Direction arrow
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.name = "Arrow";
        Destroy(arrow.GetComponent<Collider>());
        arrow.transform.SetParent(player.transform);
        arrow.transform.localPosition = new Vector3(0f, 0.6f, 0.4f);
        arrow.transform.localScale    = new Vector3(0.15f, 0.15f, 0.4f);
        arrow.GetComponent<Renderer>().material = CreateMat(new Color(0.1f, 0.5f, 0.2f));
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void BuildCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";
        }

        cam.backgroundColor = new Color(0.1f, 0.12f, 0.15f);
        cam.clearFlags      = CameraClearFlags.SolidColor;

        // Do NOT set camera position here — CameraFollow.Start() computes
        // the correct offset from the player's actual position.
        if (cam.gameObject.GetComponent<CameraFollow>() == null)
            cam.gameObject.AddComponent<CameraFollow>();
    }

    // ── Lighting ──────────────────────────────────────────────────────────────

    private void BuildLighting()
    {
        GameObject lightObj = new GameObject("Directional Light");
        Light l = lightObj.AddComponent<Light>();
        l.type      = LightType.Directional;
        l.intensity = 1.2f;
        l.color     = new Color(1f, 0.95f, 0.85f);
        l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.3f);
    }

    // ── GameManager ───────────────────────────────────────────────────────────

    private void BuildGameManager()
    {
        GameObject gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();

        GameObject spawnerObj = new GameObject("ItemSpawner");
        ItemSpawner spawner = spawnerObj.AddComponent<ItemSpawner>();

        // Pass obstacle bounds to spawner so it can also use pure-math checks
        spawner.SetObstacleBounds(obstacleBounds);

        SetPrivateField(spawner, "collectParticlePrefab", collectParticlePrefab);
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
    {
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        // Ganti StandaloneInputModule → InputSystemUIInputModule
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        TMP_Text scoreText = CreateTMPText(canvasObj, "ScoreText",
            new Vector2(20, -30), new Vector2(400, 60),
            TextAnchor.UpperLeft, 40, Color.white, "Score: 0");
        scoreText.rectTransform.anchorMin = new Vector2(0, 1);
        scoreText.rectTransform.anchorMax = new Vector2(0, 1);
        scoreText.rectTransform.pivot     = new Vector2(0, 1);

        TMP_Text timerText = CreateTMPText(canvasObj, "TimerText",
            new Vector2(-20, -30), new Vector2(300, 60),
            TextAnchor.UpperRight, 40, Color.white, "Time: 60");
        timerText.rectTransform.anchorMin = new Vector2(1, 1);
        timerText.rectTransform.anchorMax = new Vector2(1, 1);
        timerText.rectTransform.pivot     = new Vector2(1, 1);

        TMP_Text highScoreText = CreateTMPText(canvasObj, "HighScoreText",
            new Vector2(0, -90), new Vector2(400, 50),
            TextAnchor.UpperCenter, 28, new Color(1f, 0.85f, 0.3f), "Best: 0");
        highScoreText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        highScoreText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        highScoreText.rectTransform.pivot     = new Vector2(0.5f, 1f);

        TMP_Text legendText = CreateTMPText(canvasObj, "LegendText",
            new Vector2(0, 20), new Vector2(700, 50),
            TextAnchor.LowerCenter, 22, new Color(0.9f, 0.9f, 0.9f, 0.7f), "");
        legendText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        legendText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        legendText.rectTransform.pivot     = new Vector2(0.5f, 0f);

        TMP_Text popupText = CreateTMPText(canvasObj, "PopupText",
            new Vector2(0, 80), new Vector2(300, 100),
            TextAnchor.MiddleCenter, 72, Color.yellow, "+1");
        popupText.fontStyle = FontStyles.Bold;
        popupText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        popupText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        popupText.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        popupText.gameObject.SetActive(false);

        // Game Over Panel
        GameObject goPanel = new GameObject("GameOverPanel");
        goPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRT = goPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        Image panelBG = goPanel.AddComponent<Image>();
        panelBG.color = new Color(0f, 0f, 0f, 0.75f);

        TMP_Text goTitle = CreateTMPText(goPanel, "GOTitle",
            new Vector2(0, 200), new Vector2(700, 120),
            TextAnchor.MiddleCenter, 80, Color.white, "GAME OVER");
        goTitle.fontStyle = FontStyles.Bold;
        goTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        goTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        goTitle.rectTransform.pivot     = new Vector2(0.5f, 0.5f);

        TMP_Text goScore = CreateTMPText(goPanel, "GOScore",
            new Vector2(0, 60), new Vector2(600, 90),
            TextAnchor.MiddleCenter, 56, new Color(1f, 0.85f, 0f), "Final Score\n0");
        goScore.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        goScore.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        goScore.rectTransform.pivot     = new Vector2(0.5f, 0.5f);

        TMP_Text goHighScore = CreateTMPText(goPanel, "GOHighScore",
            new Vector2(0, -60), new Vector2(600, 70),
            TextAnchor.MiddleCenter, 40, new Color(0.9f, 0.7f, 0.2f), "Best: 0");
        goHighScore.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        goHighScore.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        goHighScore.rectTransform.pivot     = new Vector2(0.5f, 0.5f);

        Button btn     = CreateButton(goPanel, "PlayAgainButton",
            new Vector2(0, -160), new Vector2(340, 90),
            new Color(0.2f, 0.7f, 0.3f), "▶  PLAY AGAIN", 42);

        Button menuBtn = CreateButton(goPanel, "MenuButton",
            new Vector2(0, -270), new Vector2(340, 75),
            new Color(0.18f, 0.22f, 0.3f), "🏠  MAIN MENU", 36);

        goPanel.SetActive(false);

        GameObject uiMgrObj = new GameObject("UIManager");
        UIManager uiMgr = uiMgrObj.AddComponent<UIManager>();
        SetPrivateField(uiMgr, "scoreText",           scoreText);
        SetPrivateField(uiMgr, "timerText",           timerText);
        SetPrivateField(uiMgr, "highScoreText",       highScoreText);
        SetPrivateField(uiMgr, "collectionPopupText", popupText);
        SetPrivateField(uiMgr, "gameOverPanel",       goPanel);
        SetPrivateField(uiMgr, "finalScoreText",      goScore);
        SetPrivateField(uiMgr, "finalHighScoreText",  goHighScore);
        SetPrivateField(uiMgr, "restartButton",       btn);
        SetPrivateField(uiMgr, "menuButton",          menuBtn);
        SetPrivateField(uiMgr, "legendText",          legendText);
        
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Button CreateButton(GameObject parent, string name,
        Vector2 anchoredPos, Vector2 size, Color color, string label, float fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        Button btn = obj.AddComponent<Button>();
        TMP_Text lbl = CreateTMPText(obj, "Label", Vector2.zero, size,
            TextAnchor.MiddleCenter, fontSize, Color.white, label);
        lbl.fontStyle = FontStyles.Bold;
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = Vector2.zero;
        lbl.rectTransform.offsetMax = Vector2.zero;
        return btn;
    }

    private TMP_Text CreateTMPText(GameObject parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta, TextAnchor anchor,
        float fontSize, Color color, string text)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = ConvertAnchor(anchor);
        return tmp;
    }

    private TextAlignmentOptions ConvertAnchor(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:    return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperRight:   return TextAlignmentOptions.TopRight;
            case TextAnchor.UpperCenter:  return TextAlignmentOptions.Top;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.LowerCenter:  return TextAlignmentOptions.Bottom;
            default:                      return TextAlignmentOptions.Left;
        }
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
}
