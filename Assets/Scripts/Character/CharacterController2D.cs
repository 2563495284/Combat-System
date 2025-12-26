using UnityEngine;
using CombatSystem.Core;

namespace Character3C
{
    /// <summary>
    /// 2D 角色控制器
    /// 管理角色的物理运动和状态
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class CharacterController2D : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 50f;
        [SerializeField] private float airControlFactor = 0.6f;

        [Header("跳跃参数")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float fallGravityMultiplier = 2f;
        [SerializeField] private int maxJumpCount = 2;
        [SerializeField] private float coyoteTimeDuration = 0.15f;
        [SerializeField] private float jumpBufferDuration = 0.15f;

        [Header("冲刺参数")]
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1f;

        [Header("地面检测")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        [Header("组件引用")]
        [SerializeField] private CharacterAnimator characterAnimator;

        // 黑板数据
        public CharacterBlackboard Blackboard { get; private set; }

        // 任务管理
        private TaskEntry<CharacterBlackboard> currentTask;

        // 组件引用
        private Rigidbody2D rb;
        private Collider2D col;

        // 物理参数
        private float defaultGravityScale;

        public float MoveSpeed => moveSpeed;
        public float JumpForce => jumpForce;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;
        public int MaxJumpCount => maxJumpCount;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            defaultGravityScale = rb.gravityScale;

            // 初始化黑板
            Blackboard = new CharacterBlackboard
            {
                Transform = transform,
                Rigidbody = rb,
                Collider = col
            };

            // 如果没有指定动画器，尝试自动获取
            if (characterAnimator == null)
            {
                characterAnimator = GetComponent<CharacterAnimator>();
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // 更新地面检测
            UpdateGroundedState();

            // 更新 Coyote Time
            if (Blackboard.IsGrounded)
            {
                Blackboard.LastGroundedTime = Time.time;
                Blackboard.JumpCount = 0;
            }

            Blackboard.CoyoteTime = Time.time - Blackboard.LastGroundedTime;

            // 更新跳跃缓冲
            if (Blackboard.JumpBufferTime > 0)
            {
                Blackboard.JumpBufferTime -= deltaTime;
            }

            // 更新冲刺
            if (Blackboard.IsDashing)
            {
                Blackboard.DashTimeLeft -= deltaTime;
                if (Blackboard.DashTimeLeft <= 0)
                {
                    EndDash();
                }
            }

            // 更新当前任务
            currentTask?.Update(deltaTime);

            // 更新动画
            characterAnimator?.UpdateAnimations(Blackboard);

            // 重置单帧输入标记
            Blackboard.ResetInputFlags();
        }

        private void FixedUpdate()
        {
            if (!Blackboard.IsDashing)
            {
                ApplyMovement();
                ApplyGravity();
            }
            else
            {
                ApplyDash();
            }

            // 更新速度到黑板
            Blackboard.Velocity = rb.linearVelocity;
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

            float targetSpeed = Blackboard.InputMove.x * moveSpeed;
            float currentSpeed = rb.linearVelocity.x;

            // 计算加速度
            float speedDiff = targetSpeed - currentSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            // 空中控制减弱
            if (!Blackboard.IsGrounded)
            {
                accelRate *= airControlFactor;
            }

            float movement = speedDiff * accelRate * Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(currentSpeed + movement, rb.linearVelocity.y);

            // 更新朝向
            if (Mathf.Abs(Blackboard.InputMove.x) > 0.01f)
            {
                bool shouldFaceRight = Blackboard.InputMove.x > 0;
                if (shouldFaceRight != Blackboard.IsFacingRight)
                {
                    Flip();
                }
            }
        }

        /// <summary>
        /// 应用重力
        /// </summary>
        private void ApplyGravity()
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
            }
            else
            {
                rb.gravityScale = defaultGravityScale;
            }
        }

        /// <summary>
        /// 跳跃
        /// </summary>
        public void Jump()
        {
            if (!Blackboard.CanJump)
            {
                Blackboard.JumpBufferTime = jumpBufferDuration;
                return;
            }

            // 检查是否可以跳跃
            bool canCoyoteJump = Blackboard.CoyoteTime < coyoteTimeDuration;
            bool canMultiJump = Blackboard.JumpCount < maxJumpCount;

            if (!Blackboard.IsGrounded && !canCoyoteJump && !canMultiJump)
            {
                Blackboard.JumpBufferTime = jumpBufferDuration;
                return;
            }

            // 执行跳跃
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            Blackboard.JumpCount++;
            Blackboard.JumpBufferTime = 0;

            // 触发跳跃事件
            characterAnimator?.TriggerJump();
        }

        /// <summary>
        /// 跳跃中断（松开跳跃键）
        /// </summary>
        public void CutJump()
        {
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        /// <summary>
        /// 冲刺
        /// </summary>
        public void Dash()
        {
            if (!Blackboard.CanDash || Blackboard.IsDashing)
                return;

            // 检查冷却
            if (Time.time - Blackboard.LastDashTime < dashCooldown)
                return;

            // 确定冲刺方向
            Vector2 dashDir = Blackboard.InputMove;
            if (dashDir.sqrMagnitude < 0.01f)
            {
                dashDir = Blackboard.IsFacingRight ? Vector2.right : Vector2.left;
            }
            else
            {
                dashDir.Normalize();
            }

            // 开始冲刺
            Blackboard.IsDashing = true;
            Blackboard.DashDirection = dashDir;
            Blackboard.DashTimeLeft = dashDuration;
            Blackboard.LastDashTime = Time.time;

            // 重置重力
            rb.gravityScale = 0;

            // 触发冲刺事件
            characterAnimator?.TriggerDash();
        }

        /// <summary>
        /// 应用冲刺运动
        /// </summary>
        private void ApplyDash()
        {
            rb.linearVelocity = Blackboard.DashDirection * dashSpeed;
        }

        /// <summary>
        /// 结束冲刺
        /// </summary>
        private void EndDash()
        {
            Blackboard.IsDashing = false;
            rb.gravityScale = defaultGravityScale;
            rb.linearVelocity = new Vector2(Blackboard.InputMove.x * moveSpeed, rb.linearVelocity.y * 0.5f);
        }

        /// <summary>
        /// 翻转角色
        /// </summary>
        private void Flip()
        {
            Blackboard.IsFacingRight = !Blackboard.IsFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }

        /// <summary>
        /// 更新地面状态
        /// </summary>
        private void UpdateGroundedState()
        {
            if (groundCheck == null)
            {
                groundCheck = transform;
            }

            Blackboard.IsGrounded = Physics2D.OverlapBox(
                groundCheck.position,
                groundCheckSize,
                0f,
                groundLayer
            );
        }

        /// <summary>
        /// 设置当前任务
        /// </summary>
        public void SetTask(TaskEntry<CharacterBlackboard> task)
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
        public TaskEntry<CharacterBlackboard> GetTask()
        {
            return currentTask;
        }

        /// <summary>
        /// 绘制 Gizmos
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Blackboard != null && Blackboard.IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
            }
        }
    }
}

