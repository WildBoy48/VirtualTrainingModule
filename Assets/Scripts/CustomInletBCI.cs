using System.Collections;
using UnityEngine;
using LSL;

namespace LSL4Unity.Samples.SimpleInlet
{
    public class CustomInletBCI : MonoBehaviour
    {
        [Header("LSL stream name")]
        public string ConfidenceStreamName = "ConfidenceLevel";

        [Header("Control settings")]
        public float ActionThreshold = 0.2f;
        public float MovementSpeed = 1.0f;
        public bool PrintDebugMessages = true;

        private ContinuousResolver confidenceResolver;
        private StreamInlet confidenceInlet;

        private float[,] confidenceBuffer;
        private double[] confidenceTimestampBuffer;

        private double maxChunkDuration = 0.2;

        public float LatestSignedConfidence { get; private set; } = 0.0f;
        public float LatestConfidenceMagnitude { get; private set; } = 0.0f;
        public string LatestDirection { get; private set; } = "none";

        void Start()
        {
            if (string.IsNullOrEmpty(ConfidenceStreamName))
            {
                Debug.LogError("ConfidenceStreamName is empty.");
                enabled = false;
                return;
            }

            confidenceResolver = new ContinuousResolver("name", ConfidenceStreamName);
            StartCoroutine(ResolveConfidenceStream());
        }

        IEnumerator ResolveConfidenceStream()
        {
            var results = confidenceResolver.results();

            while (results.Length == 0)
            {
                yield return new WaitForSeconds(0.1f);
                results = confidenceResolver.results();
            }

            confidenceInlet = new StreamInlet(results[0]);

            int nChannels = confidenceInlet.info().channel_count();
            double nominalRate = confidenceInlet.info().nominal_srate();

            int bufferSamples;

            if (nominalRate > 0)
            {
                bufferSamples = Mathf.Max(
                    1,
                    Mathf.CeilToInt((float)(nominalRate * maxChunkDuration))
                );
            }
            else
            {
                bufferSamples = 32;
            }

            confidenceBuffer = new float[bufferSamples, nChannels];
            confidenceTimestampBuffer = new double[bufferSamples];

            Debug.Log(
                "Connected to confidence stream: " +
                ConfidenceStreamName +
                " | channels: " + nChannels +
                " | rate: " + nominalRate
            );
        }

        void Update()
        {
            ReadConfidenceStream();
            ApplyBCICommand();
        }

        private void ReadConfidenceStream()
        {
            if (confidenceInlet == null)
                return;

            int samplesReturned = confidenceInlet.pull_chunk(
                confidenceBuffer,
                confidenceTimestampBuffer
            );

            if (samplesReturned <= 0)
                return;

            float signedConfidence = confidenceBuffer[samplesReturned - 1, 0];

            LatestSignedConfidence = signedConfidence;
            LatestConfidenceMagnitude = Mathf.Abs(signedConfidence);

            if (signedConfidence >= ActionThreshold)
            {
                LatestDirection = "right";
            }
            else if (signedConfidence <= -ActionThreshold)
            {
                LatestDirection = "left";
            }
            else
            {
                LatestDirection = "none";
            }

            if (PrintDebugMessages)
            {
                Debug.Log(
                    "[CONFIDENCE] value = " +
                    LatestSignedConfidence.ToString("F3") +
                    " | direction = " +
                    LatestDirection +
                    " | magnitude = " +
                    LatestConfidenceMagnitude.ToString("F3")
                );
            }
        }

        private void ApplyBCICommand()
        {
            if (LatestDirection == "left")
            {
                transform.Translate(Vector3.left * MovementSpeed * Time.deltaTime);
            }
            else if (LatestDirection == "right")
            {
                transform.Translate(Vector3.right * MovementSpeed * Time.deltaTime);
            }
        }
    }
}