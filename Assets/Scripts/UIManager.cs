using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Canvases")]
    public GameObject startScreen;
    public GameObject scanningScreen;
    public GameObject hudScreen;
    public GameObject gameOverScreen;

    [Header("Game Over Elements")]
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI maxScoreText;
    public TextMeshProUGUI ratingText;

    [Header("Game Hooks")]
    public LiDARSpawner spawner;
    [Tooltip("Drag a Text object here to show loading status!")]
    public TextMeshProUGUI loadingText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ShowSplashScreen();
        if (loadingText != null) loadingText.text = "TAP ANYWHERE TO SELECT A SONG";
    }

    public void OpenIphoneFilesApp()
    {
        if (loadingText != null && loadingText.text == "LOADING SONG...") return;

        string[] fileTypes = new string[] { "public.audio", "public.mp3", "com.microsoft.waveform-audio" };

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                if (loadingText != null) loadingText.text = "TAP ANYWHERE TO SELECT A SONG";
            }
            else
            {
                if (loadingText != null) loadingText.text = "LOADING SONG...";
                StartCoroutine(LoadCustomAudioClip(path));
            }
        }, fileTypes);
    }

    private IEnumerator LoadCustomAudioClip(string path)
    {
        string uriPath = "file://" + path;
        AudioType audioType = AudioType.UNKNOWN;

        if (path.ToLower().EndsWith(".mp3")) audioType = AudioType.MPEG;
        else if (path.ToLower().EndsWith(".wav")) audioType = AudioType.WAV;

        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uriPath, audioType))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                if (loadingText != null) loadingText.text = "ERROR LOADING FILE. TAP TO TRY AGAIN.";
            }
            else
            {
                AudioClip customClip = DownloadHandlerAudioClip.GetContent(uwr);
                customClip.name = Path.GetFileNameWithoutExtension(path);
                if (loadingText != null) loadingText.text = "";
                StartScanningWithSong(customClip);
            }
        }
    }

    private void HideAllScreens()
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (scanningScreen != null) scanningScreen.SetActive(false);
        if (hudScreen != null) hudScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
    }

    public void ShowSplashScreen()
    {
        HideAllScreens();
        startScreen.SetActive(true);
    }

    public void StartScanningWithSong(AudioClip selectedSong)
    {
        HideAllScreens();
        scanningScreen.SetActive(true);
        if (spawner != null) spawner.BeginScanning(selectedSong);
    }

    public void StartPlaying()
    {
        HideAllScreens();
        hudScreen.SetActive(true);
    }

    public void TriggerGameOver(bool survived)
    {
        HideAllScreens();
        gameOverScreen.SetActive(true);
        resultTitleText.text = survived ? "LEVEL CLEARED" : "LEVEL FAILED";

        int score = ScoreManager.Instance.currentScore;
        int totalNotes = ScoreManager.Instance.totalNotesSpawned;
        int maxScore = CalculateMaxScore(totalNotes);

        finalScoreText.text = "Score: " + score.ToString("N0");
        maxScoreText.text = "Max Possible: " + maxScore.ToString("N0");

        SetRatingDisplay(score, maxScore);
    }

    private int CalculateMaxScore(int totalNotes)
    {
        if (totalNotes <= 0) return 0;
        if (totalNotes == 1) return 10;
        if (totalNotes <= 5) return 10 + ((totalNotes - 1) * 20);
        if (totalNotes <= 13) return 90 + ((totalNotes - 5) * 40);
        return 410 + ((totalNotes - 13) * 80);
    }

    // Maps score / max possible score to a letter rating.
    private void SetRatingDisplay(int score, int maxScore)
    {
        if (ratingText == null) return;

        float percentage = (maxScore > 0) ? (float)score / maxScore : 0;

        if (percentage >= 0.95f)
        {
            ratingText.text = "S";
            ratingText.color = new Color(1f, 0.84f, 0f); // gold
        }
        else if (percentage >= 0.85f)
        {
            ratingText.text = "A+";
            ratingText.color = Color.green;
        }
        else if (percentage >= 0.75f)
        {
            ratingText.text = "A";
            ratingText.color = Color.green;
        }
        else if (percentage >= 0.60f)
        {
            ratingText.text = "B";
            ratingText.color = Color.cyan;
        }
        else if (percentage >= 0.45f)
        {
            ratingText.text = "C";
            ratingText.color = Color.blue;
        }
        else if (percentage >= 0.30f)
        {
            ratingText.text = "D";
            ratingText.color = new Color(1f, 0.5f, 0f); // Orange
        }
        else
        {
            ratingText.text = "E";
            ratingText.color = Color.red;
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
