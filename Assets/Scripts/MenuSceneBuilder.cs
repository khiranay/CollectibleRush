using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Builds the Home Screen scene entirely at runtime.
/// Responsive layout — works on both portrait (phone) and landscape (1920x1080).
/// Scene build index: 0 = MenuScene, 1 = MainScene.
/// </summary>
public class MenuSceneBuilder : MonoBehaviour
{
    private const string HIGH_SCORE_KEY  = "HighScore";
    private const int    GAME_SCENE_INDEX = 1;

    private static readonly Color ColorBG          = new Color(0.06f, 0.08f, 0.12f);
    private static readonly Color ColorCard        = new Color(0.08f, 0.12f, 0.10f, 0.93f);
    private static readonly Color ColorAccent      = new Color(0.2f,  0.85f, 0.45f);
    private static readonly Color ColorAccentDark  = new Color(0.12f, 0.55f, 0.28f);
    private static readonly Color ColorGold        = new Color(1f,    0.82f, 0.2f);
    private static readonly Color ColorGray        = new Color(0.6f,  0.65f, 0.62f);
    private static readonly Color ColorRed         = new Color(0.9f,  0.25f, 0.25f);
    [SerializeField] private Shader litShader;  // Assign "Universal Render Pipeline/Lit" in Inspector


    private bool isTransitioning = false;
    private CanvasGroup canvasGroup;   // cached in BuildUI

    private void Start()
    {
        BuildCamera();
        BuildBackground();
        BuildFloatingItems();
        BuildUI();
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void BuildCamera()
{
    Camera cam = Camera.main;
    if (cam == null) return; // ← tambah ini
    cam.backgroundColor = ColorBG;
    cam.clearFlags      = CameraClearFlags.SolidColor;
    cam.transform.position = new Vector3(0, 6f, -4f);
    cam.transform.rotation = Quaternion.Euler(35f, 0, 0);
    cam.fieldOfView = 60f;
}

    // ── Background ────────────────────────────────────────────────────────────

    private void BuildBackground()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "MenuGround";
        ground.transform.position   = new Vector3(0, -1f, 5f);
        ground.transform.localScale = new Vector3(40f, 0.5f, 30f);
        Destroy(ground.GetComponent<Collider>());
        var mat = new Material(GetLitShader());
        mat.color = new Color(0.1f, 0.14f, 0.1f);
        ground.GetComponent<Renderer>().material = mat;

        // Directional light
        GameObject lightObj = new GameObject("Light");
        Light light = lightObj.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1.1f;
        light.color     = new Color(1f, 0.95f, 0.85f);
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.2f, 0.25f, 0.22f);
    }

    // ── Floating deco items ───────────────────────────────────────────────────

    private void BuildFloatingItems()
    {
        SpawnDeco(PrimitiveType.Sphere, new Vector3(-5f,  1f,  6f), new Color(1f, 0.85f, 0f),    0.6f, 1.2f);
        SpawnDeco(PrimitiveType.Cube,   new Vector3(-3f,  2f,  8f), new Color(0.2f, 0.5f, 1f),   0.7f, 0.8f);
        SpawnDeco(PrimitiveType.Cube,   new Vector3( 4f,  1.5f,7f), new Color(1f, 0.25f, 0.25f), 0.5f, 1.5f);
        SpawnDeco(PrimitiveType.Sphere, new Vector3( 6f,  2f,  5f), new Color(1f, 0.85f, 0f),    0.4f, 2.0f);
        SpawnDeco(PrimitiveType.Cube,   new Vector3(-7f,  1f,  4f), new Color(0.2f, 0.5f, 1f),   0.8f, 0.6f);
        SpawnDeco(PrimitiveType.Sphere, new Vector3( 2f,  3f, 10f), new Color(1f, 0.25f, 0.25f), 0.6f, 1.8f);
        SpawnDeco(PrimitiveType.Sphere, new Vector3( 8f,  1f,  9f), new Color(1f, 0.85f, 0f),    0.7f, 0.9f);
    }

    private void SpawnDeco(PrimitiveType type, Vector3 pos, Color color, float scale, float speed)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = "Deco";
        obj.transform.position   = pos;
        obj.transform.localScale = Vector3.one * scale;
        Destroy(obj.GetComponent<Collider>());
        var mat = new Material(GetLitShader());
        mat.color = color;
        mat.SetFloat("_Metallic",   0.5f);
        mat.SetFloat("_Smoothness", 0.8f);
        mat.SetFloat("_Glossiness", 0.8f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 0.25f);
        obj.GetComponent<Renderer>().material = mat;
        obj.AddComponent<DecoAnimator>().Init(speed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI  — fully responsive, no hardcoded pixel heights
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);  // landscape base
        scaler.matchWidthOrHeight  = 0.5f;                     // blend width+height

        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dark overlay
        MakeStretchPanel(canvasObj, "Overlay", new Color(0f, 0f, 0f, 0.5f));

        // ── Card — centered, max 85% width, 92% height so it always fits ─────
        GameObject card = new GameObject("Card");
        card.transform.SetParent(canvasObj.transform, false);
        RectTransform cardRT = card.AddComponent<RectTransform>();
        // Anchor center, stretch to fill 86% width, 90% height max
        cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRT.pivot            = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta        = new Vector2(760, 940);       // in reference space (1920x1080 → fits)
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = ColorCard;

        // Thin accent border
        GameObject border = MakeStretchPanel(card, "Border", ColorAccent * 0.5f);
        border.GetComponent<RectTransform>().offsetMin = new Vector2(-2, -2);
        border.GetComponent<RectTransform>().offsetMax = new Vector2( 2,  2);
        border.transform.SetAsFirstSibling();

        // ── Layout using anchors inside the card ─────────────────────────────
        // Top zone  : 75%–100% → title block
        // Mid zone  : 35%–72%  → legend rows
        // Score row : 28%–35%
        // Btn zone  : 5%–26%
        // Footer    : 0%–5%

        // COLLECT
        TMP_Text t1 = MakeAnchoredText(card, "TitleCollect",
            new Vector2(0f, 0.78f), new Vector2(1f, 1f), new Vector2(0, -10),
            "COLLECT", 88, ColorAccent, FontStyles.Bold);
        t1.characterSpacing = 10f;
        t1.alignment = TextAlignmentOptions.Center;

        // QUEST
        TMP_Text t2 = MakeAnchoredText(card, "TitleQuest",
            new Vector2(0f, 0.65f), new Vector2(1f, 0.79f), Vector2.zero,
            "QUEST", 60, ColorGold, FontStyles.Bold);
        t2.characterSpacing = 18f;
        t2.alignment = TextAlignmentOptions.Center;

        // Tagline
        TMP_Text tagline = MakeAnchoredText(card, "Tagline",
            new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.65f), Vector2.zero,
            "Tap to move  ·  Collect items  ·  Beat the clock",
            24, ColorGray, FontStyles.Normal);
        tagline.alignment = TextAlignmentOptions.Center;

        // Divider
        GameObject div = MakeStretchPanel(card, "Divider", ColorAccent * 0.35f);
        RectTransform divRT = div.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.08f, 0.565f);
        divRT.anchorMax = new Vector2(0.92f, 0.570f);
        divRT.offsetMin = Vector2.zero;
        divRT.offsetMax = Vector2.zero;

        // ── Legend rows ───────────────────────────────────────────────────────
        MakeLegendRow(card, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.555f),
            new Color(1f, 0.85f, 0f),    "Common",  "+1 pt");
        MakeLegendRow(card, new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.475f),
            new Color(0.3f, 0.55f, 1f),  "Rare",    "+3 pts");
        MakeLegendRow(card, new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.395f),
            new Color(1f, 0.3f, 0.3f),   "Epic",    "+5 pts");

        // Divider 2
        GameObject div2 = MakeStretchPanel(card, "Divider2", ColorAccent * 0.25f);
        RectTransform div2RT = div2.GetComponent<RectTransform>();
        div2RT.anchorMin = new Vector2(0.08f, 0.305f);
        div2RT.anchorMax = new Vector2(0.92f, 0.310f);
        div2RT.offsetMin = Vector2.zero;
        div2RT.offsetMax = Vector2.zero;

        // ── High score ────────────────────────────────────────────────────────
        int hs = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        string hsLabel = hs > 0 ? $"🏆  Best Score:  {hs}" : "No record yet — be the first!";
        TMP_Text hsText = MakeAnchoredText(card, "HighScore",
            new Vector2(0.05f, 0.245f), new Vector2(0.95f, 0.305f), Vector2.zero,
            hsLabel, 30, ColorGold, FontStyles.Normal);
        hsText.alignment = TextAlignmentOptions.Center;

        // ── PLAY button ───────────────────────────────────────────────────────
        GameObject playBtn = MakeButtonAnchor(card, "PlayBtn",
            new Vector2(0.1f, 0.115f), new Vector2(0.9f, 0.235f),
            ColorAccent, ColorAccentDark,
            "▶   PLAY", 52, Color.white);
        playBtn.GetComponent<Button>().onClick.AddListener(() =>
{
    UnityEngine.SceneManagement.SceneManager.LoadScene(1);
});

        // ── Footer: hint + timer badge ────────────────────────────────────────
        TMP_Text hint = MakeAnchoredText(card, "Hint",
            new Vector2(0.05f, 0.055f), new Vector2(0.72f, 0.108f), Vector2.zero,
            "Tap ground to move player", 22, ColorGray, FontStyles.Normal);
        hint.alignment = TextAlignmentOptions.Left;

        // Timer badge (right side of footer)
        GameObject badge = MakeStretchPanel(card, "TimerBadge", new Color(0.75f, 0.18f, 0.18f, 0.9f));
        RectTransform badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.72f, 0.045f);
        badgeRT.anchorMax = new Vector2(0.94f, 0.112f);
        badgeRT.offsetMin = Vector2.zero;
        badgeRT.offsetMax = Vector2.zero;
        TMP_Text badgeLbl = MakeAnchoredText(badge, "BadgeLabel",
            Vector2.zero, Vector2.one, Vector2.zero,
            "⏱ 60 SEC", 24, Color.white, FontStyles.Bold);
        badgeLbl.alignment = TextAlignmentOptions.Center;

        // ── Bottom version text (outside card) ────────────────────────────────
        GameObject verObj = new GameObject("Version");
        verObj.transform.SetParent(canvasObj.transform, false);
        RectTransform verRT = verObj.AddComponent<RectTransform>();
        verRT.anchorMin        = new Vector2(0f, 0f);
        verRT.anchorMax        = new Vector2(1f, 0.04f);
        verRT.offsetMin        = Vector2.zero;
        verRT.offsetMax        = Vector2.zero;
        TMP_Text ver = verObj.AddComponent<TextMeshProUGUI>();
        ver.text      = "FXMedia Technical Test  ·  Unity 6 LTS";
        ver.fontSize  = 20;
        ver.color     = ColorGray * 0.55f;
        ver.alignment = TextAlignmentOptions.Center;

        // Add CanvasGroup to root canvas object and cache it for LoadGameScene
        canvasGroup       = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI FACTORY HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Stretch panel that fills its parent rect.</summary>
    private GameObject MakeStretchPanel(GameObject parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    /// <summary>Text element placed by anchor min/max (fully responsive).</summary>
    private TMP_Text MakeAnchoredText(GameObject parent, string name,
                                       Vector2 anchorMin, Vector2 anchorMax,
                                       Vector2 offsetDelta,
                                       string text, float fontSize,
                                       Color color, FontStyles style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero + offsetDelta;
        rt.offsetMax  = Vector2.zero;
        rt.pivot      = new Vector2(0.5f, 0.5f);
        TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing  = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    /// <summary>Legend row: colored dot + label on left, points on right.</summary>
    private void MakeLegendRow(GameObject parent,
                                Vector2 anchorMin, Vector2 anchorMax,
                                Color dotColor, string label, string pts)
    {
        GameObject row = new GameObject($"Row_{label}");
        row.transform.SetParent(parent.transform, false);
        RectTransform rt = row.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        // Dot
        GameObject dot = new GameObject("Dot");
        dot.transform.SetParent(row.transform, false);
        RectTransform dotRT = dot.AddComponent<RectTransform>();
        dotRT.anchorMin = new Vector2(0f, 0.2f);
        dotRT.anchorMax = new Vector2(0.06f, 0.8f);
        dotRT.offsetMin = Vector2.zero;
        dotRT.offsetMax = Vector2.zero;
        dot.AddComponent<Image>().color = dotColor;

        // Label
        TMP_Text lbl = MakeAnchoredText(row, "Label",
            new Vector2(0.08f, 0f), new Vector2(0.6f, 1f), Vector2.zero,
            label, 28, Color.white, FontStyles.Normal);
        lbl.alignment = TextAlignmentOptions.Left;

        // Points
        TMP_Text ptsT = MakeAnchoredText(row, "Pts",
            new Vector2(0.65f, 0f), new Vector2(1f, 1f), Vector2.zero,
            pts, 28, dotColor, FontStyles.Bold);
        ptsT.alignment = TextAlignmentOptions.Right;
    }

    /// <summary>Button placed by anchor min/max.</summary>
    private GameObject MakeButtonAnchor(GameObject parent, string name,
                                         Vector2 anchorMin, Vector2 anchorMax,
                                         Color normalColor, Color pressedColor,
                                         string label, float fontSize, Color textColor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        Image img = obj.AddComponent<Image>();
        img.color = normalColor;
        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = normalColor;
        cb.highlightedColor = normalColor * 1.2f;
        cb.pressedColor     = pressedColor;
        cb.selectedColor    = normalColor;
        btn.colors = cb;
        TMP_Text txt = MakeAnchoredText(obj, "Label",
            Vector2.zero, Vector2.one, Vector2.zero,
            label, fontSize, textColor, FontStyles.Bold);
        txt.alignment = TextAlignmentOptions.Center;
        return obj;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COROUTINES
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator LoadGameScene()
    {
        isTransitioning = true;
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeOut(canvasGroup, 0.3f));
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(GAME_SCENE_INDEX);
    }

    private IEnumerator FadeIn(CanvasGroup cg, float dur)
    {
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; cg.alpha = Mathf.Clamp01(t / dur); yield return null; }
        cg.alpha = 1f;
    }

    private IEnumerator FadeOut(CanvasGroup cg, float dur)
    {
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; cg.alpha = 1f - Mathf.Clamp01(t / dur); yield return null; }
        cg.alpha = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private Shader GetLitShader()
{
    if (litShader != null) return litShader;
    
    // Fallback chain with better error handling
    Shader s = Shader.Find("Universal Render Pipeline/Lit");
    if (s != null && s.isSupported) return s;
    
    s = Shader.Find("Standard");
    if (s != null && s.isSupported) return s;
    
    // Ultimate fallback - built-in unlit
    s = Shader.Find("Unlit/Color");
    if (s != null) return s;
    
    return Shader.Find("Sprites/Default");
}

}
