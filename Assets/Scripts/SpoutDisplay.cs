using Klak.Spout;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Receives a Spout feed from the experience and:
    ///   a) Applies it to this quad's material (the in-scene screen representation).
    ///   b) Optionally forwards it to a ProjectorLightController as the cookie texture.
    ///
    /// Attach to each screen quad in the scene alongside KlakSpout's SpoutReceiver.
    /// The quad's material should use URP/Unlit — screens emit, they don't receive lighting.
    ///
    /// KlakSpout setup:
    ///   - On the SpoutReceiver component, set Source Name to the sender name the
    ///     experience broadcasts (check the experience's Spout sender configuration).
    ///   - Leave blank to connect to the first available sender.
    /// </summary>
    [RequireComponent(typeof(SpoutReceiver), typeof(Renderer))]
    public class SpoutDisplay : MonoBehaviour
    {
        [Tooltip("Material property to write the texture into. URP Unlit/Lit = _BaseMap.")]
        public string targetProperty = "_BaseMap";

        [Header("Optional")]
        [Tooltip("When the Spout texture arrives, forward it to this projector as the cookie. " +
                 "Leave null if not using the projector path.")]
        public ProjectorLightController projectorOutput;

        private SpoutReceiver _spout;
        private Material      _mat;
        private Texture       _lastTexture;

        void Start()
        {
            _spout = GetComponent<SpoutReceiver>();
            _mat   = GetComponent<Renderer>().material;
        }

        void Update()
        {
            var tex = _spout.receivedTexture;
            if (tex == null) return;

            // Apply to quad material every frame (texture content updates in-place).
            _mat.SetTexture(targetProperty, tex);

            // Forward to projector cookie once — the RenderTexture reference is stable,
            // so assigning it once is enough for live content to flow through.
            if (projectorOutput != null && tex != _lastTexture)
            {
                _lastTexture = tex;
                projectorOutput.SetCookie(tex);
            }
        }
    }
}
