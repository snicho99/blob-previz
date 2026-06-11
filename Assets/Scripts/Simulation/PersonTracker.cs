using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Attach to the capsule that owns world position for a simulated person.
    /// Computes bounding box by projecting renderer AABB corners through the overhead camera.
    ///
    /// Blob IDs are session-scoped monotonic integers starting at 1, matching Blob V1.1.
    /// A new ID is always assigned when re-entering the capture volume.
    /// </summary>
    public class PersonTracker : MonoBehaviour
    {
        [Tooltip("The overhead camera that represents the RealSense view.")]
        public Camera overheadCamera;

        [Tooltip("Renderers contributing to this person's bounding box. Auto-collected from children if empty.")]
        public Renderer[] trackedRenderers;

        // ── Public state read by OscEmitter ──────────────────────────────────

        /// <summary>Session-unique integer ID. Never reused. Starts at 1.</summary>
        public int  BlobId      { get; private set; }
        public bool IsActive    { get; private set; }
        public float TrackedTime { get; private set; }

        // Bounding box in normalised overhead-camera space (unbounded; values outside 0-1 possible).
        // See BLOB_PROTOCOL.md — axis origin to be confirmed against live data.
        public float XMin { get; private set; }
        public float YMin { get; private set; }
        public float XMax { get; private set; }
        public float YMax { get; private set; }

        /// <summary>Bounding box centre — convenience for distance checks.</summary>
        public Vector2 Center => new Vector2((XMin + XMax) * 0.5f, (YMin + YMax) * 0.5f);

        /// <summary>
        /// Set true by MergeSimulator when this tracker is folded into a merged blob.
        /// OscEmitter skips suppressed trackers entirely (no exit message either).
        /// Cleared automatically each LateUpdate before OscEmitter reads it.
        /// </summary>
        public bool IsSuppressed { get; private set; }

        // ── Private ──────────────────────────────────────────────────────────

        static int _nextBlobId = 1;

        SimulationConfig _config;
        bool _wasInsideVolume;

        // Bbox override set by MergeSimulator; applied instead of computed values.
        bool _hasBboxOverride;
        float _overrideXMin, _overrideYMin, _overrideXMax, _overrideYMax;

        void Awake()
        {
            BlobId  = 0; // unassigned until first entry
            _config = FindFirstObjectByType<SimulationManager>()?.Config;

            if (trackedRenderers == null || trackedRenderers.Length == 0)
                trackedRenderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            bool inside = IsInsideCaptureVolume();

            if (inside && !_wasInsideVolume)
            {
                // Entered (or re-entered) the volume — always assign a fresh ID.
                BlobId      = _nextBlobId++;
                IsActive    = true;
                TrackedTime = 0f;
            }
            else if (!inside && _wasInsideVolume)
            {
                IsActive = false;
            }

            _wasInsideVolume = inside;

            if (!IsActive) return;

            TrackedTime += Time.deltaTime;

            if (!_hasBboxOverride)
                ComputeViewportBounds();
        }

        void LateUpdate()
        {
            // Clear per-frame merge state so OscEmitter always reads fresh values.
            IsSuppressed    = false;
            _hasBboxOverride = false;
        }

        // ── Merge simulation API ─────────────────────────────────────────────

        /// <summary>
        /// Called by MergeSimulator to hide this tracker from OscEmitter for one frame.
        /// No join/move/exit messages will be sent while suppressed.
        /// </summary>
        public void Suppress() => IsSuppressed = true;

        /// <summary>
        /// Called by MergeSimulator to override this tracker's bbox with a merged region.
        /// </summary>
        public void SetMergedBbox(float xMin, float yMin, float xMax, float yMax)
        {
            _hasBboxOverride = true;
            XMin = xMin; YMin = yMin;
            XMax = xMax; YMax = yMax;
        }

        // ── Internal ─────────────────────────────────────────────────────────

        void ComputeViewportBounds()
        {
            if (overheadCamera == null || trackedRenderers.Length == 0) return;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var r in trackedRenderers)
            {
                if (r == null) continue;
                var bounds = r.bounds;
                Vector3 center = bounds.center;
                Vector3 ext    = bounds.extents;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = center + new Vector3(
                        ((i & 1) == 0 ? -1 : 1) * ext.x,
                        ((i & 2) == 0 ? -1 : 1) * ext.y,
                        ((i & 4) == 0 ? -1 : 1) * ext.z);

                    Vector3 vp = overheadCamera.WorldToViewportPoint(corner);
                    // Unity viewport: (0,0) = bottom-left. Flip Y so (0,0) = top-left.
                    // Axis origin/direction to be confirmed against live tracker — see BLOB_PROTOCOL.md.
                    float vpX =       vp.x;
                    float vpY = 1f - vp.y;

                    if (vpX < minX) minX = vpX;
                    if (vpY < minY) minY = vpY;
                    if (vpX > maxX) maxX = vpX;
                    if (vpY > maxY) maxY = vpY;
                }
            }

            XMin = minX; YMin = minY;
            XMax = maxX; YMax = maxY;
        }

        bool IsInsideCaptureVolume()
        {
            if (_config == null) return true;
            return _config.captureVolume.Contains(transform.position);
        }
    }
}
