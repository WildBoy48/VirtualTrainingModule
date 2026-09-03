using UnityEngine;

/// <summary>
/// A simplified version of the MiniGameManager for the Full Grab Exoskeleton Therapy game, designed to manage the game levels and UI elements.
/// Since currently, the game is not using the Lobby scene, this script is intended to be used in the Therapy scene directly.
/// Controls the visibility of background details, score UI, and session interactables.
/// </summary>
public class MiniGameManagerSimpleExo : MonoBehaviour
{
    [Header("Environment Levels")]
    [SerializeField] private GameObject ceiling;
    [SerializeField] private GameObject level1;
    [SerializeField] private GameObject level2;
    [SerializeField] private GameObject level3;
    [SerializeField] private GameObject level4;

    [Header("UI and Data Tracking")]
    [SerializeField] private GameObject scoreUI;
    [SerializeField] private TherapyDataTracker dataTracker;

    [Header("Session Interactables")]
    [SerializeField] private GameObject spawnAreaSetup;
    [SerializeField] private GameObject plasticCup;
    [SerializeField] private GameObject coffeeCup;
    [SerializeField] private GameObject killZones;

    [Header("Settings")]
    [Tooltip("The selected value for background detail level, ranging from 0 to 3.")]
    [SerializeField] private int selectedValue;

    [Header("Developer Debug")]
    [Tooltip("Check this in the Editor to force Setup Mode without loading the Lobby scene.")]
    [SerializeField] private bool forceSetupMode = false;

    private void Start()
    {
        InitalizeGameState();
        ApplyBackgroundDetailLevel();
    }

    /// <summary>
    /// Sets the baseline active state for all core gameplay elements upon loading the scene.
    /// </summary>
    private void InitalizeGameState()
    {
        if(ceiling != null) ceiling.SetActive(true);
        if(scoreUI != null) scoreUI.SetActive(true);
        if(spawnAreaSetup != null) spawnAreaSetup.SetActive(false);

        if(dataTracker != null) dataTracker.enabled = true;

        if (plasticCup != null) plasticCup.SetActive(true);
        if(coffeeCup != null) coffeeCup.SetActive(false);
        if(killZones != null) killZones.SetActive(true);
    }

    /// <summary>
    /// Toggles the visibility of background detail levels based on the selected value, 
    /// allowing for dynamic adjustment of the game's visual complexity.
    /// Used to control visual complexity or distraction for the user.
    /// </summary>
    private void ApplyBackgroundDetailLevel()
    {
        Debug.Log($"<color=cyan>[MiniGameManager]</color> Selected background detail level: {selectedValue}");

        switch (selectedValue)
        {
            case 0:
                level1.SetActive(true);
                level2.SetActive(false);
                level3.SetActive(false);
                level4.SetActive(false);
                break;
            case 1:
                level1.SetActive(false);
                level2.SetActive(true);
                level3.SetActive(false);
                level4.SetActive(false);
                break;
            case 2:
                level1.SetActive(false);
                level2.SetActive(true);
                level3.SetActive(true);
                level4.SetActive(false);
                break;
            case 3:
                level1.SetActive(false);
                level2.SetActive(true);
                level3.SetActive(true);
                level4.SetActive(true);
                break;
            default:
                Debug.LogWarning("Invalid selected value for background detail level: " + selectedValue + ". Reverting to default State Level 0");
                level1.SetActive(true);
                level2.SetActive(false);
                level3.SetActive(false);
                level4.SetActive(false);
                break;
        }
    }
}
