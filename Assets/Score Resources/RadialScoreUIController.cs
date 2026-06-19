using UnityEngine;
using UnityEngine.UIElements;

public class RadialScoreUIController : MonoBehaviour
{
    private Label percentageLabel;

    void Start()
    {
        var uiDocument = GetComponent<UIDocument>().rootVisualElement;

        percentageLabel = uiDocument.Q<Label>("percentage-label");
        percentageLabel.text = $"0%";
        ScoreManager.Instance.OnPercentageChanged += UpdatePercentage;
    }

    public void UpdatePercentage(float percentage)
    {
        if (percentageLabel != null)
        {
            percentageLabel.text = $"{percentage:F0}%";
        }
        
    }
}