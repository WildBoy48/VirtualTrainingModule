using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using UnityEditor;

namespace DefaultNamespace
{
    public class BCIMiniGameManager : MonoBehaviour
    {
        [SerializeField] private GameObject VrOrigin;
        [SerializeField] private GameObject RecenterPoint;
        [SerializeField] private GameObject SessionParametersUI;
        [SerializeField] private Dropdown SessionParametersDropdown;
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
        [SerializeField] public CustomInletOnline customInletOnline;

        private int selectedValue;

        [Header("Developer Debug")]
        [Tooltip("Enable Offline Mode for Debug and Simulate all the Modes without the Server")]
        [HideInInspector] public bool offlineMode = false;
        [Tooltip("Setup Mode: Allows to test the SessionParametersUI and preview the changes of the menu")]
        [HideInInspector] public bool forceSetupMode = false;
        [Tooltip("Calibration Mode: Allows the user to do BCI training")]
        [HideInInspector] public bool forceCalibrationMode = false;
        [Tooltip("Session Mode: Allows the user to do BCI therapy sessions")]
        [HideInInspector] public bool forceSessionMode = false;
        [Range(0.9f, 1.2f)]
        [HideInInspector] public float seatHeightValue = 1.0f;
        public enum BackgroundDetail
        {
            Level1,
            Level2,
            Level3,
            Level4
        }
        [HideInInspector] public BackgroundDetail backgroundDetail = BackgroundDetail.Level1;
        [HideInInspector] public bool enableScoreUI = false;
        [HideInInspector] public int targetScore = 1000;

        void Awake()
        {
            if (offlineMode)
            {
                Debug.LogWarning("[MiniGameManager] Offline Mode is enabled.");
                gameStatsReporter = FindObjectOfType<GameStatsReporter>();
                gameStatsReporter.enabled = false;
            } 
            else
            {
                Level1.SetActive(true);
                Level2.SetActive(false);
                Level3.SetActive(false);
                Level4.SetActive(false);
                SessionParametersUI.SetActive(false);
                Chair.SetActive(true);
                ScoreUI.SetActive(false);
                customInletTraining.enabled = false;
                customInletOnline.enabled = false;
                gameStatsReporter = FindObjectOfType<GameStatsReporter>();
                gameStatsReporter.enabled = false;
            }
            

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
            if (offlineMode)
            {
                if (forceSetupMode)
                {
                    customInletTraining.enabled = false;
                    customInletOnline.enabled = false;
                    SessionParametersUI.SetActive(true);
                    ScoreUI.SetActive(false);
                    gameStatsReporter.enabled = false;
                }
                else if (forceCalibrationMode)
                {
                    customInletTraining.enabled = true;
                    customInletOnline.enabled = false;
                    SessionParametersUI.SetActive(false);
                    ScoreUI.SetActive(false);
                    SessionParametersDropdown.value = (int)backgroundDetail;
                    SeatHeightSlider.value = seatHeightValue;   
                    GetBackgroundDetailValue();
                    GetSeatHeightValue();
                    gameStatsReporter.enabled = false;
                }
                else if (forceSessionMode)
                {
                    customInletTraining.enabled = false;
                    customInletOnline.enabled = true;
                    SessionParametersUI.SetActive(false);
                    ScoreUI.GetComponent<ScoreManager>().TargetScore = targetScore;
                    if (enableScoreUI) {
                        ScoreUI.SetActive(true);
                    }
                    else {
                        ScoreUI.SetActive(false);
                    }
                    SessionParametersDropdown.value = (int)backgroundDetail;
                    SeatHeightSlider.value = seatHeightValue;
                    GetBackgroundDetailValue();
                    GetSeatHeightValue();
                    gameStatsReporter.enabled = true;
                }
            }
            else
            {
                if (WaitingLobbyManager.Instance == null)
                {
                    Debug.LogError("[MiniGameManager] Start called but WaitingLobbyManager.Instance is null. Cannot access game parameters.");
                    SessionParametersUI.SetActive(false);
                    ScoreUI.SetActive(false);
                    gameStatsReporter.enabled = false;
                    return;
                }

                if (WaitingLobbyManager.CurrentMode == "setup")
                {
                    customInletTraining.enabled = false;
                    customInletOnline.enabled = false;
                    SessionParametersUI.SetActive(true);
                    ScoreUI.SetActive(false);
                    gameStatsReporter.enabled = false;
                }
                else if (WaitingLobbyManager.CurrentMode == "calibration")
                {
                    customInletTraining.enabled = true;
                    customInletOnline.enabled = false;
                    SessionParametersUI.SetActive(false);
                    ScoreUI.SetActive(false);
                    gameStatsReporter.enabled = false;
                }
                else if (WaitingLobbyManager.CurrentMode == "session")
                {
                    customInletTraining.enabled = false;
                    customInletOnline.enabled = true;
                    SessionParametersUI.SetActive(false);
                    ScoreUI.SetActive(WaitingLobbyManager.VisualCues);
                    gameStatsReporter.enabled = true;
                }
                else
                {
                    customInletTraining.enabled = false;
                    customInletOnline.enabled = false;
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

            Recenter();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void OnDestroy()
        {
            gameStatsReporter.enabled = false;
        }
        
        public void Recenter()
        {
            XROrigin xrOrigin = VrOrigin.GetComponent<XROrigin>();

            if (xrOrigin != null)
            {
                xrOrigin.MoveCameraToWorldLocation(RecenterPoint.transform.position);
                xrOrigin.MatchOriginUpCameraForward(RecenterPoint.transform.up, RecenterPoint.transform.forward);
                Debug.Log("[MiniGameManager] Recentered the XR Origin.");
            }
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

    [CustomEditor(typeof(BCIMiniGameManager))]
    public class BCIMiniGameManagerEditor : Editor
    {
        private BCIMiniGameManager bciMiniGameManager;
        SerializedProperty offlineModeProp;
        SerializedProperty forceSetupModeProp;
        SerializedProperty forceCalibrationModeProp;
        SerializedProperty forceSessionModeProp;
        SerializedProperty seatHeightProp;
        SerializedProperty backgroundDetailProp;
        SerializedProperty enableScoreUIProp;
        SerializedProperty targetScoreProp;

        public void OnEnable()
        {
            bciMiniGameManager = (BCIMiniGameManager)target;

            offlineModeProp = serializedObject.FindProperty("offlineMode");
            forceSetupModeProp = serializedObject.FindProperty("forceSetupMode");
            forceCalibrationModeProp = serializedObject.FindProperty("forceCalibrationMode");
            forceSessionModeProp = serializedObject.FindProperty("forceSessionMode");
            seatHeightProp = serializedObject.FindProperty("seatHeightValue");
            backgroundDetailProp = serializedObject.FindProperty("backgroundDetail");
            enableScoreUIProp = serializedObject.FindProperty("enableScoreUI");
            targetScoreProp = serializedObject.FindProperty("targetScore");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(offlineModeProp);
            
            if (offlineModeProp.boolValue)
            {
                // Setup Mode
                EditorGUILayout.PropertyField(forceSetupModeProp);

                if (forceSetupModeProp.boolValue)
                {
                    forceCalibrationModeProp.boolValue = false;
                    forceSessionModeProp.boolValue = false;
                }

                // Calibration Mode
                EditorGUILayout.PropertyField(forceCalibrationModeProp);

                if (forceCalibrationModeProp.boolValue)
                {
                    forceSetupModeProp.boolValue = false;
                    forceSessionModeProp.boolValue = false;

                    EditorGUILayout.PropertyField(seatHeightProp);
                    EditorGUILayout.PropertyField(backgroundDetailProp);
                }
                // Session Mode
                EditorGUILayout.PropertyField(forceSessionModeProp);
                if (forceSessionModeProp.boolValue)
                {
                    forceSetupModeProp.boolValue = false;
                    forceCalibrationModeProp.boolValue = false;

                    EditorGUILayout.PropertyField(seatHeightProp);
                    EditorGUILayout.PropertyField(backgroundDetailProp);
                    EditorGUILayout.PropertyField(enableScoreUIProp);

                    if (enableScoreUIProp.boolValue)
                    {
                        bciMiniGameManager.customInletOnline.ShowScoreBoard = true;
                        EditorGUILayout.PropertyField(targetScoreProp);
                    } 
                    else 
                    {
                        bciMiniGameManager.customInletOnline.ShowScoreBoard = false;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}