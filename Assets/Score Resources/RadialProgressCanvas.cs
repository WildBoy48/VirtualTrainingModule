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
        ScoreManager.Instance.OnScoreChanged += OnScoreChanged;
        
    }

    void OnScoreChanged (int score, int maxScore)
    {
        currentProgress = Mathf.Clamp01((float)score / (float)maxScore);
        UpdateRing();
    }

    // Update is called once per frame
    void UpdateRing()
    {
        progressRing.fillAmount = currentProgress;
    }
    private void OnDestroy()
    {
        // ALWAYS unsubscribe to prevent memory leaks and NullReferenceExceptions!
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;
        }
    }
}
