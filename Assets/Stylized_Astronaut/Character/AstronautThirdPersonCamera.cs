using UnityEngine;
using UnityEngine.InputSystem;

namespace AstronautThirdPersonCamera
{
    public class AstronautThirdPersonCamera : MonoBehaviour
    {
        private const float Y_ANGLE_MIN = 0.0f;
        private const float Y_ANGLE_MAX = 50.0f;

        public Transform lookAt;
        public Transform camTransform;
        public float distance = 5.0f;
        public float sensitivityX = 20.0f;
        public float sensitivityY = 20.0f;

        private float currentX = 0.0f;
        private float currentY = 45.0f;
        private Mouse mouse;

        private void Start()
        {
            camTransform = transform;
            mouse = Mouse.current;
        }

        private void Update()
        {
            if (mouse == null) return;

            // 获取鼠标输入
            Vector2 delta = mouse.delta.ReadValue();
            currentX += delta.x * 0.01f * sensitivityX;
            currentY -= delta.y * 0.01f * sensitivityY;
            currentY = Mathf.Clamp(currentY, Y_ANGLE_MIN, Y_ANGLE_MAX);
        }

        private void LateUpdate()
        {
            Vector3 dir = new Vector3(0, 0, -distance);
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            camTransform.position = lookAt.position + rotation * dir;
            camTransform.LookAt(lookAt.position);
        }
    }
}