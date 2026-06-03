using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages all in-game UI panels: HUD (score + timer), item type popup,
/// and Game Over screen with final score + high score.
/// Requires TextMeshPro package (included with Unity 6).
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── HUD References ────────────────────────────────────────────────────────
    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text highScoreText;

    // ── Popup (shows "+1", "+3", "+5") ────────────────────────────────────────
    [Header("Collection Popup")]
    [SerializeField] private TMP_Text collectionPopupText;
    [SerializeField] private float    popupDuration = 1.2f;

    // ── Game Over Panel ───────────────────────────────────────────────────────
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text   finalScoreText;
    [SerializeField] private TMP_Text   finalHighScoreText;
    [SerializeField] private Button     restartButton;

    // ── Item Legend (optional HUD legend) ────────────────────────────────────
    [Header("Item Legend")]
    [SerializeField] private TMP_Text legendText;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Coroutine popupCoroutine;

    private void Start()
    {
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.AddListener(UpdateScore);
            GameManager.Instance.OnTimerChanged.AddListener(UpdateTimer);
            GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
            GameManager.Instance.OnItemCollectedFX.AddListener(ShowCollectionPopup);
        }

        // Hide panels at start
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (collectionPopupText != null) collectionPopupText.gameObject.SetActive(false);

        // Set legend text
        if (legendText != null)
            legendText.text = "🟡 Common +1   🔵 Rare +3   🔴 Epic +5";

        // Restart button wiring
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());

        // Initial values
        UpdateScore(0);
        UpdateTimer(60f);
        UpdateHighScore();
    }

    // ── Listeners ─────────────────────────────────────────────────────────────

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void UpdateTimer(float seconds)
    {
        if (timerText == null) return;

        int s = Mathf.CeilToInt(seconds);
        timerText.text = $"Time: {s}";

        // Turn red in last 10 seconds
        timerText.color = (s <= 10) ? new Color(1f, 0.2f, 0.2f) : Color.white;
    }

    private void UpdateHighScore()
    {
        if (highScoreText != null && GameManager.Instance != null)
            highScoreText.text = $"Best: {GameManager.Instance.HighScore}";
    }

    private void ShowGameOver(int finalScore)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = $"Final Score\n{finalScore}";

        if (finalHighScoreText != null && GameManager.Instance != null)
        {
            bool isNewHigh = finalScore >= GameManager.Instance.HighScore;
            finalHighScoreText.text = isNewHigh
                ? $"🏆 NEW HIGH SCORE! 🏆"
                : $"Best: {GameManager.Instance.HighScore}";
        }
    }

    private void ShowCollectionPopup(int points)
    {
        if (collectionPopupText == null) return;

        if (popupCoroutine != null) StopCoroutine(popupCoroutine);
        popupCoroutine = StartCoroutine(AnimatePopup(points));
    }

    private IEnumerator AnimatePopup(int points)
    {
        collectionPopupText.gameObject.SetActive(true);

        // Color by point value
        if      (points >= 5) collectionPopupText.color = new Color(1f, 0.2f, 0.2f);
        else if (points >= 3) collectionPopupText.color = new Color(0.2f, 0.6f, 1f);
        else                  collectionPopupText.color = new Color(1f, 0.85f, 0f);

        collectionPopupText.text = $"+{points}";

        // Scale punch animation
        collectionPopupText.transform.localScale = Vector3.one * 1.5f;

        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            // Scale down from 1.5 → 1 in first half, then fade out
            float scale = Mathf.Lerp(1.5f, 1f, Mathf.Clamp01(t * 2f));
            collectionPopupText.transform.localScale = Vector3.one * scale;

            // Fade out in second half
            float alpha = 1f - Mathf.Clamp01((t - 0.5f) * 2f);
            Color c = collectionPopupText.color;
            c.a = alpha;
            collectionPopupText.color = c;

            yield return null;
        }

        collectionPopupText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged.RemoveListener(UpdateScore);
            GameManager.Instance.OnTimerChanged.RemoveListener(UpdateTimer);
            GameManager.Instance.OnGameOver.RemoveListener(ShowGameOver);
            GameManager.Instance.OnItemCollectedFX.RemoveListener(ShowCollectionPopup);
        }
    }
}
