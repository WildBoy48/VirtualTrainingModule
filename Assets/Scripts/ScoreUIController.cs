using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Listens to the ScoreManager for score changes and updates the UI accordingly.
/// </summary>
/// [RequireComponent(typeof(UIDocument))]
public class ScoreUIController : MonoBehaviour
{
    private Label scoreLabel;

    void Start()
    {
        var uiDocument = GetComponent<UIDocument>().rootVisualElement;
        
        scoreLabel = uiDocument.Q<Label>("score-label");
        if (scoreLabel != null)
        {
            scoreLabel.text = $"Score: 0";
        }
        else
        {
            Debug.LogWarning("[ScoreUIController] Could not find 'score-label' in the UIDocument.");
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
        }
    }

    /// <summary>
    /// Triggered when the score changes in the ScoreManager. Updates the score label in the UI.
    /// </summary>
    /// <param name="newScore"></param>
    private void UpdateScore(int newScore)
    {
        if(scoreLabel != null)
        {
            scoreLabel.text = $"Score: {newScore}";
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the score change event to prevent memory leaks
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
        }
    }
}
