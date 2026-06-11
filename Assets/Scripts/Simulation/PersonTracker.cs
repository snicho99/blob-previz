using System;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Attach to the capsule that owns world position for a simulated person.
    /// The rigged character mesh should be a child of this object.
    /// </summary>
    public class PersonTracker : MonoBehaviour
    {
        [Tooltip("The overhead orthographic camera that represents the RealSense view.")]
        public Camera overheadCamera;

        [Tooltip("Renderers contributing to this person's bounding box. Leave empty to auto-collect from children.")]
        public Renderer[] trackedRenderers;

        // Read by OscEmitter each frame.
        public string TrackerId { get; private set; }
        public bool IsActive { get; private set; }
        public float TrackedTime { get; private set; }

        // Viewport-space data (0-1, origin top-left, Y down).
        public Vector2 Centroid { get; private set; }
        public float BbTop { get; private set; }
        public float BbRight { get; private set; }
        public float BbBottom { get; private set; }
        public float BbLeft { get; private set; }
        public float Confidence { get; private set; }

        private SimulationConfig _config;
        private bool _wasInsideVolume;

        void Awake()
        {
            AssignNewId();
            _config = FindFirstObjectByType<SimulationManager>()?.Config;

            if (trackedRenderers == null || trackedRenderers.Length == 0)
                trackedRenderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            bool inside = IsInsideCaptureVolume();

            if (inside && !_wasInsideVolume)
            {
                // Entered volume.
                if (_config != null && _config.reassignIdOnReentry && _wasInsideVolume == false && TrackedTime > 0f)
                    AssignNewId();
                IsActive = true;
                TrackedTime = 0f;
            }
            else if (!inside && _wasInsideVolume)
            {
                // Left volume.
                IsActive = false;
            }

            _wasInsideVolume = inside;

            if (!IsActive) return;

            TrackedTime += Time.deltaTime;
            ComputeViewportBounds();
            Confidence = 1f; // Overridden by noise layer if enabled.
        }

        void ComputeViewportBounds()
        {
            if (overheadCamera == null || trackedRenderers.Length == 0) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var r in trackedRenderers)
            {
                if (r == null) continue;
                var bounds = r.bounds;

                // Sample the 8 corners of the world-space AABB.
                Vector3 center = bounds.center;
                Vector3 ext = bounds.extents;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = center + new Vector3(
                        ((i & 1) == 0 ? -1 : 1) * ext.x,
                        ((i & 2) == 0 ? -1 : 1) * ext.y,
                        ((i & 4) == 0 ? -1 : 1) * ext.z);

                    Vector3 vp = overheadCamera.WorldToViewportPoint(corner);
                    // Viewport is (0,0) bottom-left in Unity; flip Y so (0,0) is top-left.
                    float vpX = vp.x;
                    float vpY = 1f - vp.y;

                    if (vpX < minX) minX = vpX;
                    if (vpY < minY) minY = vpY;
                    if (vpX > maxX) maxX = vpX;
                    if (vpY > maxY) maxY = vpY;
                }
            }

            BbLeft   = minX;
            BbTop    = minY;
            BbRight  = maxX;
            BbBottom = maxY;
            Centroid = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }

        bool IsInsideCaptureVolume()
        {
            if (_config == null) return true;
            return _config.captureVolume.Contains(transform.position);
        }

        public void AssignNewId()
        {
            TrackerId = Guid.NewGuid().ToString();
        }

        // Called by MergeSimulator to temporarily suppress this tracker's individual emit.
        public void SetMerged(bool merged) => IsActive = !merged;
    }
}
