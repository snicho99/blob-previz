using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Drives a Spot Light as a texture projector.
    ///
    /// Setup:
    ///   1. Add a Spot Light to the scene, point it downward (rotation 90, 0, 0).
    ///   2. Add this component to the same GameObject.
    ///   3. Assign your cookie texture in the inspector.
    ///   4. Enable shadows on the light (Hard or Soft).
    ///   5. Ensure all surfaces use URP Lit materials with Cast/Receive Shadows on.
    ///
    /// URP Asset requirements (Project Settings > Graphics > URP Asset):
    ///   - Additional Lights:        Per Pixel
    ///   - Additional Light Shadows: Enabled
    ///   - Cookie Atlas Resolution:  1024 or 2048
    ///
    /// To use Spout content as the cookie later:
    ///   Call SetCookie(spoutRenderTexture) once the Spout receiver is ready.
    ///   RenderTextures update automatically each frame — no per-frame call needed.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class ProjectorLightController : MonoBehaviour
    {
        [Header("Projected Content")]
        [Tooltip("Cookie texture. Assign a test texture now; swap for Spout RenderTexture later.")]
        public Texture cookieTexture;

        private Light _light;

        void Awake()
        {
            _light = GetComponent<Light>();

            if (_light.type != LightType.Spot)
                Debug.LogWarning("[ProjectorLightController] Light should be type Spot.", this);

            if (cookieTexture != null)
                _light.cookie = cookieTexture;
        }

        /// <summary>
        /// Hot-swap the cookie at runtime — e.g. once the Spout RenderTexture is ready.
        /// Assigning a RenderTexture here is sufficient; it updates automatically each frame.
        /// </summary>
        public void SetCookie(Texture tex)
        {
            cookieTexture  = tex;
            _light.cookie  = tex;
        }
    }
}
