using UnityEngine;
using UnityEngine.AI;

namespace BlobPreviz
{
    /// <summary>
    /// Drives an NPC via NavMeshAgent: pick a random waypoint, walk there, repeat.
    ///
    /// Prefab setup:
    ///   - Root GameObject: NavMeshAgent + NpcWanderer (NO CharacterController)
    ///   - Child GameObject: rigged mesh + Animator + RootMotionToNavMesh
    ///
    /// The RootMotionToNavMesh component on the child handles the animation<->movement
    /// contract: root motion drives actual speed, NavMeshAgent owns steering.
    /// NpcWanderer drives the blend tree from desiredVelocity (what the agent wants),
    /// not velocity (what root motion is currently delivering) — avoids a feedback loop.
    ///
    /// Animator parameters (adjust names below to match your controller):
    ///   "Speed"    float — desired speed in m/s; drives idle→walk blend
    ///   "Grounded" bool  — keep true for NPCs (no jump/fall states needed)
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NpcWanderer : MonoBehaviour
    {
        [Header("Navigation")]
        [Tooltip("Drag the WaypointGroup GameObject here. Auto-found in scene if left empty.")]
        public WaypointGroup waypointGroup;

        [Tooltip("Walk speed in m/s.")]
        [Range(0.5f, 3f)] public float walkSpeed = 1.4f;

        [Tooltip("Distance from waypoint at which the NPC is considered arrived.")]
        public float stoppingDistance = 0.4f;

        [Header("Animator")]
        [Tooltip("Float parameter driving the idle→walk blend. Starter Assets expects values ~0 (idle) / ~2 (walk) / ~6 (run).")]
        public string speedParam = "Speed";

        [Tooltip("Starter Assets needs this set to 1.0 while moving or animation playback is frozen. Leave blank to ignore.")]
        public string motionSpeedParam = "MotionSpeed";

        [Tooltip("Bool parameter keeping the NPC in grounded state. Leave blank to ignore.")]
        public string groundedParam = "Grounded";

        [Tooltip("Value passed to the Speed parameter when walking at full walkSpeed. " +
                 "Check your blend tree thresholds — Starter Assets uses ~2 for walk.")]
        public float animatorWalkSpeed = 2f;

        [Tooltip("Seconds to wait at each waypoint before moving on.")]
        [Range(0f, 5f)] public float waypointPause = 0f;

        private NavMeshAgent _agent;
        private Animator _animator;
        private Transform _currentTarget;
        private float _pauseTimer;
        private bool _waiting;
        private bool _manualControl;

        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponentInChildren<Animator>();

            _agent.speed = walkSpeed;
            _agent.stoppingDistance = stoppingDistance;
            _agent.angularSpeed = 180f;
            _agent.acceleration = 8f;

            // RootMotionToNavMesh on the child handles applyRootMotion = false.
            // Don't force it here — let that component own the setting.

            if (waypointGroup == null)
                waypointGroup = FindFirstObjectByType<WaypointGroup>();

            if (waypointGroup == null)
            {
                Debug.LogError("[NpcWanderer] No WaypointGroup found in scene.", this);
                enabled = false;
                return;
            }

            PickNewTarget();
        }

        void Update()
        {
            // Manual control: animate but don't auto-pick waypoints.
            if (_manualControl)
            {
                DriveAnimator();
                return;
            }

            if (_waiting)
            {
                _pauseTimer -= Time.deltaTime;
                if (_pauseTimer <= 0f)
                {
                    _waiting = false;
                    PickNewTarget();
                }
                return;
            }

            if (HasArrived())
            {
                if (waypointPause > 0f)
                {
                    _pauseTimer = waypointPause;
                    _waiting = true;
                }
                else
                {
                    PickNewTarget();
                }
            }

            DriveAnimator();
        }

        // ── Public control API ───────────────────────────────────────────────

        /// <summary>
        /// Send the NPC to a specific world position, suspending random wandering.
        /// Call ResumeWandering() to hand control back.
        /// </summary>
        public void SetManualDestination(Vector3 worldPos)
        {
            _manualControl = true;
            _waiting       = false;
            _currentTarget = null;
            _agent.SetDestination(worldPos);
        }

        /// <summary>Re-enable random wandering from the current position.</summary>
        public void ResumeWandering()
        {
            _manualControl = false;
            PickNewTarget();
        }

        /// <summary>True when the agent has reached its current destination.</summary>
        public bool HasArrived()
        {
            return _agent.isOnNavMesh
                && !_agent.pathPending
                && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f;
        }

        void PickNewTarget()
        {
            var next = waypointGroup.GetRandom(_currentTarget);
            if (next == null) return;
            _currentTarget = next;
            _agent.SetDestination(next.position);
        }

        void DriveAnimator()
        {
            if (_animator == null) return;

            // Normalise agent's desired speed (0–walkSpeed) to the animator's expected range
            // (0–animatorWalkSpeed). Starter Assets blend tree uses ~2 for walk, not raw m/s.
            float t = _agent.speed > 0f ? _agent.desiredVelocity.magnitude / _agent.speed : 0f;
            float mappedSpeed = t * animatorWalkSpeed;
            _animator.SetFloat(speedParam, mappedSpeed, 0.1f, Time.deltaTime);

            // MotionSpeed must be > 0 or Starter Assets freezes animation playback entirely.
            float motionSpeed = t > 0.01f ? 1f : 0f;
            if (!string.IsNullOrEmpty(motionSpeedParam))
                _animator.SetFloat(motionSpeedParam, motionSpeed, 0.1f, Time.deltaTime);

            if (!string.IsNullOrEmpty(groundedParam))
                _animator.SetBool(groundedParam, true);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_currentTarget == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _currentTarget.position);
            Gizmos.DrawSphere(_currentTarget.position, 0.1f);
        }
#endif
    }
}
