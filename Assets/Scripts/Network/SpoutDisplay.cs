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
        [Tooltip("Texture shown on the quad (and sent to the projector) when no Spout sender is connected.")]
        public Texture defaultTexture;

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
            ApplyTexture(defaultTexture);
        }

        void Update()
        {
            var tex = _spout.receivedTexture;
            ApplyTexture(tex != null ? tex : defaultTexture);
        }

        void ApplyTexture(Texture tex)
        {
            if (tex == _lastTexture) return;
            _lastTexture = tex;
            _mat.SetTexture(targetProperty, tex);
            if (projectorOutput != null) projectorOutput.SetCookie(tex);
        }
    }
}
