using System.Collections.Generic;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// When two tracked people come within mergeThreshold (viewport space),
    /// suppress both individual emits and fire a single merged blob instead.
    /// Attach alongside OscEmitter.
    /// </summary>
    public class MergeSimulator : MonoBehaviour
    {
        private SimulationConfig _config;
        private OscEmitter _emitter;
        private PersonTracker[] _trackers;

        void Start()
        {
            _config = GetComponent<OscEmitter>()?.config
                   ?? FindFirstObjectByType<SimulationManager>()?.Config;
            _emitter = GetComponent<OscEmitter>();
            _trackers = FindObjectsByType<PersonTracker>(FindObjectsSortMode.None);
        }

        void LateUpdate()
        {
            if (_config == null || !_config.enableMergeSimulation) return;

            // Reset merge state first.
            foreach (var t in _trackers)
                if (t.IsActive) t.SetMerged(false);

            var merged = new HashSet<PersonTracker>();

            for (int i = 0; i < _trackers.Length; i++)
            {
                if (!_trackers[i].IsActive || merged.Contains(_trackers[i])) continue;

                for (int j = i + 1; j < _trackers.Length; j++)
                {
                    if (!_trackers[j].IsActive || merged.Contains(_trackers[j])) continue;

                    float dist = Vector2.Distance(_trackers[i].Centroid, _trackers[j].Centroid);
                    if (dist < _config.mergeThreshold)
                    {
                        merged.Add(_trackers[i]);
                        merged.Add(_trackers[j]);
                        EmitMergedBlob(_trackers[i], _trackers[j]);
                    }
                }
            }

            // Suppress individual emits for merged pairs.
            foreach (var t in merged)
                t.SetMerged(true);
        }

        void EmitMergedBlob(PersonTracker a, PersonTracker b)
        {
            // Use whichever ID has been active longer (simulates tracker picking the dominant blob).
            string id = a.TrackedTime >= b.TrackedTime ? a.TrackerId : b.TrackerId;

            float cx = (a.Centroid.x + b.Centroid.x) * 0.5f;
            float cy = (a.Centroid.y + b.Centroid.y) * 0.5f;
            float top    = Mathf.Min(a.BbTop,    b.BbTop);
            float left   = Mathf.Min(a.BbLeft,   b.BbLeft);
            float bottom = Mathf.Max(a.BbBottom,  b.BbBottom);
            float right  = Mathf.Max(a.BbRight,   b.BbRight);
            float trackedTime = Mathf.Max(a.TrackedTime, b.TrackedTime);

            // Delegates to OscEmitter.SendBlob — the public method handles formatting and send.
            _emitter.SendBlob(id, cx, cy, top, right, bottom, left, 1f, trackedTime);
        }
    }
}
