using UnityEngine;
using UnityEngine.UIElements;


public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public int CurrentScore { get; private set; }

    public delegate void ScoreChanged(int newScore);
    public event ScoreChanged OnScoreChanged;

    

    private void Awake()
    {
        Instance = this;
    }

   public void AddScore(int amount)
    {
        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);

        Debug.Log("Score: " + CurrentScore);
    }
}
