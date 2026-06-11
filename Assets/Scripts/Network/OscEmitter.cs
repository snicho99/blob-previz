using UnityEngine;
using OscJack;

namespace BlobPreviz
{
    /// <summary>
    /// Reads PersonTracker data each frame, applies noise, and fires OSC messages.
    ///
    /// Address pattern:
    ///   /blob/tracker/{uuid}/confidence   float  0-1
    ///   /blob/tracker/{uuid}/trackedtime  float  seconds
    ///   /blob/tracker/{uuid}/bb/top       float  0-1 viewport, Y-down
    ///   /blob/tracker/{uuid}/bb/right     float
    ///   /blob/tracker/{uuid}/bb/bottom    float
    ///   /blob/tracker/{uuid}/bb/left      float
    ///   /blob/tracker/{uuid}/x            float  centroid, 0-1
    ///   /blob/tracker/{uuid}/y            float
    ///
    /// When a person leaves the capture volume, their UUID simply goes silent.
    /// The downstream experience owns the timeout/removal decision.
    ///</summary>
    public class OscEmitter : MonoBehaviour
    {
        public SimulationConfig config;

        private OscClient _client;
        private PersonTracker[] _trackers;
        private TrackingNoiseLayer _noise;

        void Start()
        {
            string ip   = ConfigManager.Instance != null ? ConfigManager.Instance.OscTargetIp : config.oscTargetIp;
            int    port = ConfigManager.Instance != null ? ConfigManager.Instance.OscPort      : config.oscPort;
            _client = new OscClient(ip, port);
            _trackers = FindObjectsByType<PersonTracker>(FindObjectsSortMode.None);
            _noise = GetComponent<TrackingNoiseLayer>();

            if (ConfigManager.Instance != null)
                ConfigManager.Instance.OscSettingsChanged += OnOscSettingsChanged;
        }

        void OnOscSettingsChanged()
        {
            Reconnect(ConfigManager.Instance.OscTargetIp, ConfigManager.Instance.OscPort);
        }

        public void Reconnect(string ip, int port)
        {
            _client?.Dispose();
            _client = new OscClient(ip, port);
            Debug.Log($"[OscEmitter] Reconnected → {ip}:{port}");
        }

        void LateUpdate()
        {
            foreach (var tracker in _trackers)
            {
                if (!tracker.IsActive) continue;

                if (config.enableFrameDrops && Random.value < config.frameDropProbability)
                    continue;

                float cx = tracker.Centroid.x;
                float cy = tracker.Centroid.y;
                float top    = tracker.BbTop;
                float right  = tracker.BbRight;
                float bottom = tracker.BbBottom;
                float left   = tracker.BbLeft;
                float confidence = tracker.Confidence;

                if (_noise != null)
                    _noise.Apply(ref cx, ref cy, ref top, ref right, ref bottom, ref left, ref confidence);

                SendBlob(tracker.TrackerId, cx, cy, top, right, bottom, left, confidence, tracker.TrackedTime);
            }
        }

        public void SendBlob(string id,
            float cx, float cy,
            float top, float right, float bottom, float left,
            float confidence, float trackedTime)
        {
            string prefix = $"/blob/tracker/{id}";
            _client.Send(prefix + "/confidence",   confidence);
            _client.Send(prefix + "/trackedtime",  trackedTime);
            _client.Send(prefix + "/bb/top",       top);
            _client.Send(prefix + "/bb/right",     right);
            _client.Send(prefix + "/bb/bottom",    bottom);
            _client.Send(prefix + "/bb/left",      left);
            _client.Send(prefix + "/x",            cx);
            _client.Send(prefix + "/y",            cy);
        }

        // No removal message. When a person leaves the capture volume, messages for
        // their UUID simply stop. The downstream experience owns the timeout logic.

        void OnDestroy()
        {
            if (ConfigManager.Instance != null)
                ConfigManager.Instance.OscSettingsChanged -= OnOscSettingsChanged;
            _client?.Dispose();
        }
    }
}
