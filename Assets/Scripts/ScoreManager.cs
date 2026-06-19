using UnityEngine;
using UnityEngine.UIElements;


public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public int CurrentScore { get; private set; }
    public float PercentageScore {get; private set;}

    public delegate void ScoreChanged(int newScore);
    public event ScoreChanged OnScoreChanged;

    public delegate void PercentageChanged(float newPercentage);
    public event PercentageChanged OnPercentageChanged;

    [SerializeField] public int TargetScore = 1000;
    

    private void Awake()
    {
        Instance = this;
        Debug.Log("ScoreManager Awake. Target Score: " + TargetScore);
    }

   public void AddScore(int amount)
    {
        CurrentScore += amount;
        PercentageScore = (float) CurrentScore / TargetScore * 100f;

        OnScoreChanged?.Invoke(CurrentScore);
        OnPercentageChanged?.Invoke(PercentageScore);

        Debug.Log("Score: " + CurrentScore);
        Debug.Log("Percentage Score: " + PercentageScore);
    }
}
