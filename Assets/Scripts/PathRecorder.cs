using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Records and replays the world-position + Y-rotation of a PersonController capsule.
    /// Outputs a timestamped CSV next to the executable (or in Application.persistentDataPath in Editor).
    ///
    /// Toggle record:  R key
    /// Toggle replay:  P key
    /// </summary>
    public class PathRecorder : MonoBehaviour
    {
        public KeyCode recordKey = KeyCode.R;
        public KeyCode replayKey = KeyCode.P;

        [Tooltip("Path to a previously recorded CSV to load on start. Leave empty for none.")]
        public string preloadClipPath = "";

        private struct Frame { public float t; public Vector3 pos; public float yRot; }

        private List<Frame> _clip = new List<Frame>();
        private bool _recording;
        private bool _replaying;
        private float _replayStart;
        private int _replayIndex;
        private float _recordStart;

        private PersonController _controller;

        void Awake()
        {
            _controller = GetComponent<PersonController>();

            if (!string.IsNullOrEmpty(preloadClipPath) && File.Exists(preloadClipPath))
                LoadCsv(preloadClipPath);
        }

        void Update()
        {
            if (Input.GetKeyDown(recordKey))
            {
                if (_recording) StopRecording();
                else            StartRecording();
            }

            if (Input.GetKeyDown(replayKey))
            {
                if (_replaying) StopReplay();
                else            StartReplay();
            }

            if (_recording)
                _clip.Add(new Frame
                {
                    t    = Time.time - _recordStart,
                    pos  = transform.position,
                    yRot = transform.eulerAngles.y
                });

            if (_replaying)
                TickReplay();
        }

        void StartRecording()
        {
            _clip.Clear();
            _recordStart = Time.time;
            _recording = true;
            Debug.Log("[PathRecorder] Recording started.");
        }

        void StopRecording()
        {
            _recording = false;
            string path = SaveCsv();
            Debug.Log($"[PathRecorder] Saved {_clip.Count} frames to {path}");
        }

        void StartReplay()
        {
            if (_clip.Count == 0) { Debug.LogWarning("[PathRecorder] No clip loaded."); return; }
            if (_controller != null) _controller.enabled = false;
            _replayIndex = 0;
            _replayStart = Time.time;
            _replaying = true;
            Debug.Log("[PathRecorder] Replay started.");
        }

        void StopReplay()
        {
            _replaying = false;
            if (_controller != null) _controller.enabled = true;
            Debug.Log("[PathRecorder] Replay stopped.");
        }

        void TickReplay()
        {
            float elapsed = Time.time - _replayStart;

            // Advance index to the last frame at or before elapsed.
            while (_replayIndex + 1 < _clip.Count && _clip[_replayIndex + 1].t <= elapsed)
                _replayIndex++;

            if (_replayIndex >= _clip.Count - 1)
            {
                // Loop.
                _replayStart = Time.time;
                _replayIndex = 0;
                return;
            }

            // Lerp between frames.
            var a = _clip[_replayIndex];
            var b = _clip[_replayIndex + 1];
            float span = b.t - a.t;
            float frac = span > 0f ? (elapsed - a.t) / span : 0f;

            transform.position = Vector3.Lerp(a.pos, b.pos, frac);
            transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(a.yRot, b.yRot, frac), 0f);
        }

        string SaveCsv()
        {
            string dir  = Application.isEditor ? Application.persistentDataPath : Path.GetDirectoryName(Application.dataPath);
            string file = Path.Combine(dir, $"path_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            var sb = new StringBuilder("t,px,py,pz,yrot\n");
            foreach (var f in _clip)
                sb.AppendLine($"{f.t:F4},{f.pos.x:F4},{f.pos.y:F4},{f.pos.z:F4},{f.yRot:F2}");

            File.WriteAllText(file, sb.ToString());
            return file;
        }

        void LoadCsv(string path)
        {
            _clip.Clear();
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("t")) continue; // header
                var p = line.Split(',');
                if (p.Length < 5) continue;
                _clip.Add(new Frame
                {
                    t    = float.Parse(p[0]),
                    pos  = new Vector3(float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3])),
                    yRot = float.Parse(p[4])
                });
            }
            Debug.Log($"[PathRecorder] Loaded {_clip.Count} frames from {path}");
        }
    }
}
