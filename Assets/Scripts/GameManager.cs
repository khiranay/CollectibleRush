using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton GameManager. Owns game state: score, timer, high score (PlayerPrefs).
/// Broadcasts events so UI and other systems stay decoupled.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 60f;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSFX;
    [SerializeField] private AudioClip gameOverSFX;

    // ── Events (UI subscribes to these) ──────────────────────────────────────
    public UnityEvent<int>   OnScoreChanged    = new UnityEvent<int>();
    public UnityEvent<float> OnTimerChanged    = new UnityEvent<float>();
    public UnityEvent<int>   OnGameOver        = new UnityEvent<int>();   // passes final score
    public UnityEvent        OnGameStarted     = new UnityEvent();
    public UnityEvent<int>   OnItemCollectedFX = new UnityEvent<int>();   // passes point value

    // ── State ─────────────────────────────────────────────────────────────────
    private int   currentScore  = 0;
    private float timeRemaining = 0f;
    private bool  gameActive    = false;

    private AudioSource audioSource;

    // ── PlayerPrefs key ───────────────────────────────────────────────────────
    private const string HIGH_SCORE_KEY = "HighScore";

    // ── Properties ────────────────────────────────────────────────────────────
    public int   Score          => currentScore;
    public float TimeRemaining  => timeRemaining;
    public bool  GameActive     => gameActive;
    public int   HighScore      => PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (!gameActive) return;

        timeRemaining -= Time.deltaTime;
        OnTimerChanged.Invoke(Mathf.Max(0f, timeRemaining));

        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void StartGame()
    {
        currentScore  = 0;
        timeRemaining = gameDuration;
        gameActive    = true;

        OnScoreChanged.Invoke(currentScore);
        OnTimerChanged.Invoke(timeRemaining);
        OnGameStarted.Invoke();
    }

    /// <summary>
    /// Called by a Collectible when the player touches it.
    /// </summary>
    public void OnItemCollected(Collectible item)
    {
        if (!gameActive) return;

        int points = item.PointValue;
        AddScore(points);
        OnItemCollectedFX.Invoke(points);

        PlaySFX(collectSFX);
    }

    public void AddScore(int points)
    {
        currentScore += points;
        OnScoreChanged.Invoke(currentScore);
    }

    public void EndGame()
    {
        if (!gameActive) return;

        gameActive    = false;
        timeRemaining = 0f;

        // Save high score
        if (currentScore > HighScore)
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, currentScore);

        PlayerPrefs.Save();

        PlaySFX(gameOverSFX);
        OnGameOver.Invoke(currentScore);

        // Stop player movement
        PlayerController player = FindAnyObjectByType<PlayerController>();
        player?.StopMovement();
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
