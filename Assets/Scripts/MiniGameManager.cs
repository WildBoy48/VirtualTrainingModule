using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{

    [SerializeField] private Dropdown SessionParametersDropdown;
    [SerializeField] private GameObject Level2;
    [SerializeField] private GameObject Level3;
    [SerializeField] private Slider SeatHeightSlider;
    [SerializeField] private InputField SeatHeightInputField;

    private int selectedValue;
    private float seatHeightValue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
    }

    public void SaveSessionParameters()
    {
        //PlayerPrefs.SetInt("SelectedValue", selectedValue);
        //PlayerPrefs.SetFloat("SeatHeightValue", seatHeightValue);
        Debug.Log("Session parameters saved.");

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
        }
        else
        {
            Debug.LogWarning("Invalid input for seat height. Please enter a valid number.");
        }
    }
}
