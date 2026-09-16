using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Listens to the ScoreManager for score changes and updates the UI accordingly.
/// </summary>
/// [RequireComponent(typeof(UIDocument))]
public class ScoreUIController : MonoBehaviour
{
    private Label scoreLabel;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            scoreLabel = uiDocument.rootVisualElement.Q<Label>("score-label");
            if (scoreLabel != null) scoreLabel.text = "Score: 0";
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

    private void OnDisable()
    {
        // Unsubscribe from the score change event to prevent memory leaks
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
        }
    }
}
