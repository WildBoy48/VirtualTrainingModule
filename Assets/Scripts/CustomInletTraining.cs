using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using LSL;


public enum BCITrialPhase
{
    None,
    TrialStarted,
    CrossOnScreen,
    Beep,
    CueShown,
    Feedback,
    TrialEnded,
    SessionEnded,
    Training,
    ExperimentStopped
}

public enum BCIDirection
{
    None,
    Left,
    Right
}

[Serializable]
public class MarkerCodeEvent : UnityEvent<int> { }

[Serializable]
public class MarkerTextEvent : UnityEvent<string> { }

public class CustomInletTraining : MonoBehaviour
{
    [Header("LSL marker stream")]
    public string MarkerStreamName = "openvibeMarkers";

    [Header("Observed OpenViBE / Graz marker codes")]
    public int TrialStartCode = 768;
    public int CrossOnScreenCode = 786;
    public int BeepCode = 33282;

    public int LeftCueCode = 769;
    public int RightCueCode = 770;

    public int FeedbackStartCode = 781;
    public int TrialEndCode = 800;

    public int SessionEndCode = 1010;
    public int TrainCode = 33281;

    public int ExperimentStartCode = 32769;
    public int ExperimentStopCode = 32770;


    [Header("Controlled Objects")]
    [SerializeField] private GameObject rightHand;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightCup;
    [SerializeField] private GameObject leftCup;
    [SerializeField] private GameObject cross;

    private Animator rightHandAnimator;
    private Animator leftHandAnimator;
    private bool isLeftSide = true;

    //[SerializeField] private GameObject Sphere;
    [SerializeField] private float MoveDistance = 1.0f;

    [Header("Test movement behaviour")]
    [Tooltip("Object moved when relevant marker codes are received. If empty, this GameObject is moved.")]
    public Transform TargetToMove;

    [Tooltip("If true, movement uses the target object's local axes. If false, movement uses world axes.")]
    public bool UseLocalMovement = false;

    [Header("State")]
    public BCITrialPhase CurrentPhase { get; private set; } = BCITrialPhase.None;
    public BCIDirection CurrentDirection { get; private set; } = BCIDirection.None;

    public int LatestMarkerCode { get; private set; } = -1;
    public string LatestRawMarker { get; private set; } = "";
    public double LatestMarkerTimestamp { get; private set; } = 0.0;

    public bool IsInTrial { get; private set; } = false;
    public bool IsFeedbackActive { get; private set; } = false;

    // [Header("Built-in Unity events")]
    // public UnityEvent OnExperimentStart;
    // public UnityEvent OnExperimentStop;

    // public UnityEvent OnTrialStart;
    // public UnityEvent OnCrossOnScreen;
    // public UnityEvent OnBeep;

    // public UnityEvent OnLeftCue;
    // public UnityEvent OnRightCue;
    // public UnityEvent OnAnyCue;

    // public UnityEvent OnFeedbackStart;
    // public UnityEvent OnTrialEnd;

    // public UnityEvent OnSessionEnd;
    // public UnityEvent OnTrain;

    [Header("Generic marker events")]
    public MarkerCodeEvent OnAnyMarkerCode;
    public MarkerTextEvent OnAnyRawMarker;
    public MarkerCodeEvent OnUnhandledMarkerCode;

    [Header("Debug")]
    public bool PrintDebugMessages = true;
    public bool IgnoreZeroMarkers = true;
    public int MaxMarkersPerFrame = 64;

    private ContinuousResolver markerResolver;
    private StreamInlet markerInlet;
    private string[] markerSample;

    public event Action<int, double> MarkerReceived;
    public event Action<double> LeftCueReceived;
    public event Action<double> RightCueReceived;
    public event Action<double> TrialStartReceived;
    public event Action<double> TrialEndReceived;
    public event Action<double> FeedbackStartReceived;

    void Awake()
    {
        if (rightHand != null){
            rightHandAnimator = rightHand.GetComponent<Animator>();
            rightHandAnimator.speed = 0f;
        }
            

        if (leftHand != null){
            leftHandAnimator = leftHand.GetComponent<Animator>();
            leftHandAnimator.speed = 0f;
        }
        cross.SetActive(false);
        rightCup.SetActive(false);
        leftCup.SetActive(false);
    }

    void Start()
    {
        if (string.IsNullOrWhiteSpace(MarkerStreamName))
        {
            Debug.LogError("MarkerStreamName is empty.");
            enabled = false;
            return;
        }

        markerResolver = new ContinuousResolver("name", MarkerStreamName);
        StartCoroutine(ResolveMarkerStream());
    }

    IEnumerator ResolveMarkerStream()
    {
        var results = markerResolver.results();

        while (results.Length == 0)
        {
            if (PrintDebugMessages)
                Debug.Log("Waiting for LSL marker stream: " + MarkerStreamName);

            yield return new WaitForSeconds(0.5f);
            results = markerResolver.results();
        }

        markerInlet = new StreamInlet(results[0]);

        int nChannels = markerInlet.info().channel_count();
        markerSample = new string[nChannels];

        Debug.Log(
            "Connected to marker stream: " +
            MarkerStreamName +
            " | channels: " + nChannels +
            " | nominal rate: " + markerInlet.info().nominal_srate()
        );
    }

    void Update()
    {
        ReadMarkerStream();
    }

    private void ReadMarkerStream()
    {
        if (markerInlet == null || markerSample == null)
            return;

        for (int i = 0; i < MaxMarkersPerFrame; i++)
        {
            double timestamp = markerInlet.pull_sample(markerSample, 0.0);

            if (timestamp == 0.0)
                break;

            for (int channel = 0; channel < markerSample.Length; channel++)
            {
                string rawMarker = markerSample[channel];

                if (!string.IsNullOrWhiteSpace(rawMarker))
                    HandleMarker(rawMarker, timestamp);

                markerSample[channel] = null;
            }
        }
    }

    private void HandleMarker(string rawMarker, double timestamp)
    {
        if (!TryParseMarkerCode(rawMarker, out int markerCode))
        {
            if (PrintDebugMessages)
                Debug.LogWarning("[MARKER] Could not parse raw marker: " + rawMarker);

            return;
        }

        if (IgnoreZeroMarkers && markerCode == 0)
        {
            if (PrintDebugMessages)
                Debug.Log("[MARKER] Ignored zero marker: " + rawMarker);

            return;
        }

        LatestRawMarker = rawMarker;
        LatestMarkerCode = markerCode;
        LatestMarkerTimestamp = timestamp;

        MarkerReceived?.Invoke(markerCode, timestamp);
        OnAnyMarkerCode?.Invoke(markerCode);
        OnAnyRawMarker?.Invoke(rawMarker);

        bool handled = true;

        if (markerCode == ExperimentStartCode)
        {
            CurrentPhase = BCITrialPhase.None;
            CurrentDirection = BCIDirection.None;
            IsInTrial = false;
            IsFeedbackActive = false;

            // OnExperimentStart?.Invoke();
        }
        else if (markerCode == TrialStartCode)
        {
            
            CurrentPhase = BCITrialPhase.TrialStarted;
            CurrentDirection = BCIDirection.None;
            IsInTrial = true;
            IsFeedbackActive = false;

            // OnTrialStart?.Invoke();
            TrialStartReceived?.Invoke(timestamp);

            cross.SetActive(true);
            rightCup.SetActive(false);
            leftCup.SetActive(false);
            rightHandAnimator.Play("Right Hand - Rest to Lift", 0, 0f);
            // rightHandAnimator.Update(0f);
            rightHandAnimator.speed = 0f;
            leftHandAnimator.Play("Left Hand - Rest to Lift", 0, 0f);
            //leftHandAnimator.Update(0f);
            leftHandAnimator.speed = 0f;
        }
        else if (markerCode == CrossOnScreenCode)
        {
            CurrentPhase = BCITrialPhase.CrossOnScreen;

            // OnCrossOnScreen?.Invoke();
        }
        else if (markerCode == BeepCode)
        {
            CurrentPhase = BCITrialPhase.Beep;

            // OnBeep?.Invoke();
        }
        else if (markerCode == LeftCueCode)
        {
            CurrentPhase = BCITrialPhase.CueShown;
            CurrentDirection = BCIDirection.Left;

            // OnLeftCue?.Invoke();
            // OnAnyCue?.Invoke();
            LeftCueReceived?.Invoke(timestamp);

            cross.SetActive(false);
            leftCup.SetActive(true);
            rightCup.SetActive(false);
            rightHandAnimator.speed = 0f;
            leftHandAnimator.speed = 0f;
            isLeftSide = true;
        }
        else if (markerCode == RightCueCode)
        {
            
            CurrentPhase = BCITrialPhase.CueShown;
            CurrentDirection = BCIDirection.Right;

            // OnRightCue?.Invoke();
            // OnAnyCue?.Invoke();
            RightCueReceived?.Invoke(timestamp);

            cross.SetActive(false);
            leftCup.SetActive(false);
            rightCup.SetActive(true);
            rightHandAnimator.speed = 0f;
            leftHandAnimator.speed = 0f;
            isLeftSide = false;
        }
        else if (markerCode == FeedbackStartCode)
        {
            CurrentPhase = BCITrialPhase.Feedback;
            IsFeedbackActive = true;

            // OnFeedbackStart?.Invoke();
            FeedbackStartReceived?.Invoke(timestamp);

            if (isLeftSide)
            {
                leftHandAnimator.speed = 1.0f;
            }
            else
            {
                rightHandAnimator.speed = 1.0f;
            }
        }
        else if (markerCode == TrialEndCode)
        {
            CurrentPhase = BCITrialPhase.TrialEnded;
            IsInTrial = false;
            IsFeedbackActive = false;

            // OnTrialEnd?.Invoke();
            TrialEndReceived?.Invoke(timestamp);

            rightHandAnimator.Play("Right Hand - Rest to Lift", 0, 0f);
            rightHandAnimator.speed = 0f;
            //rightHandAnimator.Update(0f);
            leftHandAnimator.Play("Left Hand - Rest to Lift", 0, 0f);
            leftHandAnimator.speed = 0f;
            //leftHandAnimator.Update(0f);
        }
        else if (markerCode == SessionEndCode)
        {
            CurrentPhase = BCITrialPhase.SessionEnded;
            CurrentDirection = BCIDirection.None;
            IsInTrial = false;
            IsFeedbackActive = false;

            // OnSessionEnd?.Invoke();
        }
        else if (markerCode == TrainCode)
        {
            CurrentPhase = BCITrialPhase.Training;

            // OnTrain?.Invoke();
        }
        else if (markerCode == ExperimentStopCode)
        {
            CurrentPhase = BCITrialPhase.ExperimentStopped;
            CurrentDirection = BCIDirection.None;
            IsInTrial = false;
            IsFeedbackActive = false;

            // OnExperimentStop?.Invoke();
        }
        else
        {
            handled = false;
            OnUnhandledMarkerCode?.Invoke(markerCode);
        }

        if (PrintDebugMessages)
        {
            Debug.Log(
                "[MARKER] raw = " + rawMarker +
                " | code = " + markerCode +
                " | phase = " + CurrentPhase +
                " | direction = " + CurrentDirection +
                " | inTrial = " + IsInTrial +
                " | feedback = " + IsFeedbackActive +
                " | timestamp = " + timestamp.ToString("F6", CultureInfo.InvariantCulture) +
                (handled ? "" : " | UNHANDLED")
            );
        }
    }

    private bool TryParseMarkerCode(string rawMarker, out int markerCode)
    {
        markerCode = -1;

        if (string.IsNullOrWhiteSpace(rawMarker))
            return false;

        string text = rawMarker.Trim();

        if (text.StartsWith("[") && text.EndsWith("]"))
            text = text.Substring(1, text.Length - 2).Trim();

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out markerCode))
            return true;

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double markerAsDouble))
        {
            double rounded = Math.Round(markerAsDouble);

            if (Math.Abs(markerAsDouble - rounded) < 0.000001)
            {
                markerCode = (int)rounded;
                return true;
            }
        }

        MatchCollection matches = Regex.Matches(rawMarker, @"-?\d+");

        if (matches.Count > 0)
        {
            string lastNumber = matches[matches.Count - 1].Value;
            return int.TryParse(lastNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out markerCode);
        }

        return false;
    }

    private void OnDestroy()
    {
        if (markerInlet != null)
            markerInlet.close_stream();
    }
}