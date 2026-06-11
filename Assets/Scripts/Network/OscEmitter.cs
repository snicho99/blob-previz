using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Implements the Blob V1.1 OSC protocol. See BLOB_PROTOCOL.md for the full spec.
    ///
    /// Each LateUpdate frame:
    ///   - All active-tracker messages are packed into a single OSC bundle.
    ///   - Tracker transitions are detected against last-frame state:
    ///       inactive → active  : /blob/join
    ///       active   → active  : /blob/move
    ///       active   → inactive: /blob/exit   (always sent, never dropped)
    ///   - Suppressed trackers (MergeSimulator) are skipped entirely — no exit sent.
    ///   - Frame drops (if enabled in SimulationConfig) apply to join/move only.
    ///
    /// Execution order -1 on MergeSimulator ensures it runs before this component's
    /// LateUpdate so bbox overrides and suppression flags are set before we read them.
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class OscEmitter : MonoBehaviour
    {
        public SimulationConfig config;

        OscBundleClient _bundle;
        List<PersonTracker> _trackers = new List<PersonTracker>();
        TrackingNoiseLayer _noise;

        /// <summary>Session-level tracker GUID — fixed for the lifetime of the play session.</summary>
        string _sessionGuid;

        /// <summary>Trackers that were active at the end of the previous frame.</summary>
        readonly HashSet<PersonTracker> _activeLastFrame = new HashSet<PersonTracker>();

        void Start()
        {
            _sessionGuid = Guid.NewGuid().ToString();

            string ip   = ConfigManager.Instance?.OscTargetIp ?? config?.oscTargetIp ?? "127.0.0.1";
            int    port = ConfigManager.Instance != null ? ConfigManager.Instance.OscPort : (config != null ? config.oscPort : 9000);
            _bundle = new OscBundleClient(ip, port);

            RefreshTrackers();
            _noise = GetComponent<TrackingNoiseLayer>();

            if (ConfigManager.Instance != null)
                ConfigManager.Instance.OscSettingsChanged += OnOscSettingsChanged;

            Debug.Log($"[OscEmitter] Session GUID: {_sessionGuid} → {ip}:{port}");
        }

        /// <summary>
        /// Re-scans the scene for PersonTrackers. Call after spawning or despawning NPCs.
        /// Exits are sent on the next LateUpdate for any active tracker that was removed.
        /// </summary>
        public void RefreshTrackers()
        {
            _trackers = new List<PersonTracker>(FindObjectsByType<PersonTracker>(FindObjectsSortMode.None));
        }

        void OnOscSettingsChanged()
            => Reconnect(ConfigManager.Instance.OscTargetIp, ConfigManager.Instance.OscPort);

        public void Reconnect(string ip, int port)
        {
            _bundle?.Reconnect(ip, port);
            Debug.Log($"[OscEmitter] Reconnected → {ip}:{port}");
        }

        void LateUpdate()
        {
            _bundle.BeginBundle();

            var activeThisFrame = new HashSet<PersonTracker>();
            var currentSet      = new HashSet<PersonTracker>(_trackers);

            foreach (var tracker in _trackers)
            {
                if (tracker == null || tracker.IsSuppressed) continue;

                bool wasActive = _activeLastFrame.Contains(tracker);

                if (tracker.IsActive)
                {
                    activeThisFrame.Add(tracker);

                    // Frame drop applies to join/move only — exits are always reliable.
                    if (config != null && config.enableFrameDrops && UnityEngine.Random.value < config.frameDropProbability)
                        continue;

                    float xMin = tracker.XMin, yMin = tracker.YMin;
                    float xMax = tracker.XMax, yMax = tracker.YMax;
                    _noise?.Apply(ref xMin, ref yMin, ref xMax, ref yMax);

                    string address = wasActive ? "/blob/move" : "/blob/join";
                    _bundle.AddJoinMove(address, _sessionGuid, tracker.BlobId,
                        tracker.TrackedTime, xMin, yMin, xMax, yMax);
                }
                else if (wasActive)
                {
                    _bundle.AddExit(_sessionGuid, tracker.BlobId);
                }
            }

            // Send exits for any tracker that was active but has since been removed
            // (NPC despawned). If the object was destroyed (t == null), BlobId is
            // unrecoverable — exit is silently dropped; acceptable for forced despawn.
            foreach (var t in _activeLastFrame)
            {
                if (t != null && !currentSet.Contains(t))
                    _bundle.AddExit(_sessionGuid, t.BlobId);
            }

            if (_bundle.HasMessages)
                _bundle.Send();

            _activeLastFrame.Clear();
            _activeLastFrame.UnionWith(activeThisFrame);
        }

        void OnDestroy()
        {
            if (ConfigManager.Instance != null)
                ConfigManager.Instance.OscSettingsChanged -= OnOscSettingsChanged;
            _bundle?.Dispose();
        }
    }
}
