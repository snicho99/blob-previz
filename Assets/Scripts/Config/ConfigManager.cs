using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Central runtime config. Reads/writes an INI file alongside the executable.
    /// Runs before all other scripts via DefaultExecutionOrder(-100).
    ///
    /// Scene setup: add to the SimulationManager GameObject (or any persistent root).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance { get; private set; }

        // ── Change events ────────────────────────────────────────────────────
        public event Action OscSettingsChanged;
        public event Action NpcSettingsChanged;
        public event Action SpoutSettingsChanged;
        public event Action DepthSettingsChanged;

        // ── Backing fields ───────────────────────────────────────────────────
        string _spoutSourceName         = "";
        string _oscTargetIp             = "127.0.0.1";
        // depth
        string _depthSpoutName          = "BlobPrevizDepth";
        float  _depthRangeMin           = 0.3f;
        float  _depthRangeMax           = 5.0f;
        int    _oscPort                 = 9000;
        int    _npcCount                = 2;
        bool   _wanderingEnabled        = true;
        float  _wanderingSpeed          = 1.4f;
        float  _wanderingSpeedVariability = 0.3f;
        bool   _npcVisible              = true;

        // ── Spout properties ─────────────────────────────────────────────────
        public string SpoutSourceName
        {
            get => _spoutSourceName;
            set { _spoutSourceName = value ?? ""; Save(); SpoutSettingsChanged?.Invoke(); }
        }

        // ── OSC properties ───────────────────────────────────────────────────
        public string OscTargetIp
        {
            get => _oscTargetIp;
            set { _oscTargetIp = value; Save(); OscSettingsChanged?.Invoke(); }
        }
        public int OscPort
        {
            get => _oscPort;
            set { _oscPort = Mathf.Clamp(value, 1, 65535); Save(); OscSettingsChanged?.Invoke(); }
        }

        // ── Depth properties ─────────────────────────────────────────────────
        public string DepthSpoutName
        {
            get => _depthSpoutName;
            set { _depthSpoutName = value ?? ""; Save(); DepthSettingsChanged?.Invoke(); }
        }
        public float DepthRangeMin
        {
            get => _depthRangeMin;
            set { _depthRangeMin = Mathf.Max(0f, value); Save(); DepthSettingsChanged?.Invoke(); }
        }
        public float DepthRangeMax
        {
            get => _depthRangeMax;
            set { _depthRangeMax = Mathf.Max(_depthRangeMin + 0.1f, value); Save(); DepthSettingsChanged?.Invoke(); }
        }

        // ── NPC properties ───────────────────────────────────────────────────
        public int NpcCount
        {
            get => _npcCount;
            set { _npcCount = Mathf.Max(0, value); Save(); NpcSettingsChanged?.Invoke(); }
        }
        public bool WanderingEnabled
        {
            get => _wanderingEnabled;
            set { _wanderingEnabled = value; Save(); NpcSettingsChanged?.Invoke(); }
        }
        public float WanderingSpeed
        {
            get => _wanderingSpeed;
            set { _wanderingSpeed = Mathf.Max(0.1f, value); Save(); NpcSettingsChanged?.Invoke(); }
        }
        public float WanderingSpeedVariability
        {
            get => _wanderingSpeedVariability;
            set { _wanderingSpeedVariability = Mathf.Max(0f, value); Save(); NpcSettingsChanged?.Invoke(); }
        }
        public bool NpcVisible
        {
            get => _npcVisible;
            set { _npcVisible = value; Save(); NpcSettingsChanged?.Invoke(); }
        }

        // ── Lifecycle ────────────────────────────────────────────────────────

        // Self-bootstraps before any scene objects awake. This means ConfigManager
        // never needs to be placed manually in the scene.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[ConfigManager]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ConfigManager>(); // triggers Awake immediately
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        // ── INI read/write ───────────────────────────────────────────────────

        static string ConfigPath =>
#if UNITY_EDITOR
            Path.Combine(Application.persistentDataPath, "blobpreviz_config.ini");
#else
            Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "config.ini");
#endif

        void Load()
        {
            string path = ConfigPath;
            try
            {
                var d = IniParser.Read(path);
                _spoutSourceName           = d.GetString("Spout",  "SourceName",       _spoutSourceName);
                _oscTargetIp               = d.GetString("OSC",   "TargetIP",         _oscTargetIp);
                _depthSpoutName            = d.GetString("Depth", "SpoutName",        _depthSpoutName);
                _depthRangeMin             = d.GetFloat ("Depth", "RangeMin",         _depthRangeMin);
                _depthRangeMax             = d.GetFloat ("Depth", "RangeMax",         _depthRangeMax);
                _oscPort                   = d.GetInt   ("OSC",  "Port",              _oscPort);
                _npcCount                  = d.GetInt   ("NPCs", "Count",             _npcCount);
                _wanderingEnabled          = d.GetBool  ("NPCs", "WanderingEnabled",  _wanderingEnabled);
                _wanderingSpeed            = d.GetFloat ("NPCs", "WanderingSpeed",    _wanderingSpeed);
                _wanderingSpeedVariability = d.GetFloat ("NPCs", "SpeedVariability",  _wanderingSpeedVariability);
                _npcVisible                = d.GetBool  ("NPCs", "Visible",           _npcVisible);
                Debug.Log($"[ConfigManager] Loaded from {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigManager] Failed to load {path}: {e.Message} — using defaults.");
            }
        }

        public void Save()
        {
            string path = ConfigPath;
            try
            {
                var data = new Dictionary<string, Dictionary<string, string>>
                {
                    ["Spout"] = new Dictionary<string, string>
                    {
                        ["SourceName"] = _spoutSourceName,
                    },
                    ["OSC"] = new Dictionary<string, string>
                    {
                        ["TargetIP"] = _oscTargetIp,
                        ["Port"]     = _oscPort.ToString(),
                    },
                    ["NPCs"] = new Dictionary<string, string>
                    {
                        ["Count"]            = _npcCount.ToString(),
                        ["WanderingEnabled"] = _wanderingEnabled ? "true" : "false",
                        ["WanderingSpeed"]   = _wanderingSpeed.ToString("F2", CultureInfo.InvariantCulture),
                        ["SpeedVariability"] = _wanderingSpeedVariability.ToString("F2", CultureInfo.InvariantCulture),
                        ["Visible"]          = _npcVisible ? "true" : "false",
                    },
                    ["Depth"] = new Dictionary<string, string>
                    {
                        ["SpoutName"] = _depthSpoutName,
                        ["RangeMin"]  = _depthRangeMin.ToString("F2", CultureInfo.InvariantCulture),
                        ["RangeMax"]  = _depthRangeMax.ToString("F2", CultureInfo.InvariantCulture),
                    },
                };
                IniParser.Write(path, data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigManager] Failed to save {path}: {e.Message}");
            }
        }
    }
}
