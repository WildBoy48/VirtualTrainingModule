using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class BCIMiniGameManager : MonoBehaviour
{
    [SerializeField] private GameObject SessionParametersUI;
    [SerializeField] private Dropdown SessionParametersDropdown;
    [SerializeField] private GameObject Ceiling;
    [SerializeField] private GameObject Level1;
    [SerializeField] private GameObject Level2;
    [SerializeField] private GameObject Level3;
    [SerializeField] private GameObject Level4;
    [SerializeField] private Slider SeatHeightSlider;
    [SerializeField] private InputField SeatHeightInputField;
    [SerializeField] private GameObject Chair;
    [SerializeField] private GameObject ScoreUI;
    [SerializeField] private GameStatsReporter gameStatsReporter;
    [SerializeField] private CustomInletTraining customInletTraining;

    private int selectedValue;
    private float seatHeightValue;

    [Header("Developer Debug")]
    [Tooltip("Check this in the Editor to simulate all Modes without loading the Lobby scene.")]
    public bool forceSetupMode = false;
    public bool forceCalibrationMode = false;
    public bool forceSessionMode = false;

    void Awake()
    {
        Level1.SetActive(true);
        Level2.SetActive(false);
        Level3.SetActive(false);
        Level4.SetActive(false);
        Ceiling.SetActive(true);
        SessionParametersUI.SetActive(false);
        Chair.SetActive(true);
        ScoreUI.SetActive(false);
        customInletTraining.enabled = false;

        gameStatsReporter = FindObjectOfType<GameStatsReporter>();
        gameStatsReporter.enabled = false;

        // Add null checks for WaitingLobbyManager initialization
        if (WaitingLobbyManager.Instance != null)
        {
            Debug.Log("[MiniGameManager] Awake called. Background Detail: " + WaitingLobbyManager.BackgroundDetail);
            Debug.Log("[MiniGameManager] Awake called. Seat Height: " + WaitingLobbyManager.SeatHeight);
        }
        else
        {
            Debug.LogWarning("[MiniGameManager] Awake called but WaitingLobbyManager.Instance is null");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Defensive: ensure WaitingLobbyManager is initialized before accessing static properties
        // if (WaitingLobbyManager.Instance == null)
        // {
        //     Debug.LogError("[MiniGameManager] Start called but WaitingLobbyManager.Instance is null. Cannot access game parameters.");
        //     SessionParametersUI.SetActive(false);
        //     ScoreUI.SetActive(false);
        //     gameStatsReporter.enabled = false;
        //     return;
        // }

        if (WaitingLobbyManager.CurrentMode == "setup" || forceSetupMode)
        {
            customInletTraining.enabled = false;
            SessionParametersUI.SetActive(true);
            ScoreUI.SetActive(false);
            gameStatsReporter.enabled = false;
        }
        else if (WaitingLobbyManager.CurrentMode == "calibration" || forceCalibrationMode)
        {
            customInletTraining.enabled = true;
            SessionParametersUI.SetActive(false);
            if (forceCalibrationMode){
                ScoreUI.SetActive(false);
            } else {
                ScoreUI.SetActive(WaitingLobbyManager.VisualCues);
            }
            gameStatsReporter.enabled = false;
        }
        else if (WaitingLobbyManager.CurrentMode == "session" || forceSessionMode)
        {
            customInletTraining.enabled = false;
            SessionParametersUI.SetActive(false);
            if (forceSessionMode){
                ScoreUI.SetActive(true);
            } else {
                ScoreUI.SetActive(WaitingLobbyManager.VisualCues);
            }
            gameStatsReporter.enabled = true;
        }
        else
        {
            customInletTraining.enabled = false;
            SessionParametersUI.SetActive(false);
            ScoreUI.SetActive(false);
            gameStatsReporter.enabled = false;
        }

        // Apply slider and input field values
        SessionParametersDropdown.value = Mathf.Max(0, WaitingLobbyManager.BackgroundDetail - 1); // Ensure valid dropdown index
        SeatHeightSlider.value = WaitingLobbyManager.SeatHeight;
        SeatHeightInputField.text = WaitingLobbyManager.SeatHeight.ToString("F2");
        GetBackgroundDetailValue();
        GetSeatHeightValue();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        gameStatsReporter.enabled = false;
    }
    
    public void GetBackgroundDetailValue()
    {
        selectedValue = SessionParametersDropdown.value;
        Debug.Log("Selected value: " + selectedValue);

        if (selectedValue == 0)
        {
            Level1.SetActive(true);
            Level2.SetActive(false);
            Level3.SetActive(false);
            Level4.SetActive(false);
        }
        else if (selectedValue == 1)
        {
            Level1.SetActive(false);
            Level2.SetActive(true);
            Level3.SetActive(false);
            Level4.SetActive(false);
        }
        else if (selectedValue == 2)
        {
            Level1.SetActive(false);
            Level2.SetActive(true);
            Level3.SetActive(true);
            Level4.SetActive(false);
        }
        else if (selectedValue == 3)
        {
            Level1.SetActive(false);
            Level2.SetActive(true);
            Level3.SetActive(true);
            Level4.SetActive(true);
        }
    }

    public void GetSeatHeightValue()
    {
        seatHeightValue = SeatHeightSlider.value;
        Debug.Log("Slider value: " + seatHeightValue);

        SeatHeightInputField.text = seatHeightValue.ToString("F2");
        Chair.transform.position = new Vector3(Chair.transform.position.x, seatHeightValue, Chair.transform.position.z);
    }

    public async void SaveSessionParameters()
    {
        WaitingLobbyManager.SeatHeight = seatHeightValue;
        WaitingLobbyManager.BackgroundDetail = selectedValue + 1;
        
        Debug.Log("Seat Height set to: " + WaitingLobbyManager.SeatHeight);
        Debug.Log("Background Detail set to: " + WaitingLobbyManager.BackgroundDetail);

        // Await export to ensure therapist app receives updated values before we change scene
        await WaitingLobbyManager.ExportParametersStaticAsync();
        SceneManager.LoadScene(0);
    }

    public void GetInputSeatHeightValue()
    {
        if (float.TryParse(SeatHeightInputField.text, out float inputValue))
        {
            if (inputValue < SeatHeightSlider.minValue || inputValue > SeatHeightSlider.maxValue)
            {
                Debug.LogWarning("Input value is out of range. Please enter a value between " + SeatHeightSlider.minValue + " and " + SeatHeightSlider.maxValue + ".");
                return;
            }
            seatHeightValue = inputValue;
            Debug.Log("Input field value: " + seatHeightValue);

            SeatHeightSlider.value = seatHeightValue;
            Chair.transform.position = new Vector3(Chair.transform.position.x, seatHeightValue, Chair.transform.position.z);
        }
        else
        {
            Debug.LogWarning("Invalid input for seat height. Please enter a valid number.");
        }
    }
}
