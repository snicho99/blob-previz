using System.Collections.Generic;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// When two tracked people come within mergeThreshold (normalised camera space),
    /// suppresses both individual trackers and promotes the dominant one with a merged bbox.
    ///
    /// Execution order -1 ensures this runs before OscEmitter.LateUpdate so suppression
    /// flags and bbox overrides are in place before messages are built.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class MergeSimulator : MonoBehaviour
    {
        SimulationConfig _config;
        PersonTracker[] _trackers;

        void Start()
        {
            _config   = GetComponent<OscEmitter>()?.config
                     ?? FindFirstObjectByType<SimulationManager>()?.Config;
            _trackers = FindObjectsByType<PersonTracker>(FindObjectsSortMode.None);
        }

        void LateUpdate()
        {
            if (_config == null || !_config.enableMergeSimulation) return;

            var merged = new HashSet<PersonTracker>();

            for (int i = 0; i < _trackers.Length; i++)
            {
                if (!_trackers[i].IsActive || merged.Contains(_trackers[i])) continue;

                for (int j = i + 1; j < _trackers.Length; j++)
                {
                    if (!_trackers[j].IsActive || merged.Contains(_trackers[j])) continue;

                    float dist = Vector2.Distance(_trackers[i].Center, _trackers[j].Center);
                    if (dist < _config.mergeThreshold)
                    {
                        merged.Add(_trackers[i]);
                        merged.Add(_trackers[j]);
                        ApplyMerge(_trackers[i], _trackers[j]);
                    }
                }
            }
        }

        void ApplyMerge(PersonTracker a, PersonTracker b)
        {
            // Dominant tracker (tracked longer) stays visible with merged bbox.
            // Subordinate is suppressed — no OSC messages of any kind this frame.
            PersonTracker dominant   = a.TrackedTime >= b.TrackedTime ? a : b;
            PersonTracker subordinate = dominant == a ? b : a;

            float xMin = Mathf.Min(a.XMin, b.XMin);
            float yMin = Mathf.Min(a.YMin, b.YMin);
            float xMax = Mathf.Max(a.XMax, b.XMax);
            float yMax = Mathf.Max(a.YMax, b.YMax);

            dominant.SetMergedBbox(xMin, yMin, xMax, yMax);
            subordinate.Suppress();
        }
    }
}
