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
        [Tooltip("Float parameter name driving the idle→walk blend. Match your Animator Controller.")]
        public string speedParam = "Speed";

        [Tooltip("Bool parameter keeping the NPC in grounded state. Leave blank to ignore.")]
        public string groundedParam = "Grounded";

        [Tooltip("Seconds to wait at each waypoint before moving on.")]
        [Range(0f, 5f)] public float waypointPause = 0f;

        private NavMeshAgent _agent;
        private Animator _animator;
        private Transform _currentTarget;
        private float _pauseTimer;
        private bool _waiting;

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

        void PickNewTarget()
        {
            var next = waypointGroup.GetRandom(_currentTarget);
            if (next == null) return;
            _currentTarget = next;
            _agent.SetDestination(next.position);
        }

        bool HasArrived()
        {
            return _agent.isOnNavMesh
                && !_agent.pathPending
                && _agent.remainingDistance <= _agent.stoppingDistance + 0.05f;
        }

        void DriveAnimator()
        {
            if (_animator == null) return;

            // desiredVelocity = where the agent *wants* to go this frame (pre-root-motion).
            // Using velocity here would create a feedback loop with RootMotionToNavMesh.
            float desiredSpeed = _agent.desiredVelocity.magnitude;
            _animator.SetFloat(speedParam, desiredSpeed, 0.1f, Time.deltaTime);

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
