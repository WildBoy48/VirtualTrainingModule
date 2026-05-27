using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    [SerializeField] private GameObject SessionParametersUI;
    [SerializeField] private Dropdown SessionParametersDropdown;
    [SerializeField] private GameObject Level2;
    [SerializeField] private GameObject Level3;
    [SerializeField] private Slider SeatHeightSlider;
    [SerializeField] private InputField SeatHeightInputField;
    [SerializeField] private GameObject Chair;

    private int selectedValue;
    private float seatHeightValue;

    void Awake()
    {
        Level2.SetActive(false);
        Level3.SetActive(false);
        SessionParametersUI.SetActive(false);
        Chair.SetActive(true);

        Debug.Log("[MiniGameManager] Awake called. Background Detail: " + WaitingLobbyManager.BackgroundDetail);
        Debug.Log("[MiniGameManager] Awake called. Seat Height: " + WaitingLobbyManager.SeatHeight);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (WaitingLobbyManager.CurrentMode == "setup")
        {
            SessionParametersUI.SetActive(true);
        }
        else
        {
            SessionParametersUI.SetActive(false);
        }

        SessionParametersDropdown.value = WaitingLobbyManager.BackgroundDetail - 1;
        SeatHeightSlider.value = WaitingLobbyManager.SeatHeight;
        SeatHeightInputField.text = WaitingLobbyManager.SeatHeight.ToString("F2");
        GetBackgroundDetailValue();
        GetSeatHeightValue();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetBackgroundDetailValue()
    {
        selectedValue = SessionParametersDropdown.value;
        Debug.Log("Selected value: " + selectedValue);

        if (selectedValue == 0)
        {
            Level2.SetActive(false);
            Level3.SetActive(false);
        }
        else if (selectedValue == 1)
        {
            Level2.SetActive(true);
            Level3.SetActive(false);
        }
        else if (selectedValue == 2)
        {
            Level2.SetActive(true);
            Level3.SetActive(true);
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
