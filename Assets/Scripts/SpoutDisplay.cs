using Klak.Spout;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Pulls a named Spout feed from the experience and applies it to this object's material.
    /// Attach to the screen quad(s) in the scene.
    /// </summary>
    [RequireComponent(typeof(SpoutReceiver), typeof(Renderer))]
    public class SpoutDisplay : MonoBehaviour
    {
        // KlakSpout's SpoutReceiver component does the heavy lifting.
        // This script exists only to document intent and expose the material slot.

        [Tooltip("Material property to write the Spout texture into. URP Lit = _BaseMap, Unlit = _BaseMap, legacy Built-in = _MainTex.")]
        public string targetProperty = "_BaseMap";

        private SpoutReceiver _spout;
        private Material _mat;

        void Start()
        {
            _spout = GetComponent<SpoutReceiver>();
            _mat   = GetComponent<Renderer>().material;
        }

        void Update()
        {
            if (_spout.receivedTexture != null)
                _mat.SetTexture(targetProperty, _spout.receivedTexture);
        }
    }
}
