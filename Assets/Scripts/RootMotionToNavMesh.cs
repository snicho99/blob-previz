using UnityEngine;
using UnityEngine.AI;

namespace BlobPreviz
{
    /// <summary>
    /// Place on the same GameObject as the Animator (the character mesh child).
    /// Intercepts root motion and redirects it to the parent's NavMeshAgent,
    /// so the animation drives actual movement speed with no foot-skating.
    ///
    /// The NavMeshAgent still owns steering and pathfinding; the animation
    /// owns the movement delta each frame.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RootMotionToNavMesh : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private Animator _animator;

        void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponentInParent<NavMeshAgent>();

            if (_agent == null)
            {
                Debug.LogError("[RootMotionToNavMesh] No NavMeshAgent found in parent.", this);
                return;
            }

            // Disable applyRootMotion — we apply the delta ourselves in OnAnimatorMove.
            _animator.applyRootMotion = false;

            // Stop the agent from moving the transform. We do that via root motion.
            // This also means desiredVelocity is driven purely by the path, not actual
            // movement — so NpcWanderer can read it immediately to bootstrap the blend tree.
            _agent.updatePosition = false;
        }

        // --- Starter Assets animation event stubs ---
        // The walk/run animations fire these events. Without a receiver Unity spams warnings.
        void OnFootstep(AnimationEvent e) { }
        void OnLand(AnimationEvent e)     { }

        // Called by Unity every frame after the Animator updates, regardless of applyRootMotion.
        void OnAnimatorMove()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            // Apply root motion to the transform ourselves.
            transform.position += _animator.deltaPosition;

            // Keep the agent's internal position in sync so steering stays accurate.
            _agent.nextPosition = transform.position;
        }
    }
}


