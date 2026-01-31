using UnityEngine;
using UnityEngine.InputSystem;

namespace AstronautPlayer
{
    public class AstronautPlayer : MonoBehaviour
    {
        private Animator anim;
        private CharacterController controller;

        public float speed = 600.0f;
        public float turnSpeed = 400.0f;
        private Vector3 moveDirection = Vector3.zero;
        public float gravity = 20.0f;

        private PlayerInput playerInput;
        private Keyboard keyboard;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            anim = gameObject.GetComponentInChildren<Animator>();
            keyboard = Keyboard.current;
        }

        void Update()
        {
            // 检测W键输入控制动画
            bool isMoving = keyboard != null && keyboard.wKey.isPressed;
            anim.SetInteger("AnimationPar", isMoving ? 1 : 0);

            // 前后移动
            if (controller.isGrounded)
            {
                float vertical = 0;
                if (keyboard != null)
                {
                    vertical = keyboard.wKey.isPressed ? 1 : keyboard.sKey.isPressed ? -1 : 0;
                }
                moveDirection = transform.forward * vertical * speed;
            }

            // 左右转向
            float turn = 0;
            if (keyboard != null)
            {
                turn = keyboard.dKey.isPressed ? 1 : keyboard.aKey.isPressed ? -1 : 0;
            }
            transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);

            // 应用移动和重力
            controller.Move(moveDirection * Time.deltaTime);
            moveDirection.y -= gravity * Time.deltaTime;
        }
    }
}