using UnityEngine;
using UnityEngine.UIElements;

public class RadialScoreUIController : MonoBehaviour
{
    private Label percentageLabel;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        if(uiDocument != null && uiDocument.rootVisualElement != null)
        {
            percentageLabel = uiDocument.rootVisualElement.Q<Label>("percentage-label");
            if (percentageLabel != null)
            {
                percentageLabel.text = $"0%";
            }
        }
        else
        {
            Debug.LogWarning("[RadialScoreUIController] UIDocument or its rootVisualElement is null.");
        }
        
        if(ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;

        }
    }

    public void UpdateScore(int newScore)
    {
        if (percentageLabel != null)
        {
            percentageLabel.text = $"{newScore}%";
        }
        
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
        }
    }
}