using UnityEngine;

namespace BlobPreviz
{
    /// <summary>
    /// Debug fly camera. Completely decoupled from the overhead sim camera.
    /// Toggle with the key below; WASD + mouse look while active.
    /// The overhead camera continues to generate OSC data regardless.
    /// </summary>
    public class FreeFlyCamera : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.F;
        public float moveSpeed = 5f;
        public float fastMultiplier = 3f;   // hold Shift
        public float mouseSensitivity = 2f;

        private bool _active;
        private float _yaw;
        private float _pitch;

        void Start()
        {
            _yaw   = transform.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
            SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                SetActive(!_active);

            if (!_active) return;

            _yaw   += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);
            Vector3 dir = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) dir += transform.forward;
            if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
            if (Input.GetKey(KeyCode.A)) dir -= transform.right;
            if (Input.GetKey(KeyCode.D)) dir += transform.right;
            if (Input.GetKey(KeyCode.E)) dir += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) dir -= Vector3.up;
            transform.position += dir.normalized * speed * Time.deltaTime;
        }

        void SetActive(bool on)
        {
            _active = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !on;
        }
    }
}
