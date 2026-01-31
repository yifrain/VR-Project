using UnityEngine;
using UnityEngine.InputSystem;

namespace AstronautPlayer
{
    public class AstronautPlayer : MonoBehaviour
    {
        // 动画控制器，用于切换跑动/待机动作
        private Animator anim;
        // 角色控制器，Unity自带的用于处理角色移动和碰撞的组件
        private CharacterController controller;

        // 移动速度（米/秒）
        public float speed = 6.0f;
        // 转向速度（角度/秒），配合差值运算控制转身的快慢
        public float turnSpeed = 180.0f;
        // 当前的移动向量（包含重力影响）
        private Vector3 moveDirection = Vector3.zero;
        // 重力加速度
        public float gravity = 20.0f;

        // 新输入系统的键盘引用
        private PlayerInput playerInput;
        private Keyboard keyboard;

        void Awake()
        {
            // 获取物体上的组件引用
            controller = GetComponent<CharacterController>();
            anim = gameObject.GetComponentInChildren<Animator>();
            // 获取当前活跃的键盘设备
            keyboard = Keyboard.current;
        }

        void Update()
        {
            // 1. 获取输入
            // h = horizontal (水平/左右), v = vertical (垂直/前后)
            float h = 0;
            float v = 0;
            if (keyboard != null)
            {
                // 检测 WASD 按键状态
                if (keyboard.wKey.isPressed) v += 1;
                if (keyboard.sKey.isPressed) v -= 1;
                if (keyboard.aKey.isPressed) h -= 1;
                if (keyboard.dKey.isPressed) h += 1;
            }

            // 2. 处理动画
            // 只要水平或垂直方向有输入（绝对值大于阈值），就认为在移动
            // AnimationPar: 0 = Idle (待机), 1 = Run (跑动)
            bool hasInput = (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);
            anim.SetInteger("AnimationPar", hasInput ? 1 : 0);

            // 3. 处理移动逻辑 (仅在着地时可以改变移动方向)
            if (controller.isGrounded)
            {
                // --- 计算相对于相机的移动方向 ---
                
                // 获取相机方向（忽略Y轴，只保留在水平面上的分量，防止角色往天上飞或钻地）
                Vector3 camForward = Vector3.zero;
                Vector3 camRight = Vector3.zero;
                
                if (Camera.main != null)
                {
                    // Camera.main.transform.forward 是相机正前方
                    // Scale(..., new Vector3(1, 0, 1)) 去掉 Y 轴分量
                    // normalized 归一化，确保向量长度为 1
                    camForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                    camRight = Vector3.Scale(Camera.main.transform.right, new Vector3(1, 0, 1)).normalized;
                }
                else
                {
                    // 如果找不到主相机，就退化为使用世界坐标系（W 永远是世界北）
                    camForward = Vector3.forward;
                    camRight = Vector3.right;
                }

                // 合成最终的目标移动方向：
                // 比如按 W (v=1)，就朝 camForward 走
                // 按 D (h=1)，就朝 camRight 走
                // 同时按 W+D，就是右前方
                Vector3 targetDirection = (camForward * v + camRight * h).normalized;

                // --- 转向与移动 ---
                
                // 如果有有效的移动输入
                if (targetDirection.magnitude > 0.1f)
                {
                    // A. 转向：计算目标旋转角度
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    // 使用 Slerp (球面插值) 平滑地旋转到目标方向
                    // Time.deltaTime 确保旋转速度与帧率无关
                    // 0.1f 是一个额外的平滑系数，可以调整手感
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime * 0.1f);

                    // B. 移动：既然已经转向了目标方向，那就直接朝自己的前方移动
                    // 这样实现了“按 S -> 转身 -> 向前跑”的效果，而不是倒退
                    moveDirection = transform.forward * speed;
                }
                else
                {
                    // 没有输入时，水平速度归零（但保留 Y 轴重力，下面会处理）
                    moveDirection = Vector3.zero;
                }
            }

            // 4. 应用重力
            // 每一帧都减去重力加速度，模拟自由落体
            moveDirection.y -= gravity * Time.deltaTime;
            
            // 5. 最终应用移动
            // controller.Move 会自动处理与场景的碰撞，防止穿墙
            controller.Move(moveDirection * Time.deltaTime);
        }
    }
}