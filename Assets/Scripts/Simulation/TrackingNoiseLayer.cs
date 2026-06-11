using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Applies configurable corruption to clean tracker data before OSC send.
    /// Attach alongside OscEmitter.
    /// </summary>
    public class TrackingNoiseLayer : MonoBehaviour
    {
        private SimulationConfig _config;

        void Awake()
        {
            _config = GetComponent<OscEmitter>()?.config
                   ?? FindFirstObjectByType<SimulationManager>()?.Config;
        }

        public void Apply(
            ref float cx, ref float cy,
            ref float top, ref float right, ref float bottom, ref float left,
            ref float confidence)
        {
            if (_config == null) return;

            if (_config.enableCentroidJitter)
            {
                var jitter = Random.insideUnitCircle * _config.centroidJitterRadius;
                cx += jitter.x;
                cy += jitter.y;
            }

            if (_config.enableBboxNoise)
            {
                float n = _config.bboxEdgeNoise;
                top    += Random.Range(-n, n);
                bottom += Random.Range(-n, n);
                left   += Random.Range(-n, n);
                right  += Random.Range(-n, n);
            }

            // Confidence drops near capture-volume edges; noise brings it below 1.
            // For now: full confidence unless explicitly degraded by merge simulation.
        }
    }
}
