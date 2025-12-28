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
        
        // 击退状态管理
        private Vector3 knockbackVelocity;
        private float knockbackDecay = 10f; // 击退衰减速度
        private bool isKnockedBack = false;

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

            // 配置Rigidbody - 设置为Kinematic模式
            rb.isKinematic = true; // Kinematic模式：完全由脚本控制，不受物理力影响
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

            // 更新黑板数据
            UpdateBlackboard();

            // 更新当前任务
            currentTask?.Update(deltaTime);

            // 重置单帧输入标记
            Blackboard.ResetInputFlags();
        }

        private void FixedUpdate()
        {
            // 在物理更新中检测地面（与移动同步）
            UpdateGroundedState();
            
            CalculateHorizontalMovement();
            CalculateVerticalMovement();
            ApplyMovement();
        }

        /// <summary>
        /// 计算水平移动速度 (Kinematic模式)
        /// </summary>
        private void CalculateHorizontalMovement()
        {
            // 处理击退衰减
            if (isKnockedBack)
            {
                knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDecay * Time.fixedDeltaTime);
                if (knockbackVelocity.sqrMagnitude < 0.01f)
                {
                    knockbackVelocity = Vector3.zero;
                    isKnockedBack = false;
                }
            }

            if (!Blackboard.CanMove && !isKnockedBack)
            {
                // 不能移动且没有击退时，速度归零
                velocity.x = 0;
                velocity.z = 0;
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

            // 更新面向方向（但不旋转，因为使用Billboard）
            if (moveDirection.sqrMagnitude > 0.01f && !isKnockedBack)
            {
                Blackboard.FacingDirection = moveDirection.normalized;
            }
        }

        /// <summary>
        /// 计算垂直移动速度 (Kinematic模式)
        /// </summary>
        private void CalculateVerticalMovement()
        {
            if (!isGrounded)
            {
                // 应用重力加速度
                velocity.y -= gravity * Time.fixedDeltaTime;
            }
            else
            {
                // 在地面上时，只有向下的速度才重置
                if (velocity.y < 0)
                {
                    velocity.y = 0;
                }
                
                // 只有在真正贴地时才重置跳跃计数
                if (velocity.y <= 0.01f)
                {
                    jumpCount = 0;
                }
            }
        }

        /// <summary>
        /// 应用移动 - 统一处理水平和垂直移动 (Kinematic模式，带碰撞检测)
        /// </summary>
        private void ApplyMovement()
        {
            // 计算水平速度（包含击退）
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            if (isKnockedBack)
            {
                // 击退时，移动速度会被击退速度影响
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, knockbackVelocity, 0.7f);
            }

            // 合并水平和垂直速度
            Vector3 finalVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);

            // 计算目标位置
            Vector3 newPosition = rb.position + finalVelocity * Time.fixedDeltaTime;
            
            // 碰撞检测：分离水平和垂直移动
            Vector3 horizontalMovement = new Vector3(horizontalVelocity.x, 0, horizontalVelocity.z) * Time.fixedDeltaTime;
            Vector3 verticalMovement = new Vector3(0, velocity.y, 0) * Time.fixedDeltaTime;
            
            // 检测水平移动碰撞
            if (horizontalMovement.sqrMagnitude > 0.0001f)
            {
                float distance = horizontalMovement.magnitude;
                Vector3 direction = horizontalMovement.normalized;
                RaycastHit hit;
                
                // 使用 SphereCast 检测前方障碍物（使用 Collider 半径）
                if (Physics.SphereCast(rb.position + Vector3.up * col.height * 0.5f, 
                                      col.radius * 0.8f, 
                                      direction, 
                                      out hit, 
                                      distance + 0.01f))
                {
                    // 如果碰到障碍，缩短移动距离
                    float safeDistance = Mathf.Max(0, hit.distance - 0.01f);
                    horizontalMovement = direction * safeDistance;
                }
            }
            
            // 应用最终位置（水平 + 垂直）
            newPosition = rb.position + horizontalMovement + verticalMovement;
            rb.MovePosition(newPosition);
        }

        /// <summary>
        /// 获取相对于相机的移动方向
        /// </summary>
        private Vector3 GetCameraRelativeMovement(Vector2 input)
        {
            // input.x -> X轴（左右）
            // input.y -> Z轴（上下）
            return new Vector3(input.x, 0, input.y).normalized;
        }

        /// <summary>
        /// 跳跃 (Kinematic模式)
        /// </summary>
        public void Jump()
        {
            if (!Blackboard.CanJump)
                return;

            if (jumpCount >= maxJumpCount)
                return;

            // 直接设置垂直速度，由ApplyGravity处理位置变化
            velocity.y = jumpForce;
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
            // 对于Kinematic Rigidbody，使用我们手动维护的velocity
            Blackboard.Velocity = velocity;
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
        /// 应用击退效果（供外部调用，如技能系统）
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0;
            direction.Normalize();
            
            // 计算击退速度
            knockbackVelocity = direction * force;
            isKnockedBack = true;
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

