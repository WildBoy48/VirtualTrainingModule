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

    public int CurrentScore { get; private set; }

    public event Action<int> OnScoreChanged;

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
        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);

        Debug.Log($"<color=yellow>[ScoreManager]</color> Score updated: {CurrentScore}");
    }
}
