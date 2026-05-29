using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace BlobPreviz
{
    /// <summary>
    /// Click anywhere in the Game view to send a target NPC to that world position.
    ///
    /// Attach to any GameObject (e.g. the SimulationManager or a dedicated controller object).
    /// Raycasts against physics colliders on the specified layer(s), then snaps the hit
    /// point to the NavMesh before sending the NPC.
    ///
    /// Optional: hold a modifier key to activate (useful when WASD is in use for the player).
    /// Optional: NPC resumes random wandering automatically after it arrives.
    /// </summary>
    public enum ClickMouseButton { Left, Right, Middle }

    public class ClickToMoveController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The NPC to direct. Must have an NpcWanderer component.")]
        public NpcWanderer target;

        [Header("Input")]
        [Tooltip("Mouse button to use for placing a destination.")]
        public ClickMouseButton mouseButton = ClickMouseButton.Left;

        [Tooltip("Hold this key to activate clicks. Set to None to always be active.")]
        public Key modifierKey = Key.None;

        [Header("Raycast")]
        [Tooltip("Camera to raycast from. Defaults to Camera.main if left empty.")]
        public Camera sourceCamera;

        [Tooltip("Only hit colliders on these layers. Set to the layer your floor/ground is on.")]
        public LayerMask groundLayers = Physics.DefaultRaycastLayers;

        [Tooltip("Max NavMesh sample radius when snapping the hit point to walkable surface.")]
        public float navMeshSampleRadius = 2f;

        [Header("Behaviour")]
        [Tooltip("After the NPC arrives at the clicked point, resume random wandering.")]
        public bool resumeWanderingOnArrival = false;

        private bool _waitingForArrival;

        void Start()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            if (target == null)
                Debug.LogWarning("[ClickToMoveController] No target NPC assigned.", this);
        }

        void Update()
        {
            HandleClick();

            if (_waitingForArrival && target != null && target.HasArrived())
            {
                _waitingForArrival = false;
                if (resumeWanderingOnArrival)
                    target.ResumeWandering();
            }
        }

        void HandleClick()
        {
            if (target == null || sourceCamera == null) return;
            if (Mouse.current == null) return;

            // Modifier key gate.
            if (!ModifierHeld()) return;

            if (!ButtonPressedThisFrame()) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = sourceCamera.ScreenPointToRay(mousePos);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayers))
                return;

            // Snap to nearest NavMesh point so we never send the agent off-mesh.
            if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                Debug.LogWarning("[ClickToMoveController] Click hit a collider but no NavMesh " +
                                 $"found within {navMeshSampleRadius}m. Try increasing navMeshSampleRadius.", this);
                return;
            }

            target.SetManualDestination(navHit.position);
            _waitingForArrival = resumeWanderingOnArrival;

            Debug.Log($"[ClickToMoveController] Sending {target.name} to {navHit.position}", this);
        }

        bool ModifierHeld()
        {
            if (modifierKey == Key.None) return true;
            return Keyboard.current != null && Keyboard.current[modifierKey].isPressed;
        }

        bool ButtonPressedThisFrame()
        {
            return mouseButton switch
            {
                ClickMouseButton.Left   => Mouse.current.leftButton.wasPressedThisFrame,
                ClickMouseButton.Right  => Mouse.current.rightButton.wasPressedThisFrame,
                ClickMouseButton.Middle => Mouse.current.middleButton.wasPressedThisFrame,
                _                       => false
            };
        }
    }
}
