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
                Debug.LogError("[RootMotionToNavMesh] No NavMeshAgent found in parent.", this);

            // We handle application manually in OnAnimatorMove.
            _animator.applyRootMotion = false;
        }

        // Called by Unity every frame after the Animator updates, regardless of applyRootMotion.
        void OnAnimatorMove()
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            // Hand the root-motion delta to the agent as a velocity.
            // The agent uses this for movement while still steering via its own path.
            _agent.velocity = _animator.deltaPosition / Time.deltaTime;
        }
    }
}


