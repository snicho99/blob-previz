using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Applies configurable corruption to clean bounding-box data before OSC send.
    /// Attach alongside OscEmitter.
    /// </summary>
    public class TrackingNoiseLayer : MonoBehaviour
    {
        SimulationConfig _config;

        void Awake()
        {
            _config = GetComponent<OscEmitter>()?.config
                   ?? FindFirstObjectByType<SimulationManager>()?.Config;
        }

        /// <summary>
        /// Applies noise in-place to bounding-box edges.
        /// Centroid jitter from the old protocol has been removed — centroid is no longer a sent field.
        /// </summary>
        public void Apply(ref float xMin, ref float yMin, ref float xMax, ref float yMax)
        {
            if (_config == null || !_config.enableBboxNoise) return;

            float n = _config.bboxEdgeNoise;
            xMin += Random.Range(-n, n);
            yMin += Random.Range(-n, n);
            xMax += Random.Range(-n, n);
            yMax += Random.Range(-n, n);
        }
    }
}
