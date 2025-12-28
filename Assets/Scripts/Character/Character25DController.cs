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
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Character25DController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float deceleration = 20f;

        [Header("跳跃参数")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private int maxJumpCount = 1;

        // 黑板数据
        public CharacterBlackboard25D Blackboard { get; private set; }

        // 任务管理
        private TaskEntry<CharacterBlackboard25D> currentTask;

        // 组件引用
        private Rigidbody2D rb;
        private BoxCollider2D col;

        // 移动状态
        private Vector2 velocity;
        private Vector2 moveDirection;
        private int jumpCount;
        
        // 击退状态管理
        private Vector2 knockbackVelocity;
        private float knockbackDecay = 10f; // 击退衰减速度
        private bool isKnockedBack = false;
        
        // 碰撞处理
        private HashSet<Collider2D> blockingColliders = new HashSet<Collider2D>(); // 阻挡玩家移动的碰撞体
        private bool ignoreCharacterCollisions = false; // 是否忽略角色间碰撞（用于技能强制位移）
        private HashSet<Collider2D> ignoredColliders = new HashSet<Collider2D>(); // 临时忽略的碰撞体（用于击退）

        // 相机引用（用于计算移动方向）
        private Camera mainCamera;

        public float MoveSpeed => moveSpeed;
        public float JumpForce => jumpForce;
        public bool IsGrounded => rb.IsTouchingLayers(); // 2D物理自动检测地面碰撞

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            mainCamera = Camera.main;

            // 配置Rigidbody2D - 使用物理模式（非Kinematic）
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.gravityScale = 0f; // 禁用重力（2D游戏不需要重力）
            rb.bodyType = RigidbodyType2D.Dynamic; // 动态模式：受物理力影响
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 平滑移动

            // 配置碰撞体 - 使用真实物理碰撞（非Trigger）
            // 2D物理会自动处理地面碰撞，不需要手动检测
            col.isTrigger = false;

            // 初始化黑板
            Blackboard = new CharacterBlackboard25D
            {
                Transform = transform,
                Rigidbody = null, // 2D系统不使用3D Rigidbody
                Collider = null  // 2D系统不使用3D Collider
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
            // 2D物理自动处理地面碰撞，不需要手动检测
            CalculateHorizontalMovement();
            CalculateVerticalMovement();
            ApplyMovement();
        }

        /// <summary>
        /// 计算水平移动速度（使用Rigidbody2D.velocity）
        /// </summary>
        private void CalculateHorizontalMovement()
        {
            // 处理击退衰减
            if (isKnockedBack)
            {
                knockbackVelocity = Vector2.MoveTowards(knockbackVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
                if (knockbackVelocity.sqrMagnitude < 0.01f)
                {
                    knockbackVelocity = Vector2.zero;
                    isKnockedBack = false;
                    ignoreCharacterCollisions = false; // 击退结束后恢复碰撞检测
                    
                    // 恢复所有被忽略的碰撞
                    foreach (var ignoredCol in ignoredColliders)
                    {
                        if (ignoredCol != null)
                        {
                            Physics2D.IgnoreCollision(col, ignoredCol, false);
                        }
                    }
                    ignoredColliders.Clear();
                }
            }

            if (!Blackboard.CanMove && !isKnockedBack)
            {
                // 不能移动且没有击退时，速度归零（X轴）
                velocity.x = 0;
                return;
            }

            // 获取输入方向（相对于相机）
            Vector2 input = Blackboard.InputMove;
            if (input.sqrMagnitude > 0.01f)
            {
                // 根据相机方向计算移动方向（XY平面）
                // input.x -> X轴（左右）
                // input.y -> Y轴（上下）
                moveDirection = GetCameraRelativeMovement(input);

                // 计算目标速度（X轴和Y轴）
                Vector2 targetVelocity = moveDirection * moveSpeed;
                
                // 如果有阻挡碰撞体，简单检查移动方向是否朝向阻挡物
                if (blockingColliders.Count > 0 && !isKnockedBack && !ignoreCharacterCollisions)
                {
                    foreach (var blockingCol in blockingColliders)
                    {
                        if (blockingCol == null) continue;
                        
                        // 计算从玩家到阻挡物的方向
                        Vector2 toBlocking = (Vector2)(blockingCol.transform.position - transform.position);
                        
                        if (toBlocking.sqrMagnitude > 0.01f)
                        {
                            // 如果移动方向朝向阻挡物（点积 > 0），阻止移动
                            float dot = Vector2.Dot(moveDirection.normalized, toBlocking.normalized);
                            if (dot > 0.1f) // 朝向阻挡物
                            {
                                // 阻止移动
                                targetVelocity = Vector2.zero;
                                break;
                            }
                        }
                    }
                }
                
                velocity.x = targetVelocity.x;
                velocity.y = targetVelocity.y; // 添加Y轴速度支持
            }
            else
            {
                // 停止移动
                velocity.x = 0;
                velocity.y = 0; // 停止Y轴移动
            }

            // 更新面向方向（但不旋转，因为使用Billboard）
            if (moveDirection.sqrMagnitude > 0.01f && !isKnockedBack)
            {
                Blackboard.FacingDirection = moveDirection;
            }
        }

        /// <summary>
        /// 计算垂直移动速度（Y轴，无重力系统）
        /// </summary>
        private void CalculateVerticalMovement()
        {
            // 2D游戏不需要重力，Y轴速度由输入控制
            // Y轴速度已经在CalculateHorizontalMovement中设置了（通过WS键输入）
            // 这里只需要处理跳跃相关的逻辑
            
            if (rb.linearVelocity.y <= 0.01f && rb.linearVelocity.y >= -0.01f)
            {
                jumpCount = 0; // 重置跳跃计数
            }
            
            // 如果有跳跃速度，使用跳跃速度；否则使用输入速度
            // velocity.y已经在CalculateHorizontalMovement中设置了
        }

        /// <summary>
        /// 应用移动 - 使用Rigidbody2D.velocity直接控制（XY平面系统）
        /// </summary>
        private void ApplyMovement()
        {
            // 计算水平速度（X轴，包含击退）
            Vector2 horizontalVelocity = new Vector2(velocity.x, 0);
            if (isKnockedBack)
            {
                // 击退时，使用击退速度（X轴）
                horizontalVelocity = new Vector2(knockbackVelocity.x, 0);
            }

            // 获取当前Rigidbody2D的速度
            Vector2 currentVelocity = rb.linearVelocity;
            
            // 设置水平速度（X轴）
            currentVelocity.x = horizontalVelocity.x;
            
            // 处理垂直速度（Y轴）- 无重力，由输入或跳跃控制
            if (velocity.y != 0)
            {
                // 使用输入或跳跃速度
                currentVelocity.y = velocity.y;
            }
            else
            {
                // 如果没有输入，强制垂直速度为0（无重力，不会自动下落）
                currentVelocity.y = 0;
            }
            
            rb.linearVelocity = currentVelocity;
        }

        /// <summary>
        /// 获取相对于相机的移动方向（XY平面）
        /// </summary>
        private Vector2 GetCameraRelativeMovement(Vector2 input)
        {
            // input.x -> X轴（左右）
            // input.y -> Y轴（上下），但在2.5D中通常只用于跳跃
            // 水平移动主要在X轴，Y轴用于垂直移动（跳跃）
            return input.normalized;
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
            // 在XY平面系统中，跳跃是沿Y轴（垂直方向）向上
            Vector2 jumpVelocity = rb.linearVelocity;
            jumpVelocity.y = jumpForce;
            rb.linearVelocity = jumpVelocity;
            
            // 更新内部速度记录
            velocity.y = jumpForce;
            jumpCount++;

            Debug.Log($"跳跃 - 次数: {jumpCount}");
        }


        /// <summary>
        /// 更新黑板数据
        /// </summary>
        private void UpdateBlackboard()
        {
            // 使用Rigidbody2D的实际速度
            Blackboard.Velocity = rb.linearVelocity;
            Blackboard.IsGrounded = IsGrounded;
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
        public void ApplyKnockback(Vector2 direction, float force)
        {
            // 在XY平面系统中，击退只在X轴（水平方向），不影响Y轴（垂直）
            direction.y = 0; // 不影响垂直
            direction.Normalize();
            
            // 先清零当前水平速度（保留垂直速度）
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            velocity.x = 0;
            
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
                    Physics2D.IgnoreCollision(col, character.col, true);
                    ignoredColliders.Add(character.col);
                }
            }
            
            var allEnemies = FindObjectsByType<Character3C.Enemy.Enemy25DController>(FindObjectsSortMode.None);
            foreach (var enemy in allEnemies)
            {
                if (enemy != null && enemy.GetComponent<Collider2D>() != null)
                {
                    Collider2D enemyCol = enemy.GetComponent<Collider2D>();
                    Physics2D.IgnoreCollision(col, enemyCol, true);
                    ignoredColliders.Add(enemyCol);
                }
            }
            
            // 应用击退力
            rb.AddForce(direction * force, ForceMode2D.Impulse);
            
            // 记录击退速度用于衰减
            knockbackVelocity = direction * force;
            isKnockedBack = true;
        }

        /// <summary>
        /// 被击退的方法（从特定位置）
        /// </summary>
        public void TakeKnockback(Vector2 fromPosition, float force)
        {
            Vector2 direction = ((Vector2)transform.position - fromPosition).normalized;
            direction.y = 0; // 保持水平（不影响垂直）
            ApplyKnockback(direction, force);
        }

        /// <summary>
        /// 物理碰撞检测 - 与敌人碰撞时停止移动
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.collider, true);
        }

        /// <summary>
        /// 物理碰撞持续检测 - 防止穿过敌人
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleCollision(collision.collider, false);
        }

        /// <summary>
        /// 碰撞离开 - 移除阻挡标记
        /// </summary>
        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Enemy"))
            {
                blockingColliders.Remove(collision.collider);
            }
        }

        /// <summary>
        /// 处理碰撞逻辑 - 简化版本：只记录碰撞体，不做其他处理
        /// </summary>
        private void HandleCollision(Collider2D other, bool isEnter)
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
            // 绘制移动方向（X轴水平）
            if (Application.isPlaying && moveDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, moveDirection * 2f);
            }
        }
    }
}

