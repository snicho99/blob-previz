using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BlobPreviz
{
    /// <summary>
    /// Minimal INI read/write. Supports [Sections], key=value pairs, and # ; comments.
    /// All lookups are case-insensitive. Float values use InvariantCulture.
    /// </summary>
    public static class IniParser
    {
        public static Dictionary<string, Dictionary<string, string>> Read(string path)
        {
            var data = new Dictionary<string, Dictionary<string, string>>(CIComparer);
            if (!File.Exists(path)) return data;

            string section = "";
            data[section] = new Dictionary<string, string>(CIComparer);

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == ';') continue;

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    if (!data.ContainsKey(section))
                        data[section] = new Dictionary<string, string>(CIComparer);
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                data[section][key] = val;
            }
            return data;
        }

        public static void Write(string path, Dictionary<string, Dictionary<string, string>> data)
        {
            var sb = new StringBuilder();
            foreach (var section in data)
            {
                if (!string.IsNullOrEmpty(section.Key))
                    sb.AppendLine($"[{section.Key}]");
                foreach (var kv in section.Value)
                    sb.AppendLine($"{kv.Key}={kv.Value}");
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        }

        // ── Typed helpers ────────────────────────────────────────────────────

        public static string GetString(this Dictionary<string, Dictionary<string, string>> d,
            string section, string key, string fallback = "")
        {
            return d.TryGetValue(section, out var s) && s.TryGetValue(key, out var v) ? v : fallback;
        }

        public static int GetInt(this Dictionary<string, Dictionary<string, string>> d,
            string section, string key, int fallback = 0)
        {
            return int.TryParse(d.GetString(section, key), out var r) ? r : fallback;
        }

        public static float GetFloat(this Dictionary<string, Dictionary<string, string>> d,
            string section, string key, float fallback = 0f)
        {
            return float.TryParse(d.GetString(section, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : fallback;
        }

        public static bool GetBool(this Dictionary<string, Dictionary<string, string>> d,
            string section, string key, bool fallback = false)
        {
            var s = d.GetString(section, key, null);
            if (s == null) return fallback;
            return s.ToLowerInvariant() == "true" || s == "1";
        }

        static readonly StringComparer CIComparer = StringComparer.OrdinalIgnoreCase;
    }
}
