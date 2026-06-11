using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BlobPreviz
{
    /// <summary>
    /// Global NPC lifecycle manager. Adopts any NpcWanderers already in the scene,
    /// then spawns/despawns to match ConfigManager.NpcCount.
    ///
    /// Scene setup:
    ///   1. Create a prefab from your NPC GameObject (root needs NpcWanderer + NavMeshAgent + PersonTracker).
    ///   2. Assign the prefab to NpcPrefab below.
    ///   3. Assign your overhead camera to OverheadCamera below.
    ///   4. Remove or keep scene-placed NPCs — this component will adopt them.
    /// </summary>
    public class NpcManager : MonoBehaviour
    {
        public static NpcManager Instance { get; private set; }

        [Header("Spawning")]
        [Tooltip("NPC prefabs to cycle through when spawning. Each new NPC gets the next prefab in order, wrapping back to the first.")]
        public GameObject[] npcPrefabs;

        [Tooltip("Overhead camera assigned to PersonTracker on each spawned NPC.")]
        public Camera overheadCamera;

        readonly List<GameObject> _npcs = new List<GameObject>();
        WaypointGroup _waypointGroup;
        int _dealIndex;

        // ── Lifecycle ────────────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _waypointGroup = FindFirstObjectByType<WaypointGroup>();

            // Adopt any NPCs already placed in the scene.
            foreach (var w in FindObjectsByType<NpcWanderer>(FindObjectsSortMode.None))
                _npcs.Add(w.gameObject);

            if (ConfigManager.Instance == null)
            {
                Debug.LogError("[NpcManager] ConfigManager.Instance is null — NPC spawning disabled.", this);
                return;
            }

            ConfigManager.Instance.NpcSettingsChanged += OnNpcSettingsChanged;
            ApplyAll();
        }

        void OnDestroy()
        {
            if (ConfigManager.Instance != null)
                ConfigManager.Instance.NpcSettingsChanged -= OnNpcSettingsChanged;
        }

        void OnNpcSettingsChanged() => ApplyAll();

        void ApplyAll()
        {
            var cfg = ConfigManager.Instance;
            ApplyCount(cfg.NpcCount);
            ApplyWandering(cfg.WanderingEnabled);
            ApplySpeed(cfg.WanderingSpeed, cfg.WanderingSpeedVariability);
            ApplyVisibility(cfg.NpcVisible);
        }

        // ── Count ─────────────────────────────────────────────────────────────

        void ApplyCount(int target)
        {
            // Clean up any null entries first (scene NPCs that may have been deleted).
            _npcs.RemoveAll(n => n == null);

            while (_npcs.Count < target) SpawnOne();
            while (_npcs.Count > target) DespawnLast();
        }

        void SpawnOne()
        {
            if (npcPrefabs == null || npcPrefabs.Length == 0)
            {
                Debug.LogWarning("[NpcManager] NpcPrefabs is empty — cannot spawn.", this);
                return;
            }
            if (_waypointGroup == null)
            {
                Debug.LogWarning("[NpcManager] No WaypointGroup in scene — cannot find spawn position.", this);
                return;
            }

            // Round-robin through the prefab array.
            var prefab = npcPrefabs[_dealIndex % npcPrefabs.Length];
            _dealIndex++;

            if (prefab == null)
            {
                Debug.LogWarning($"[NpcManager] npcPrefabs[{(_dealIndex - 1) % npcPrefabs.Length}] is null — skipping.", this);
                return;
            }

            // Spawn at a random waypoint snapped to NavMesh.
            var wpt = _waypointGroup.GetRandom();
            if (wpt == null) return;

            if (!NavMesh.SamplePosition(wpt.position, out var hit, 3f, NavMesh.AllAreas))
            {
                Debug.LogWarning("[NpcManager] SamplePosition failed near waypoint — skipping spawn.", this);
                return;
            }

            var go = Instantiate(prefab, hit.position, Quaternion.Euler(0, Random.Range(0f, 360f), 0));

            var tracker = go.GetComponent<PersonTracker>();
            if (tracker != null && overheadCamera != null)
                tracker.overheadCamera = overheadCamera;

            _npcs.Add(go);

            // Apply current speed settings immediately.
            if (ConfigManager.Instance != null)
                ApplySpeedToNpc(go, ConfigManager.Instance.WanderingSpeed, ConfigManager.Instance.WanderingSpeedVariability);
        }

        void DespawnLast()
        {
            if (_npcs.Count == 0) return;
            int idx = _npcs.Count - 1;
            var go = _npcs[idx];
            _npcs.RemoveAt(idx);
            if (go != null) Destroy(go);
        }

        // ── Wandering ─────────────────────────────────────────────────────────

        void ApplyWandering(bool enabled)
        {
            foreach (var npc in _npcs)
            {
                if (npc == null) continue;
                var wanderer = npc.GetComponent<NpcWanderer>();
                var agent    = npc.GetComponent<NavMeshAgent>();
                if (wanderer == null) continue;

                wanderer.enabled = enabled;
                if (!enabled) agent?.ResetPath();
            }
        }

        // ── Speed ─────────────────────────────────────────────────────────────

        void ApplySpeed(float speed, float variability)
        {
            foreach (var npc in _npcs)
                ApplySpeedToNpc(npc, speed, variability);
        }

        void ApplySpeedToNpc(GameObject npc, float speed, float variability)
        {
            if (npc == null) return;
            var wanderer = npc.GetComponent<NpcWanderer>();
            var agent    = npc.GetComponent<NavMeshAgent>();
            if (wanderer != null)
            {
                wanderer.walkSpeed        = speed;
                wanderer.speedVariability = variability;
            }
            if (agent != null) agent.speed = speed;
        }

        // ── Visibility ────────────────────────────────────────────────────────

        void ApplyVisibility(bool visible)
        {
            foreach (var npc in _npcs)
            {
                if (npc == null) continue;
                foreach (var r in npc.GetComponentsInChildren<Renderer>())
                    r.enabled = visible;
            }
        }
    }
}
