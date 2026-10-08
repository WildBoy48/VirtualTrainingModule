using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Singleton manager responsible for tracking and updating the player's score. 
/// It provides methods to add to the score and notifies subscribers when the score changes.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] public int maxScore = 100; // Maximum score for the game, can be set in the Inspector
    public int currentScore { get; private set; }

    public event Action<int, int> OnScoreChanged;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    ///  Adds to the current score and invokes the OnScoreChanged event to notify subscribers of the change.
    /// </summary> 
    /// <param name="amount"> The amount to add to the current score.</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore,maxScore);

        Debug.Log($"<color=yellow>[ScoreManager]</color> Score updated: {currentScore}");

    }
}
