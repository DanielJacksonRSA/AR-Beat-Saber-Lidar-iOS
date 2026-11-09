using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI multiplierText;
    [Tooltip("Drag your FeedbackText here!")]
    public TextMeshProUGUI feedbackText;

    [Header("Score Stats")]
    public int currentScore = 0;
    public int currentCombo = 0;
    public int currentMultiplier = 1;

    public int totalNotesSpawned = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
        ClearFeedback();
    }

    public void RegisterHit(int basePoints = 10)
    {
        currentCombo++;

        if (currentCombo >= 14) currentMultiplier = 8;
        else if (currentCombo >= 6) currentMultiplier = 4;
        else if (currentCombo >= 2) currentMultiplier = 2;
        else currentMultiplier = 1;

        currentScore += (basePoints * currentMultiplier);

        UpdateUI();
    }

    public void RegisterMissOrBomb()
    {
        currentCombo = 0;
        currentMultiplier = 1;

        UpdateUI();
    }

    public void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            CancelInvoke("ClearFeedback");
            Invoke("ClearFeedback", 1.0f);
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }

        if (multiplierText != null)
        {
            if (currentMultiplier == 1)
            {
                multiplierText.text = "";
            }
            else
            {
                multiplierText.text = currentMultiplier.ToString() + "x";

                switch (currentMultiplier)
                {
                    case 2: multiplierText.fontSize = 90; break;
                    case 4: multiplierText.fontSize = 120; break;
                    case 8: multiplierText.fontSize = 150; break;
                }
            }
        }
    }
}
