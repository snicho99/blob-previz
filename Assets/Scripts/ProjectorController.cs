using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Drives the BlobPreviz/ProjectedTexture shader.
    ///
    /// Setup:
    ///   1. Add this component to an empty GameObject above the scene.
    ///   2. Add a Camera component to the same GameObject.
    ///      - Set Projection to Perspective.
    ///      - Point it straight down (rotation 90, 0, 0).
    ///      - Set Field of View to match the real projector's throw angle.
    ///      - The camera will be disabled automatically — it is only used for its matrices.
    ///   3. Assign your test texture to Projected Texture.
    ///   4. Apply the BlobPreviz/ProjectedTexture material to floor and character meshes.
    ///
    /// Later: swap Projected Texture for the Spout RenderTexture to use live content.
    /// If the image appears upside down, tick Flip Y on the material.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ProjectorController : MonoBehaviour
    {
        [Header("Projected Content")]
        [Tooltip("Texture to project. Swap for a RenderTexture when using Spout.")]
        public Texture projectedTexture;

        [Header("Debug")]
        [Tooltip("Draw the projector perspective frustum in the scene view.")]
        public bool showGizmo = true;

        private Camera _cam;

        // Shader property IDs — cached for performance.
        private static readonly int PropVP  = Shader.PropertyToID("_ProjectorVP");
        private static readonly int PropTex = Shader.PropertyToID("_ProjectorTex");
        private static readonly int PropPos = Shader.PropertyToID("_ProjectorPos");

        void Awake()
        {
            _cam = GetComponent<Camera>();
            // Disable rendering — we only want the matrix, not a rendered output.
            _cam.enabled = false;
        }

        void Update()
        {
            PushGlobals();
        }

        void PushGlobals()
        {
            if (projectedTexture != null)
                Shader.SetGlobalTexture(PropTex, projectedTexture);

            // View matrix: world → camera space.
            // Projection matrix: camera space → clip space.
            // Unity's Camera.projectionMatrix is OpenGL-convention (Y up in NDC).
            Matrix4x4 vp = _cam.projectionMatrix * _cam.worldToCameraMatrix;
            Shader.SetGlobalMatrix(PropVP, vp);

            // World position is used per-fragment for the perspective facing test.
            Shader.SetGlobalVector(PropPos, transform.position);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!showGizmo) return;
            var cam = GetComponent<Camera>();
            if (cam == null) return;

            Gizmos.color  = new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            if (cam.orthographic)
            {
                float s = cam.orthographicSize;
                float a = s * cam.aspect;
                float d = cam.farClipPlane;
                Gizmos.DrawWireCube(new Vector3(0, 0, d * 0.5f), new Vector3(a * 2, s * 2, d));
            }
            else
            {
                Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
