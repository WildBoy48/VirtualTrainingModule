using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using LSL;

public enum BCIOnlinePhase
{
    Idle,
    TrialStarted,
    CrossOnScreen,
    Beep,
    CueShown,
    Feedback,
    TrialEnded,
    SessionEnded,
    ExperimentStopped
}

public enum BCIOnlineDirection
{
    None,
    Left,
    Right
}

public class CustomInletOnline : MonoBehaviour
{
    [Header("LSL streams")]
    public string MarkerStreamName = "openvibeMarkers";
    public string ConfidenceStreamName = "ConfidenceLevel";

    [Header("Observed OpenViBE / Graz marker codes")]
    public int TrialStartCode = 768;
    public int CrossOnScreenCode = 786;
    public int BeepCode = 33282;
    public int LeftCueCode = 769;
    public int RightCueCode = 770;
    public int FeedbackStartCode = 781;
    public int TrialEndCode = 800;
    public int SessionEndCode = 1010;
    public int ExperimentStartCode = 32769;
    public int ExperimentStopCode = 32770;

    [Header("Confidence gating")]
    [Tooltip("Confidence stream must send signed values in the range -1.0 to +1.0.")]
    public bool PositiveConfidenceMeansRight = true;

    [Range(0.0f, 1.0f)]
    public float ActivationConfidence = 0.30f;

    [Tooltip("A confidence sample is ignored after this many seconds.")]
    public float MaxConfidenceSampleAgeSeconds = 0.75f;

    [Header("Animation speed scaling")]
    public float MinAnimatorSpeed = 0.50f;
    public float MaxAnimatorSpeed = 2.00f;

    [Header("Reset behaviour")]
    [Tooltip("Recommended ON. Uses Animator.Rebind plus Play(0) to force a full visual reset.")]
    public bool UseAnimatorRebindOnReset = true;

    [Tooltip("Recommended ON. Restores the initial local position/rotation/scale of hands and cups.")]
    public bool RestoreInitialTransformsOnReset = true;

    [Tooltip("Recommended ON. Allows inactive cup objects to be reset before they are shown again.")]
    public bool TemporarilyActivateObjectsForReset = true;

    [Header("Scoreboard visuals")]
    [Tooltip("If true, the scoreBoard object is shown.")]
    public bool ShowScoreBoard = true;

    [Tooltip("If true, the scoreBoard object is hidden when marker 800 / TrialEnd is received.")]
    public bool HideScoreBoardOnTrialEnd = false;

    [Header("Scoring")]
    public int PointsPerCompletedCycle = 100;

    [Tooltip("If true, ScoreManager is reset when a new left/right cue appears.")]
    public bool ResetScoreOnNewCue = false;

    [Header("Controlled objects")]
    [SerializeField] private GameObject rightHand;
    [SerializeField] private GameObject leftHand;
    [SerializeField] private GameObject rightCup;
    [SerializeField] private GameObject leftCup;
    [SerializeField] private GameObject cross;
    [SerializeField] private GameObject scoreBoard;

    [Header("Animator state names")]
    public string RightAnimationStateName = "Right Hand - Rest to Lift";
    public string LeftAnimationStateName = "Left Hand - Rest to Lift";

    [Header("Trial visuals")]
    public bool HideCueOnTrialEnd = true;

    [Header("Debug")]
    public bool PrintDebugMessages = true;
    public bool IgnoreZeroMarkers = true;
    public int MaxSamplesPerFrame = 64;

    public BCIOnlinePhase CurrentPhase { get; private set; } = BCIOnlinePhase.Idle;
    public BCIOnlineDirection CurrentDirection { get; private set; } = BCIOnlineDirection.None;
    public float LatestSignedConfidence { get; private set; } = 0.0f;
    public float LatestAbsoluteConfidence { get; private set; } = 0.0f;
    public float LatestDirectionalConfidence { get; private set; } = 0.0f;

    private Animator rightHandAnimator;
    private Animator leftHandAnimator;
    private Animator rightCupAnimator;
    private Animator leftCupAnimator;

    private ObjectPose rightHandInitialPose;
    private ObjectPose leftHandInitialPose;
    private ObjectPose rightCupInitialPose;
    private ObjectPose leftCupInitialPose;

    private ContinuousResolver markerResolver;
    private ContinuousResolver confidenceResolver;

    private StreamInlet markerInlet;
    private StreamInlet confidenceInlet;

    private string[] markerSample;
    private float[] confidenceSample;

    private bool hasConfidence = false;
    private float latestConfidenceReceiveTime = -9999.0f;

    private struct ObjectPose
    {
        public bool IsValid;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;

        public ObjectPose(Transform target)
        {
            if (target == null)
            {
                IsValid = false;
                LocalPosition = Vector3.zero;
                LocalRotation = Quaternion.identity;
                LocalScale = Vector3.one;
                return;
            }

            IsValid = true;
            LocalPosition = target.localPosition;
            LocalRotation = target.localRotation;
            LocalScale = target.localScale;
        }

        public void Restore(Transform target)
        {
            if (!IsValid || target == null)
                return;

            target.localPosition = LocalPosition;
            target.localRotation = LocalRotation;
            target.localScale = LocalScale;
        }
    }

    private void Awake()
    {
        if (rightHand != null)
            rightHandAnimator = rightHand.GetComponent<Animator>();

        if (leftHand != null)
            leftHandAnimator = leftHand.GetComponent<Animator>();

        if (rightCup != null)
            rightCupAnimator = rightCup.GetComponent<Animator>();

        if (leftCup != null)
            leftCupAnimator = leftCup.GetComponent<Animator>();

        CaptureInitialObjectPoses();

        ForceResetBothSides();

        SetActiveSafe(cross, false);
        SetActiveSafe(rightCup, false);
        SetActiveSafe(leftCup, false);
        SetActiveSafe(scoreBoard, true);

        scoreBoard.SetActive(ShowScoreBoard);
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(MarkerStreamName))
        {
            Debug.LogError("MarkerStreamName is empty.");
            enabled = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(ConfidenceStreamName))
        {
            Debug.LogError("ConfidenceStreamName is empty.");
            enabled = false;
            return;
        }

        markerResolver = new ContinuousResolver("name", MarkerStreamName);
        confidenceResolver = new ContinuousResolver("name", ConfidenceStreamName);

        StartCoroutine(ResolveMarkerStream());
        StartCoroutine(ResolveConfidenceStream());
    }

    private IEnumerator ResolveMarkerStream()
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
        markerSample = new string[markerInlet.info().channel_count()];

        Debug.Log("Connected to marker stream: " + MarkerStreamName +
                  " | channels: " + markerSample.Length);
    }

    private IEnumerator ResolveConfidenceStream()
    {
        var results = confidenceResolver.results();

        while (results.Length == 0)
        {
            if (PrintDebugMessages)
                Debug.Log("Waiting for LSL confidence stream: " + ConfidenceStreamName);

            yield return new WaitForSeconds(0.5f);
            results = confidenceResolver.results();
        }

        confidenceInlet = new StreamInlet(results[0]);
        confidenceSample = new float[confidenceInlet.info().channel_count()];

        Debug.Log("Connected to confidence stream: " + ConfidenceStreamName +
                  " | channels: " + confidenceSample.Length);
    }

    private void Update()
    {
        ReadMarkerStream();
        ReadConfidenceStream();
        ApplyOnlineFeedback();
    }

    private void ReadMarkerStream()
    {
        if (markerInlet == null || markerSample == null)
            return;

        for (int i = 0; i < MaxSamplesPerFrame; i++)
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

    private void ReadConfidenceStream()
    {
        if (confidenceInlet == null || confidenceSample == null)
            return;

        for (int i = 0; i < MaxSamplesPerFrame; i++)
        {
            double timestamp = confidenceInlet.pull_sample(confidenceSample, 0.0);

            if (timestamp == 0.0)
                break;

            HandleConfidence(confidenceSample[0], timestamp);
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
            return;

        if (markerCode == ExperimentStartCode)
        {
            CurrentPhase = BCIOnlinePhase.Idle;
            CurrentDirection = BCIOnlineDirection.None;
            
            EndFeedback(true);
        }
        else if (markerCode == TrialStartCode)
        {
            CurrentPhase = BCIOnlinePhase.TrialStarted;
            CurrentDirection = BCIOnlineDirection.None;
            ClearConfidence();

            ForceResetBothSides();

            SetActiveSafe(cross, true);
            SetActiveSafe(leftCup, false);
            SetActiveSafe(rightCup, false);
        }
        else if (markerCode == CrossOnScreenCode)
        {
            CurrentPhase = BCIOnlinePhase.CrossOnScreen;
            SetActiveSafe(cross, true);
        }
        else if (markerCode == BeepCode)
        {
            CurrentPhase = BCIOnlinePhase.Beep;
        }
        else if (markerCode == LeftCueCode)
        {
            ShowCue(BCIOnlineDirection.Left);
        }
        else if (markerCode == RightCueCode)
        {
            ShowCue(BCIOnlineDirection.Right);
        }
        else if (markerCode == FeedbackStartCode)
        {
            BeginFeedback();
        }
        else if (markerCode == TrialEndCode)
        {
            CurrentPhase = BCIOnlinePhase.TrialEnded;
            EndFeedback(HideCueOnTrialEnd);
        }
        else if (markerCode == SessionEndCode)
        {
            CurrentPhase = BCIOnlinePhase.SessionEnded;
            CurrentDirection = BCIOnlineDirection.None;
            EndFeedback(true);
        }
        else if (markerCode == ExperimentStopCode)
        {
            CurrentPhase = BCIOnlinePhase.ExperimentStopped;
            CurrentDirection = BCIOnlineDirection.None;
            EndFeedback(true);
        }

        if (PrintDebugMessages)
        {
            Debug.Log("[MARKER] raw = " + rawMarker +
                      " | code = " + markerCode +
                      " | phase = " + CurrentPhase +
                      " | cue = " + CurrentDirection +
                      " | score = " + GetCurrentScoreForLog() +
                      " | timestamp = " + timestamp.ToString("F6", CultureInfo.InvariantCulture));
        }
    }

    private void HandleConfidence(float rawConfidence, double timestamp)
    {
        LatestSignedConfidence = Mathf.Clamp(rawConfidence, -1.0f, 1.0f);
        LatestAbsoluteConfidence = Mathf.Abs(LatestSignedConfidence);

        float signedValue = PositiveConfidenceMeansRight
            ? LatestSignedConfidence
            : -LatestSignedConfidence;

        if (CurrentDirection == BCIOnlineDirection.Right)
            LatestDirectionalConfidence = signedValue;
        else if (CurrentDirection == BCIOnlineDirection.Left)
            LatestDirectionalConfidence = -signedValue;
        else
            LatestDirectionalConfidence = 0.0f;

        hasConfidence = true;
        latestConfidenceReceiveTime = Time.time;
    }

    private void ShowCue(BCIOnlineDirection direction)
    {
        CurrentPhase = BCIOnlinePhase.CueShown;
        CurrentDirection = direction;

        ForceResetBothSides();

        SetActiveSafe(cross, false);
        SetActiveSafe(leftCup, direction == BCIOnlineDirection.Left);
        SetActiveSafe(rightCup, direction == BCIOnlineDirection.Right);

        if (ResetScoreOnNewCue)
        {
            ResetScoreManager();
        }
    }

    private void BeginFeedback()
    {
        if (CurrentDirection == BCIOnlineDirection.None)
        {
            if (PrintDebugMessages)
                Debug.LogWarning("[ONLINE] Feedback marker received, but no left/right cue is active.");

            return;
        }

        CurrentPhase = BCIOnlinePhase.Feedback;

        if (PrintDebugMessages)
            Debug.Log("[ONLINE] Feedback started for " + CurrentDirection);
    }

    private void EndFeedback(bool hideCueObjects)
    {
        PauseBothAnimations();
        ForceResetBothSides();

        if (hideCueObjects)
        {
            SetActiveSafe(cross, false);
            SetActiveSafe(leftCup, false);
            SetActiveSafe(rightCup, false);
        }
    }

    private void ApplyOnlineFeedback()
    {
        if (CurrentPhase != BCIOnlinePhase.Feedback)
            return;

        Animator activeAnimator = GetActiveAnimator();
        string activeStateName = GetActiveAnimationStateName();

        if (activeAnimator == null || string.IsNullOrWhiteSpace(activeStateName))
            return;

        PauseInactiveAnimator();

       

        float signedValue = PositiveConfidenceMeansRight
            ? LatestSignedConfidence
            : -LatestSignedConfidence;

        float directionalConfidence = CurrentDirection == BCIOnlineDirection.Right
            ? signedValue
            : -signedValue;

        LatestDirectionalConfidence = directionalConfidence;

        bool correctDirectionAndStrongEnough = directionalConfidence >= ActivationConfidence;

        if (!correctDirectionAndStrongEnough)
        {
            ForceResetActiveSide();
            return;
        }

        EnsureAnimationStateIsPlaying(activeAnimator, activeStateName);

        float t = Mathf.InverseLerp(ActivationConfidence, 1.0f, directionalConfidence);
        activeAnimator.speed = Mathf.Lerp(MinAnimatorSpeed, MaxAnimatorSpeed, t);

        CheckForCompletedAnimationCycle(activeAnimator, activeStateName);
    }

    private void EnsureAnimationStateIsPlaying(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(stateName))
        {
            animator.Play(stateName, 0, 0.0f);
            animator.Update(0.0f);
        }
    }

    private void CheckForCompletedAnimationCycle(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(stateName))
            return;

        if (stateInfo.normalizedTime >= 0.995f)
        {
            AddPoints(PointsPerCompletedCycle);

            ForceResetActiveSide();
        }
    }

    private Animator GetActiveAnimator()
    {
        if (CurrentDirection == BCIOnlineDirection.Left)
            return leftHandAnimator;

        if (CurrentDirection == BCIOnlineDirection.Right)
            return rightHandAnimator;

        return null;
    }

    private string GetActiveAnimationStateName()
    {
        if (CurrentDirection == BCIOnlineDirection.Left)
            return LeftAnimationStateName;

        if (CurrentDirection == BCIOnlineDirection.Right)
            return RightAnimationStateName;

        return "";
    }

    private void PauseInactiveAnimator()
    {
        if (CurrentDirection == BCIOnlineDirection.Left && rightHandAnimator != null)
            rightHandAnimator.speed = 0.0f;
        else if (CurrentDirection == BCIOnlineDirection.Right && leftHandAnimator != null)
            leftHandAnimator.speed = 0.0f;
    }

    private bool HasFreshConfidence()
    {
        return hasConfidence && (Time.time - latestConfidenceReceiveTime) <= MaxConfidenceSampleAgeSeconds;
    }

    private void ClearConfidence()
    {
        hasConfidence = false;
        latestConfidenceReceiveTime = -9999.0f;
        LatestSignedConfidence = 0.0f;
        LatestAbsoluteConfidence = 0.0f;
        LatestDirectionalConfidence = 0.0f;
    }

    private void CaptureInitialObjectPoses()
    {
        rightHandInitialPose = new ObjectPose(rightHand != null ? rightHand.transform : null);
        leftHandInitialPose = new ObjectPose(leftHand != null ? leftHand.transform : null);
        rightCupInitialPose = new ObjectPose(rightCup != null ? rightCup.transform : null);
        leftCupInitialPose = new ObjectPose(leftCup != null ? leftCup.transform : null);
    }

    private void ForceResetBothSides()
    {
        ForceResetSide(BCIOnlineDirection.Right);
        ForceResetSide(BCIOnlineDirection.Left);
    }

    private void ForceResetActiveSide()
    {
        ForceResetSide(CurrentDirection);
    }

    private void ForceResetSide(BCIOnlineDirection direction)
    {
        if (direction == BCIOnlineDirection.Right)
        {
            ForceResetVisualObject(rightHand, rightHandAnimator, RightAnimationStateName, rightHandInitialPose, true);
            ForceResetVisualObject(rightCup, rightCupAnimator, "", rightCupInitialPose, false);
        }
        else if (direction == BCIOnlineDirection.Left)
        {
            ForceResetVisualObject(leftHand, leftHandAnimator, LeftAnimationStateName, leftHandInitialPose, true);
            ForceResetVisualObject(leftCup, leftCupAnimator, "", leftCupInitialPose, false);
        }
    }

    private void ForceResetVisualObject(GameObject targetObject, Animator animator, string stateName, ObjectPose initialPose, bool playNamedState)
    {
        bool changedActiveState = false;
        bool originalActiveSelf = false;

        if (targetObject != null)
        {
            originalActiveSelf = targetObject.activeSelf;

            if (TemporarilyActivateObjectsForReset && !targetObject.activeInHierarchy)
            {
                targetObject.SetActive(true);
                changedActiveState = true;
            }
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 0.0f;

            if (UseAnimatorRebindOnReset)
            {
                animator.Rebind();
                animator.Update(0.0f);
            }

            if (playNamedState && !string.IsNullOrWhiteSpace(stateName))
            {
                animator.Play(stateName, 0, 0.0f);
                animator.Update(0.0f);
            }

            animator.speed = 0.0f;
        }

        if (RestoreInitialTransformsOnReset && targetObject != null)
            initialPose.Restore(targetObject.transform);

        if (changedActiveState && targetObject != null)
            targetObject.SetActive(originalActiveSelf);
    }

    private void PauseBothAnimations()
    {
        if (rightHandAnimator != null)
            rightHandAnimator.speed = 0.0f;

        if (leftHandAnimator != null)
            leftHandAnimator.speed = 0.0f;

        if (rightCupAnimator != null)
            rightCupAnimator.speed = 0.0f;

        if (leftCupAnimator != null)
            leftCupAnimator.speed = 0.0f;
    }

    private void AddPoints(int amount)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[SCORE] Cannot add points because ScoreManager.Instance is null.");
            return;
        }

        //ScoreManager.Instance.AddScore(amount);

        if (PrintDebugMessages)
        {
            Debug.Log("[SCORE] +" + amount +
                      " | total = " + ScoreManager.Instance.CurrentScore);
        }
    }

    private void ResetScoreManager()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[SCORE] Cannot reset score because ScoreManager.Instance is null.");
            return;
        }

        int currentScore = ScoreManager.Instance.CurrentScore;

        if (currentScore != 0)
        {
            ScoreManager.Instance.AddScore(-currentScore);
        }

        if (PrintDebugMessages)
        {
            Debug.Log("[SCORE] Score reset.");
        }
    }

    private int GetCurrentScoreForLog()
    {
        return ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
    }

    private void SetActiveSafe(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
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

        if (confidenceInlet != null)
            confidenceInlet.close_stream();
    }
}
