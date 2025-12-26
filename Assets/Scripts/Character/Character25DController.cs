using UnityEngine;
using CombatSystem.Core;

namespace Character3C
{
    /// <summary>
    /// 2.5D 角色控制器
    /// 在3D空间中移动，但使用2D Sprite渲染
    /// 支持等角视角和斜45度视角
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class Character25DController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float deceleration = 20f;
        // [SerializeField] private float rotationSpeed = 720f;

        [Header("跳跃参数")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private int maxJumpCount = 1;

        [Header("地面检测")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        [Header("2.5D 设置")]
        [SerializeField] private bool isIsometric = true; // 是否为等角视角
        [SerializeField] private float isometricAngle = 30f; // 等角视角角度

        // 黑板数据
        public CharacterBlackboard25D Blackboard { get; private set; }

        // 任务管理
        private TaskEntry<CharacterBlackboard25D> currentTask;

        // 组件引用
        private Rigidbody rb;
        private CapsuleCollider col;

        // 移动状态
        private Vector3 velocity;
        private Vector3 moveDirection;
        private bool isGrounded;
        private int jumpCount;

        // 相机引用（用于计算移动方向）
        private Camera mainCamera;

        public float MoveSpeed => moveSpeed;
        public float JumpForce => jumpForce;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();
            mainCamera = Camera.main;

            // 配置Rigidbody
            rb.useGravity = false; // 使用自定义重力
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // 初始化黑板
            Blackboard = new CharacterBlackboard25D
            {
                Transform = transform,
                Rigidbody = rb,
                Collider = col
            };
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // 更新地面检测
            UpdateGroundedState();

            // 更新黑板数据
            UpdateBlackboard();

            // 更新当前任务
            currentTask?.Update(deltaTime);

            // 重置单帧输入标记
            Blackboard.ResetInputFlags();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
            ApplyGravity();
        }

        /// <summary>
        /// 应用移动
        /// </summary>
        private void ApplyMovement()
        {
            if (!Blackboard.CanMove)
            {
                return;
            }

            // 获取输入方向（相对于相机）
            Vector2 input = Blackboard.InputMove;
            if (input.sqrMagnitude > 0.01f)
            {
                // 根据相机方向计算移动方向
                moveDirection = GetCameraRelativeMovement(input);

                // 平滑加速
                Vector3 targetVelocity = moveDirection * moveSpeed;
                velocity.x = Mathf.MoveTowards(velocity.x, targetVelocity.x, acceleration * Time.fixedDeltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, targetVelocity.z, acceleration * Time.fixedDeltaTime);
            }
            else
            {
                // 平滑减速
                velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.fixedDeltaTime);
                velocity.z = Mathf.MoveTowards(velocity.z, 0, deceleration * Time.fixedDeltaTime);
            }

            // 应用水平移动
            rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

            // 更新面向方向（但不旋转，因为使用Billboard）
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Blackboard.FacingDirection = moveDirection.normalized;
            }
        }

        /// <summary>
        /// 获取相对于相机的移动方向
        /// </summary>
        private Vector3 GetCameraRelativeMovement(Vector2 input)
        {
            if (mainCamera == null)
                return new Vector3(input.x, 0, input.y);

            if (isIsometric)
            {
                // 等角视角：固定角度的相机
                // 输入的X对应世界的X和Z，输入的Y对应世界的Z和X
                float angle = isometricAngle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 right = new Vector3(cos, 0, -sin);
                Vector3 forward = new Vector3(sin, 0, cos);

                return (right * input.x + forward * input.y).normalized;
            }
            else
            {
                // 自由相机：根据相机朝向计算
                Vector3 forward = mainCamera.transform.forward;
                Vector3 right = mainCamera.transform.right;

                forward.y = 0;
                right.y = 0;

                forward.Normalize();
                right.Normalize();

                return (right * input.x + forward * input.y).normalized;
            }
        }

        /// <summary>
        /// 应用重力
        /// </summary>
        private void ApplyGravity()
        {
            if (!isGrounded)
            {
                velocity.y = rb.linearVelocity.y - gravity * Time.fixedDeltaTime;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocity.y, rb.linearVelocity.z);
            }
            else
            {
                velocity.y = rb.linearVelocity.y;
                jumpCount = 0;
            }
        }

        /// <summary>
        /// 跳跃
        /// </summary>
        public void Jump()
        {
            if (!Blackboard.CanJump)
                return;

            if (jumpCount >= maxJumpCount)
                return;

            velocity.y = jumpForce;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, velocity.y, rb.linearVelocity.z);
            jumpCount++;

            Debug.Log($"跳跃 - 次数: {jumpCount}");
        }

        /// <summary>
        /// 更新地面状态
        /// </summary>
        private void UpdateGroundedState()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        }

        /// <summary>
        /// 更新黑板数据
        /// </summary>
        private void UpdateBlackboard()
        {
            Blackboard.Velocity = rb.linearVelocity;
            Blackboard.IsGrounded = isGrounded;
            Blackboard.Position = transform.position;
            Blackboard.MoveDirection = moveDirection;
        }

        /// <summary>
        /// 设置当前任务
        /// </summary>
        public void SetTask(TaskEntry<CharacterBlackboard25D> task)
        {
            currentTask?.Stop();
            currentTask = task;

            if (currentTask != null)
            {
                currentTask.Blackboard = Blackboard;
                currentTask.Start();
            }
        }

        /// <summary>
        /// 获取当前任务
        /// </summary>
        public TaskEntry<CharacterBlackboard25D> GetTask()
        {
            return currentTask;
        }

        /// <summary>
        /// 绘制 Gizmos
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(origin, Vector3.down * (groundCheckDistance + 0.1f));

            // 绘制移动方向
            if (Application.isPlaying && moveDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position + Vector3.up, moveDirection * 2f);
            }
        }
    }
}

