using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Procedurally builds the entire game scene at runtime:
/// ground plane, boundary walls, obstacle walls, collectibles, player, camera.
/// No external assets required — primitive shapes only.
/// Attach to an empty GameObject named "SceneBuilder" in a blank scene.
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    [Header("Arena Size")]
    [SerializeField] private float arenaWidth  = 20f;
    [SerializeField] private float arenaHeight = 20f;

    [Header("Collectibles")]
    [SerializeField] private int commonCount = 6;
    [SerializeField] private int rareCount   = 3;
    [SerializeField] private int epicCount   = 1;

    // Materials (created at runtime)
    private Material groundMat;
    private Material wallMat;
    private Material obstacleMat;
    private Material playerMat;

    private void Awake()
    {
        CreateMaterials();
        BuildArena();
        BuildObstacles();
        BuildCollectibles();
        BuildPlayer();
        BuildCamera();
        BuildLighting();
        BuildUI();
        BuildGameManager();
    }

    // ── Materials ──────────────────────────────────────────────────────────────

    private void CreateMaterials()
    {
        groundMat   = CreateMat(new Color(0.18f, 0.25f, 0.18f));   // Dark green
        wallMat     = CreateMat(new Color(0.4f,  0.4f,  0.45f));   // Gray
        obstacleMat = CreateMat(new Color(0.55f, 0.35f, 0.2f));    // Brown
        playerMat   = CreateMat(new Color(0.2f,  0.8f,  0.4f));    // Bright green
    }

    private Material CreateMat(Color color, bool metallic = false)
    {
        Material mat = new Material(GetLitShader());
        mat.color = color;
        if (metallic)
        {
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.8f);
            mat.SetFloat("_Glossiness", 0.8f);
        }
        return mat;
    }

    /// <summary>
    /// Returns the correct lit shader for the active render pipeline.
    /// URP: "Universal Render Pipeline/Lit"
    /// Built-in: "Standard"
    /// Fallback: "Sprites/Default" (always available, flat color)
    /// </summary>
    private static Shader GetLitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s != null) return s;
        s = Shader.Find("Standard");
        if (s != null) return s;
        return Shader.Find("Sprites/Default");
    }

    // ── Arena ─────────────────────────────────────────────────────────────────

    private void BuildArena()
    {
        // Ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(arenaWidth, 1f, arenaHeight);
        ground.GetComponent<Renderer>().material = groundMat;
        // Keep ground on Default layer (layer 0) so raycast mask works
        ground.layer = 0;

        // Add grid texture look via tiled material (shader supports it)
        // Boundary walls (4 sides)
        float wallThick = 1f;
        float wallHeight = 2f;

        CreateWall("Wall_North", new Vector3(0, wallHeight / 2f - 0.5f,  arenaHeight / 2f + wallThick / 2f),
                   new Vector3(arenaWidth + wallThick * 2f, wallHeight, wallThick));
        CreateWall("Wall_South", new Vector3(0, wallHeight / 2f - 0.5f, -arenaHeight / 2f - wallThick / 2f),
                   new Vector3(arenaWidth + wallThick * 2f, wallHeight, wallThick));
        CreateWall("Wall_East",  new Vector3( arenaWidth / 2f + wallThick / 2f, wallHeight / 2f - 0.5f, 0),
                   new Vector3(wallThick, wallHeight, arenaHeight));
        CreateWall("Wall_West",  new Vector3(-arenaWidth / 2f - wallThick / 2f, wallHeight / 2f - 0.5f, 0),
                   new Vector3(wallThick, wallHeight, arenaHeight));
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = wallMat;
    }

    // ── Obstacles ─────────────────────────────────────────────────────────────

    private void BuildObstacles()
    {
        // Cross obstacle in center
        CreateObstacle("Obstacle_Center_H", new Vector3(0, 0, 0),       new Vector3(6f, 1.5f, 1f));
        CreateObstacle("Obstacle_Center_V", new Vector3(0, 0, 0),       new Vector3(1f, 1.5f, 6f));

        // Corner obstacles
        CreateObstacle("Obstacle_NE", new Vector3( 6f, 0,  6f), new Vector3(2f, 2f, 2f));
        CreateObstacle("Obstacle_NW", new Vector3(-6f, 0,  6f), new Vector3(2f, 2f, 2f));
        CreateObstacle("Obstacle_SE", new Vector3( 6f, 0, -6f), new Vector3(2f, 2f, 2f));
        CreateObstacle("Obstacle_SW", new Vector3(-6f, 0, -6f), new Vector3(2f, 2f, 2f));

        // Long wall divider (creates corridors)
        CreateObstacle("Obstacle_Divider_1", new Vector3( 3f, 0, -3f), new Vector3(1f, 1.5f, 4f));
        CreateObstacle("Obstacle_Divider_2", new Vector3(-3f, 0,  3f), new Vector3(1f, 1.5f, 4f));
    }

    private void CreateObstacle(string name, Vector3 pos, Vector3 scale)
    {
        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.name = name;
        obs.transform.position = new Vector3(pos.x, pos.y + scale.y / 2f - 0.5f, pos.z);
        obs.transform.localScale = scale;
        obs.GetComponent<Renderer>().material = obstacleMat;
        // Layer 9 = "Obstacle" (set at runtime without needing Tags registered)
        obs.layer = 9;
    }

    // ── Collectibles ──────────────────────────────────────────────────────────

    private void BuildCollectibles()
    {
        List<Vector3> usedPositions = new List<Vector3>();
        float halfW = arenaWidth  / 2f - 1.5f;
        float halfH = arenaHeight / 2f - 1.5f;

        SpawnCollectibles(Collectible.ItemType.Common, commonCount, usedPositions, halfW, halfH);
        SpawnCollectibles(Collectible.ItemType.Rare,   rareCount,   usedPositions, halfW, halfH);
        SpawnCollectibles(Collectible.ItemType.Epic,   epicCount,   usedPositions, halfW, halfH);
    }

    private void SpawnCollectibles(Collectible.ItemType type, int count,
                                   List<Vector3> used, float halfW, float halfH)
    {
        int attempts = 0;
        int spawned  = 0;

        while (spawned < count && attempts < 200)
        {
            attempts++;
            float x = Random.Range(-halfW, halfW);
            float z = Random.Range(-halfH, halfH);
            Vector3 pos = new Vector3(x, 0.5f, z);

            // Avoid placing inside obstacles or too close to other items
            if (IsClearPosition(pos, used))
            {
                SpawnCollectible(type, pos);
                used.Add(pos);
                spawned++;
            }
        }
    }

    private bool IsClearPosition(Vector3 pos, List<Vector3> used)
    {
        // Check overlap with existing collectibles
        foreach (var p in used)
            if (Vector3.Distance(pos, p) < 2f) return false;

        // Check overlap with obstacles via sphere cast (layer 9 = obstacle layer)
        Collider[] hits = Physics.OverlapSphere(pos, 1.2f);
        foreach (var h in hits)
            if (h.gameObject.layer == 9) return false;

        // Avoid center cross obstacle area
        if (Mathf.Abs(pos.x) < 3.5f && Mathf.Abs(pos.z) < 3.5f) return false;

        return true;
    }

    private void SpawnCollectible(Collectible.ItemType type, Vector3 pos)
    {
        // Common = Sphere, Rare = Cube, Epic = Cube (gem shape via script)
        PrimitiveType prim = (type == Collectible.ItemType.Common)
            ? PrimitiveType.Sphere
            : PrimitiveType.Cube;

        GameObject obj = GameObject.CreatePrimitive(prim);
        obj.name = $"Collectible_{type}";
        obj.transform.position = pos;

        // Replace box collider with trigger
        Collider col = obj.GetComponent<Collider>();
        col.isTrigger = true;

        // Add Collectible script — it sets own color/scale
        Collectible c = obj.AddComponent<Collectible>();

        // Inject type via reflection (field is private serialized)
        var field = typeof(Collectible).GetField("itemType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(c, type);
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private void BuildPlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag  = "Player";
        player.transform.position = new Vector3(-7f, 0.5f, -7f);

        player.GetComponent<Renderer>().material = playerMat;

        // Physics
        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;

        // Collider already present (CapsuleCollider)
        // Add PlayerController
        PlayerController pc = player.AddComponent<PlayerController>();

        // Set ground layer mask — must cast to LayerMask struct, not raw int
        var field = typeof(PlayerController).GetField("groundLayerMask",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        LayerMask groundMask = LayerMask.GetMask("Default");
        field?.SetValue(pc, groundMask);

        // Add directional arrow indicator (child cube pointing forward)
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
        cam.clearFlags = CameraClearFlags.SolidColor;

        CameraFollow cf = cam.gameObject.AddComponent<CameraFollow>();
    }

    // ── Lighting ──────────────────────────────────────────────────────────────

    private void BuildLighting()
    {
        // Directional light
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1.2f;
        light.color     = new Color(1f, 0.95f, 0.85f);
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ambient light
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.3f);
    }

    // ── GameManager ───────────────────────────────────────────────────────────

    private void BuildGameManager()
    {
        GameObject gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Score text
        TMP_Text scoreText = CreateTMPText(canvasObj, "ScoreText",
            new Vector2(20, -30), new Vector2(400, 60),
            TextAnchor.UpperLeft, 40, Color.white, "Score: 0");
        scoreText.rectTransform.anchorMin = new Vector2(0, 1);
        scoreText.rectTransform.anchorMax = new Vector2(0, 1);
        scoreText.rectTransform.pivot     = new Vector2(0, 1);

        // Timer text
        TMP_Text timerText = CreateTMPText(canvasObj, "TimerText",
            new Vector2(-20, -30), new Vector2(300, 60),
            TextAnchor.UpperRight, 40, Color.white, "Time: 60");
        timerText.rectTransform.anchorMin = new Vector2(1, 1);
        timerText.rectTransform.anchorMax = new Vector2(1, 1);
        timerText.rectTransform.pivot     = new Vector2(1, 1);

        // High score text
        TMP_Text highScoreText = CreateTMPText(canvasObj, "HighScoreText",
            new Vector2(0, -90), new Vector2(400, 50),
            TextAnchor.UpperCenter, 28, new Color(1f, 0.85f, 0.3f), "Best: 0");
        highScoreText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        highScoreText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        highScoreText.rectTransform.pivot     = new Vector2(0.5f, 1f);

        // Legend text
        TMP_Text legendText = CreateTMPText(canvasObj, "LegendText",
            new Vector2(0, 20), new Vector2(700, 50),
            TextAnchor.LowerCenter, 22, new Color(0.9f, 0.9f, 0.9f, 0.7f), "");
        legendText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        legendText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        legendText.rectTransform.pivot     = new Vector2(0.5f, 0f);

        // Collection popup (center screen)
        TMP_Text popupText = CreateTMPText(canvasObj, "PopupText",
            new Vector2(0, 80), new Vector2(300, 100),
            TextAnchor.MiddleCenter, 72, Color.yellow, "+1");
        popupText.fontStyle = FontStyles.Bold;
        popupText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        popupText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        popupText.rectTransform.pivot     = new Vector2(0.5f, 0.5f);
        popupText.gameObject.SetActive(false);

        // ── Game Over Panel ──────────────────────────────────────────────────

        GameObject goPanel = new GameObject("GameOverPanel");
        goPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRT = goPanel.AddComponent<RectTransform>();
        panelRT.anchorMin   = Vector2.zero;
        panelRT.anchorMax   = Vector2.one;
        panelRT.offsetMin   = Vector2.zero;
        panelRT.offsetMax   = Vector2.zero;

        // Semi-transparent background
        Image panelBG = goPanel.AddComponent<Image>();
        panelBG.color = new Color(0f, 0f, 0f, 0.75f);

        // "GAME OVER" title
        TMP_Text goTitle = CreateTMPText(goPanel, "GOTitle",
            new Vector2(0, 200), new Vector2(700, 120),
            TextAnchor.MiddleCenter, 80, Color.white, "GAME OVER");
        goTitle.fontStyle = FontStyles.Bold;
        goTitle.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        goTitle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        goTitle.rectTransform.pivot     = new Vector2(0.5f, 0.5f);

        // Final score
        TMP_Text goScore = CreateTMPText(goPanel, "GOScore",
            new Vector2(0, 60), new Vector2(600, 90),
            TextAnchor.MiddleCenter, 56, new Color(1f, 0.85f, 0f), "Final Score\n0");
        goScore.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        goScore.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        goScore.rectTransform.pivot     = new Vector2(0.5f, 0.5f);

        // High score
        TMP_Text goHighScore = CreateTMPText(goPanel, "GOHighScore",
            new Vector2(0, -60), new Vector2(600, 70),
            TextAnchor.MiddleCenter, 40, new Color(0.9f, 0.7f, 0.2f), "Best: 0");
        goHighScore.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        goHighScore.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        goHighScore.rectTransform.pivot     = new Vector2(0.5f, 0.5f);

        // Restart Button
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(goPanel.transform, false);
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.anchorMin    = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax    = new Vector2(0.5f, 0.5f);
        btnRT.pivot        = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0, -180);
        btnRT.sizeDelta    = new Vector2(320, 90);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.3f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.9f, 0.4f);
        cb.pressedColor     = new Color(0.1f, 0.5f, 0.2f);
        btn.colors = cb;

        TMP_Text btnLabel = CreateTMPText(btnObj, "BtnLabel",
            Vector2.zero, new Vector2(320, 90),
            TextAnchor.MiddleCenter, 42, Color.white, "PLAY AGAIN");
        btnLabel.fontStyle = FontStyles.Bold;
        btnLabel.rectTransform.anchorMin = Vector2.zero;
        btnLabel.rectTransform.anchorMax = Vector2.one;
        btnLabel.rectTransform.offsetMin = Vector2.zero;
        btnLabel.rectTransform.offsetMax = Vector2.zero;

        goPanel.SetActive(false);

        // ── Wire UIManager ───────────────────────────────────────────────────

        GameObject uiMgrObj = new GameObject("UIManager");
        UIManager uiMgr = uiMgrObj.AddComponent<UIManager>();

        // Inject references via reflection
        SetPrivateField(uiMgr, "scoreText",            scoreText);
        SetPrivateField(uiMgr, "timerText",            timerText);
        SetPrivateField(uiMgr, "highScoreText",        highScoreText);
        SetPrivateField(uiMgr, "collectionPopupText",  popupText);
        SetPrivateField(uiMgr, "gameOverPanel",        goPanel);
        SetPrivateField(uiMgr, "finalScoreText",       goScore);
        SetPrivateField(uiMgr, "finalHighScoreText",   goHighScore);
        SetPrivateField(uiMgr, "restartButton",        btn);
        SetPrivateField(uiMgr, "legendText",           legendText);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TMP_Text CreateTMPText(GameObject parent, string name,
                                    Vector2 anchoredPos, Vector2 sizeDelta,
                                    TextAnchor anchor, float fontSize,
                                    Color color, string text)
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
