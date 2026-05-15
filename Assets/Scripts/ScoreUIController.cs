using UnityEngine;
using UnityEngine.UIElements;

public class ScoreUIController : MonoBehaviour
{
    private Label scoreLabel;

    void Start()
    {
        var uiDocument = GetComponent<UIDocument>().rootVisualElement;
        
        scoreLabel = uiDocument.Q<Label>("score-label");
        scoreLabel.text = $"Score: 0";
        ScoreManager.Instance.OnScoreChanged += UpdateScore;
    }

    public void UpdateScore(int newScore)
    {
        if(scoreLabel != null)
        {
            scoreLabel.text = $"Score: {newScore}";
        }
    }
}
