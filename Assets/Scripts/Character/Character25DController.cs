    using UnityEngine;
using CombatSystem.Core;
using System.Collections.Generic;

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
        
        // 碰撞处理
        private HashSet<Collider> blockingColliders = new HashSet<Collider>(); // 阻挡玩家移动的碰撞体
        private bool ignoreCharacterCollisions = false; // 是否忽略角色间碰撞（用于技能强制位移）
        private HashSet<Collider> ignoredColliders = new HashSet<Collider>(); // 临时忽略的碰撞体（用于击退）

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

            // 配置Rigidbody - 使用物理模式（非Kinematic）
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.useGravity = true; // 使用Unity系统重力
            rb.isKinematic = false; // 非运动学模式：受物理力影响
            rb.interpolation = RigidbodyInterpolation.Interpolate; // 平滑移动
            rb.constraints = RigidbodyConstraints.FreezeRotation; // 只锁定旋转，Y轴由重力控制

            // 配置碰撞体 - 使用真实物理碰撞（非Trigger）
            // 这样可以与地面产生真实的物理碰撞，同时通过代码控制角色间碰撞
            col.isTrigger = false;
            col.material = null; // 使用默认物理材质（无摩擦力，无弹跳）

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
        /// 计算水平移动速度（使用Rigidbody.velocity）
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
                    ignoreCharacterCollisions = false; // 击退结束后恢复碰撞检测
                    
                    // 恢复所有被忽略的碰撞
                    foreach (var ignoredCol in ignoredColliders)
                    {
                        if (ignoredCol != null)
                        {
                            Physics.IgnoreCollision(col, ignoredCol, false);
                        }
                    }
                    ignoredColliders.Clear();
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

                // 计算目标速度
                Vector3 targetVelocity = moveDirection * moveSpeed;
                
                // 如果有阻挡碰撞体，简单检查移动方向是否朝向阻挡物
                if (blockingColliders.Count > 0 && !isKnockedBack && !ignoreCharacterCollisions)
                {
                    foreach (var blockingCol in blockingColliders)
                    {
                        if (blockingCol == null) continue;
                        
                        // 计算从玩家到阻挡物的方向
                        Vector3 toBlocking = (blockingCol.transform.position - transform.position);
                        toBlocking.y = 0;
                        
                        if (toBlocking.sqrMagnitude > 0.01f)
                        {
                            // 如果移动方向朝向阻挡物（点积 > 0），阻止移动
                            float dot = Vector3.Dot(moveDirection.normalized, toBlocking.normalized);
                            if (dot > 0.1f) // 朝向阻挡物
                            {
                                // 阻止移动
                                targetVelocity = Vector3.zero;
                                break;
                            }
                        }
                    }
                }
                
                velocity.x = targetVelocity.x;
                velocity.z = targetVelocity.z;
            }
            else
            {
                // 停止移动
                velocity.x = 0;
                velocity.z = 0;
            }

            // 更新面向方向（但不旋转，因为使用Billboard）
            if (moveDirection.sqrMagnitude > 0.01f && !isKnockedBack)
            {
                Blackboard.FacingDirection = moveDirection.normalized;
            }
        }

        /// <summary>
        /// 计算垂直移动速度（使用Unity系统重力）
        /// </summary>
        private void CalculateVerticalMovement()
        {
            // 使用Unity系统重力，不需要手动计算重力
            // 只需要在地面上时重置垂直速度，并更新跳跃计数
            if (isGrounded)
            {
                // 在地面上时，如果垂直速度向下，重置为0（防止穿透地面）
                if (rb.linearVelocity.y < 0)
                {
                    velocity.y = 0;
                }
                else
                {
                    // 保持当前的垂直速度（可能是跳跃后的上升速度）
                    velocity.y = rb.linearVelocity.y;
                }
                
                // 只有在真正贴地时才重置跳跃计数
                if (rb.linearVelocity.y <= 0.01f)
                {
                    jumpCount = 0;
                }
            }
            else
            {
                // 在空中时，使用Rigidbody的实际垂直速度（由Unity重力控制）
                velocity.y = rb.linearVelocity.y;
            }
        }

        /// <summary>
        /// 应用移动 - 使用Rigidbody.velocity直接控制
        /// </summary>
        private void ApplyMovement()
        {
            // 计算水平速度（包含击退）
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            if (isKnockedBack)
            {
                // 击退时，使用击退速度
                horizontalVelocity = knockbackVelocity;
            }

            // 获取当前Rigidbody的速度（包含Unity重力系统影响的垂直速度）
            Vector3 currentVelocity = rb.linearVelocity;
            
            // 设置水平速度
            currentVelocity.x = horizontalVelocity.x;
            currentVelocity.z = horizontalVelocity.z;
            
            // 处理垂直速度
            if (isGrounded)
            {
                // 在地面上时，如果正在跳跃（velocity.y > 0），使用跳跃速度
                if (velocity.y > 0)
                {
                    currentVelocity.y = velocity.y;
                }
                else
                {
                    // 否则强制垂直速度为0（防止穿透地面）
                    currentVelocity.y = 0;
                }
            }
            // 如果设置了跳跃速度，使用跳跃速度
            else if (velocity.y > 0)
            {
                currentVelocity.y = velocity.y;
            }
            // 否则保持Unity重力系统计算的垂直速度（已经在 currentVelocity.y 中）
            
            rb.linearVelocity = currentVelocity;
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
        /// 跳跃（使用AddForce或直接设置垂直速度）
        /// </summary>
        public void Jump()
        {
            if (!Blackboard.CanJump)
                return;

            if (jumpCount >= maxJumpCount)
                return;

            // 使用AddForce应用跳跃力（与Unity重力系统配合）
            Vector3 jumpVelocity = rb.linearVelocity;
            jumpVelocity.y = jumpForce;
            rb.linearVelocity = jumpVelocity;
            
            // 更新内部速度记录
            velocity.y = jumpForce;
            jumpCount++;

            Debug.Log($"跳跃 - 次数: {jumpCount}");
        }

        /// <summary>
        /// 更新地面状态
        /// </summary>
        private void UpdateGroundedState()
        {
            // 使用 SphereCast 进行地面检测（用于判断是否在地面上）
            Vector3 origin = transform.position + Vector3.up * (col.height * 0.5f);
            float checkDistance = groundCheckDistance + col.height * 0.5f;
            
            RaycastHit hit;
            isGrounded = Physics.SphereCast(origin, col.radius * 0.9f, Vector3.down, out hit, checkDistance, groundLayer);
            
            // 注意：真实的地面碰撞由Unity物理引擎自动处理，这里只用于状态判断
            // 如果检测到地面，确保垂直速度被正确重置（在ApplyMovement中处理）
        }

        /// <summary>
        /// 更新黑板数据
        /// </summary>
        private void UpdateBlackboard()
        {
            // 使用Rigidbody的实际速度
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
        /// 应用击退效果（供外部调用，如技能系统）
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            direction.y = 0;
            direction.Normalize();
            
            // 先清零当前速度
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // 保留垂直速度
            velocity.x = 0;
            velocity.z = 0;
            
            // 清除阻挡碰撞体（允许击退时穿透）
            blockingColliders.Clear();
            ignoreCharacterCollisions = true; // 临时忽略角色间碰撞
            
            // 临时忽略所有角色碰撞体（允许穿透，但不影响地面碰撞）
            // 查找场景中所有角色碰撞体并临时忽略
            var allCharacters = FindObjectsByType<Character25DController>(FindObjectsSortMode.None);
            foreach (var character in allCharacters)
            {
                if (character != this && character.col != null)
                {
                    Physics.IgnoreCollision(col, character.col, true);
                    ignoredColliders.Add(character.col);
                }
            }
            
            var allEnemies = FindObjectsByType<Character3C.Enemy.Enemy25DController>(FindObjectsSortMode.None);
            foreach (var enemy in allEnemies)
            {
                if (enemy != null && enemy.GetComponent<Collider>() != null)
                {
                    Collider enemyCol = enemy.GetComponent<Collider>();
                    Physics.IgnoreCollision(col, enemyCol, true);
                    ignoredColliders.Add(enemyCol);
                }
            }
            
            // 应用击退力
            rb.AddForce(direction * force, ForceMode.VelocityChange);
            
            // 记录击退速度用于衰减
            knockbackVelocity = direction * force;
            isKnockedBack = true;
        }

        /// <summary>
        /// 被击退的方法（从特定位置）
        /// </summary>
        public void TakeKnockback(Vector3 fromPosition, float force)
        {
            Vector3 direction = (transform.position - fromPosition).normalized;
            direction.y = 0; // 保持水平
            ApplyKnockback(direction, force);
        }

        /// <summary>
        /// 物理碰撞检测 - 与敌人碰撞时停止移动
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision.collider, true);
        }

        /// <summary>
        /// 物理碰撞持续检测 - 防止穿过敌人
        /// </summary>
        private void OnCollisionStay(Collision collision)
        {
            HandleCollision(collision.collider, false);
        }

        /// <summary>
        /// 碰撞离开 - 移除阻挡标记
        /// </summary>
        private void OnCollisionExit(Collision collision)
        {
            if (collision.collider.CompareTag("Enemy"))
            {
                blockingColliders.Remove(collision.collider);
            }
        }

        /// <summary>
        /// 处理碰撞逻辑 - 简化版本：只记录碰撞体，不做其他处理
        /// </summary>
        private void HandleCollision(Collider other, bool isEnter)
        {
            // 如果正在击退或忽略碰撞，不处理
            if (ignoreCharacterCollisions || isKnockedBack)
                return;

            // 检测与敌人的碰撞，只记录碰撞体
            if (other.CompareTag("Enemy"))
            {
                if (isEnter)
                {
                    blockingColliders.Add(other);
                }
            }
        }

        /// <summary>
        /// 绘制 Gizmos
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 绘制地面检测范围
            float colHeight = col != null ? col.height : 1f;
            float colRadius = col != null ? col.radius : 0.5f;
            Vector3 origin = transform.position + Vector3.up * (colHeight * 0.5f);
            float checkDistance = groundCheckDistance + colHeight * 0.5f;
            
            Gizmos.color = isGrounded ? Color.green : Color.red;
            // 绘制 SphereCast 的起点和方向
            Gizmos.DrawWireSphere(origin, colRadius * 0.9f);
            Gizmos.DrawRay(origin, Vector3.down * checkDistance);
            // 绘制检测终点
            Gizmos.DrawWireSphere(origin + Vector3.down * checkDistance, colRadius * 0.9f);

            // 绘制移动方向
            if (Application.isPlaying && moveDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position + Vector3.up, moveDirection * 2f);
            }
        }
    }
}

