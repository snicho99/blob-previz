using Klak.Spout;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Manages a Spot Light's cookie from a Spout receiver, with a fallback texture
    /// when no sender is connected.
    ///
    /// Setup:
    ///   1. Add this component to the Projector Light (alongside SpoutReceiver).
    ///   2. Assign Default Cookie (any Texture — 2D, RenderTexture, cookie asset).
    ///   3. Remove (clear) the SpoutReceiver's Target Texture field — this component
    ///      clears it automatically at runtime, but clearing it in the Inspector avoids
    ///      confusion about which system owns the cookie.
    ///   4. Leave SpoutReceiver's Target Renderer empty.
    ///
    /// Why targetTexture is cleared:
    ///   When SpoutReceiver has a targetTexture set, receivedTexture always returns that
    ///   RT — even before any frames arrive — making "not connected" detection impossible.
    ///   Without targetTexture, receivedTexture is null until the first frame, then holds
    ///   an internal RenderTexture for the lifetime of the connection.
    ///
    /// Note: once Spout connects and then disconnects mid-session the last received frame
    /// stays on screen (the internal buffer is not cleared). For a previz tool this is
    /// acceptable; restart play mode or toggle the component to reset.
    /// </summary>
    [RequireComponent(typeof(Light), typeof(SpoutReceiver))]
    public class SpoutLightCookie : MonoBehaviour
    {
        [Tooltip("Cookie shown when no Spout sender is connected.")]
        public Texture defaultCookie;

        Light _light;
        SpoutReceiver _spout;
        Texture _lastApplied;
        RenderTexture _squareRT;

        void Awake()
        {
            _light = GetComponent<Light>();
            _spout = GetComponent<SpoutReceiver>();

            // Take over cookie management — internal buffer mode required for null detection.
            _spout.targetTexture  = null;
            _spout.targetRenderer = null;
        }

        void Start()
        {
            Apply(defaultCookie);

            if (ConfigManager.Instance == null) return;

            // Seed ConfigManager from the Inspector source name on first run,
            // then let ConfigManager be the source of truth going forward.
            if (string.IsNullOrEmpty(ConfigManager.Instance.SpoutSourceName)
                && !string.IsNullOrEmpty(_spout.sourceName))
            {
                ConfigManager.Instance.SpoutSourceName = _spout.sourceName;
            }
            else
            {
                ApplySourceName();
            }

            ConfigManager.Instance.SpoutSettingsChanged += ApplySourceName;
        }

        void OnDestroy()
        {
            if (ConfigManager.Instance != null)
                ConfigManager.Instance.SpoutSettingsChanged -= ApplySourceName;
            _squareRT?.Release();
        }

        void Update()
        {
            var raw = _spout.receivedTexture;
            var source = raw != null ? (Texture)raw : defaultCookie;
            Apply(Squarify(source));
        }

        void Apply(Texture tex)
        {
            if (tex == _lastApplied) return;
            _lastApplied = tex;
            _light.cookie = tex;
        }

        // Unity light cookies must be square. Center-crops non-square textures into
        // a cached square RT sized to the smaller dimension. Already-square textures
        // pass through without allocation.
        Texture Squarify(Texture src)
        {
            if (src == null) return null;
            if (src.width == src.height) return src;

            int size = Mathf.Min(src.width, src.height);
            if (_squareRT == null || _squareRT.width != size)
            {
                _squareRT?.Release();
                _squareRT = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
                _squareRT.Create();
            }

            // Scale and offset into the source UV so we sample the centred crop.
            float scaleX  = (float)size / src.width;
            float scaleY  = (float)size / src.height;
            float offsetX = (1f - scaleX) * 0.5f;
            float offsetY = (1f - scaleY) * 0.5f;
            Graphics.Blit(src, _squareRT, new Vector2(scaleX, scaleY), new Vector2(offsetX, offsetY));
            return _squareRT;
        }

        void ApplySourceName()
        {
            if (ConfigManager.Instance == null) return;
            var name = ConfigManager.Instance.SpoutSourceName;
            if (!string.IsNullOrEmpty(name))
                _spout.sourceName = name;
        }
    }
}
