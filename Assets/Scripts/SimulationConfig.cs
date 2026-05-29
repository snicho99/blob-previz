using UnityEngine;

namespace BlobPreviz
{
    [CreateAssetMenu(menuName = "BlobPreviz/Simulation Config")]
    public class SimulationConfig : ScriptableObject
    {
        [Header("OSC")]
        public string oscTargetIp = "127.0.0.1";
        public int oscPort = 9000;

        [Header("Capture Volume")]
        [Tooltip("World-space bounds of the RealSense capture volume. The overhead camera frustum should match this exactly.")]
        public Bounds captureVolume = new Bounds(Vector3.zero, new Vector3(4f, 3f, 4f));

        [Header("Noise — Centroid")]
        public bool enableCentroidJitter = false;
        [Range(0f, 0.02f)] public float centroidJitterRadius = 0.005f;

        [Header("Noise — Bounding Box")]
        public bool enableBboxNoise = false;
        [Range(0f, 0.02f)] public float bboxEdgeNoise = 0.005f;

        [Header("Noise — Frame Drops")]
        public bool enableFrameDrops = false;
        [Range(0f, 0.3f)] public float frameDropProbability = 0.05f;

        [Header("Noise — Merge")]
        public bool enableMergeSimulation = true;
        [Tooltip("Viewport-space distance below which two people collapse to one blob.")]
        [Range(0f, 0.15f)] public float mergeThreshold = 0.08f;

        [Header("Noise — ID Reassignment")]
        [Tooltip("When someone re-enters the volume, assign a new UUID instead of reusing the old one.")]
        public bool reassignIdOnReentry = true;
    }
}
