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

    void OnScoreChanged (int score)
    {
        currentProgress = Mathf.Clamp01(score / 100f);
        UpdateRing();
    }

    // Update is called once per frame
    void UpdateRing()
    {
        progressRing.fillAmount = currentProgress;
    }
}
