using UnityEngine;
using UnityEngine.UI;


public class RadialProgressCanvas : MonoBehaviour
{
    public Image progressRing;
    [Range(0f,1f)]
    public float currentProgress;
    
    void Start()
    {
        UpdateRing();
        ScoreManager.Instance.OnPercentageChanged += OnPercentageChanged;
    }

    void OnPercentageChanged (float percentage)
    {
        currentProgress = percentage;
        UpdateRing();
    }

    // Update is called once per frame
    void UpdateRing()
    {
        progressRing.fillAmount = currentProgress / 100f;
    }
}
