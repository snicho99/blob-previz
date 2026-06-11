using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// WASD puppet controller. Attach to the capsule (position owner).
    /// The rigged character mesh rides along as a child and rotates to face movement direction.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PersonController : MonoBehaviour
    {
        public float moveSpeed = 1.4f;    // ~comfortable walking pace in m/s
        public float turnSpeed = 10f;     // degrees/s for mesh child rotation

        [Tooltip("The child mesh root that visually rotates to face movement. Leave null to skip.")]
        public Transform meshRoot;

        private CharacterController _cc;

        void Awake() => _cc = GetComponent<CharacterController>();

        void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 move = new Vector3(h, 0f, v).normalized * moveSpeed;
            move.y = -9.81f; // gravity
            _cc.Move(move * Time.deltaTime);

            if (meshRoot != null && move.sqrMagnitude > 0.01f)
            {
                Vector3 flatMove = new Vector3(move.x, 0f, move.z);
                Quaternion target = Quaternion.LookRotation(flatMove);
                meshRoot.rotation = Quaternion.Slerp(meshRoot.rotation, target, turnSpeed * Time.deltaTime);
            }
        }
    }
}
