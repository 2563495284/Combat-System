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
        // [SerializeField] private float acceleration = 20f;
        // [SerializeField] private float deceleration = 20f;

        [Header("跳跃参数")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private int maxJumpCount = 1;

        [Header("地面检测")]
        [SerializeField] private float groundCheckDistance = 0.1f; // 地面检测距离
        [SerializeField] private LayerMask groundLayer = 1; // 地面层级（默认所有层）
        [SerializeField] private Vector2 groundCheckOffset = Vector2.zero; // 检测点偏移

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
        private bool isGrounded;
        
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
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            mainCamera = Camera.main;

            // 配置Rigidbody2D - 按照标准2D平台游戏设置
            rb.mass = 1f;
            rb.linearDamping = 5f; // 线性阻力，让移动有惯性
            rb.angularDamping = 0f;
            rb.gravityScale = 0f; // 禁用重力
            rb.bodyType = RigidbodyType2D.Dynamic; // 动态模式
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 平滑移动
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 连续碰撞检测，防止高速穿透
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 锁定旋转

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
            // 检测地面
            CheckGrounded();
            
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
        /// 计算垂直移动速度（Y轴，手动控制模式）
        /// </summary>
        private void CalculateVerticalMovement()
        {
            // XY平面游戏不使用重力，X轴速度（垂直）由代码手动控制
            // 在地面上时重置跳跃计数
            if (isGrounded && rb.linearVelocity.x <= 0.01f)
            {
                jumpCount = 0;
            }
            
            // 如果有跳跃输入，velocity.x会在Jump()方法中设置
            // 否则X轴速度（垂直）保持为0（无重力，不会自动下落）
        }

        /// <summary>
        /// 应用移动 - 使用Rigidbody2D.velocity直接控制（XY平面系统，无重力）
        /// </summary>
        private void ApplyMovement()
        {
            // 如果有阻挡碰撞体且不在击退状态，停止水平移动
            if (blockingColliders.Count > 0 && !isKnockedBack && !ignoreCharacterCollisions)
            {
                velocity.x = 0;
            }

            // 获取当前速度
            Vector2 currentVelocity = rb.linearVelocity;

            // 设置水平速度（Y轴）- XY平面中Y轴是水平方向
            if (isKnockedBack)
            {
                currentVelocity.y = knockbackVelocity.y;
            }
            else
            {
                currentVelocity.y = velocity.y;
            }

            // 处理垂直速度（X轴）- 无重力，手动控制，XY平面中X轴是垂直方向
            if (velocity.x != 0)
            {
                // 使用输入或跳跃速度
                currentVelocity.x = velocity.x;
            }
            else
            {
                // 如果没有输入，强制垂直速度为0（无重力，不会自动下落）
                currentVelocity.x = 0;
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

            // 在地面上才能跳跃
            if (!isGrounded && jumpCount > 0)
                return;

            // 直接设置垂直速度实现跳跃（XY平面系统，无重力）
            // X轴是垂直方向，所以使用velocity.x
            velocity.x = jumpForce;
            jumpCount++;

            Debug.Log($"跳跃 - 次数: {jumpCount}");
        }


        /// <summary>
        /// 检测角色是否在地面上（XY平面为地面，Z轴是垂直方向）
        /// 使用3D物理系统向Z轴正方向检测地面
        /// </summary>
        private void CheckGrounded()
        {
            isGrounded = true;
            return;
            // 计算检测起点（角色底部中心 + 偏移）
            Vector3 checkPosition = transform.position + new Vector3(groundCheckOffset.x, groundCheckOffset.y, 0);
            
            // 如果有BoxCollider2D，使用碰撞体底部作为检测起点
            if (col != null)
            {
                Bounds bounds = col.bounds;
                // 底部是Z轴的最小值（bounds.min.z），XY保持中心
                checkPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z) + new Vector3(groundCheckOffset.x, groundCheckOffset.y, 0);
            }
            
            // 向Z轴正方向（向上）发射射线检测地面（地面是XY平面）
            RaycastHit hit;
            bool hasHit = Physics.Raycast(
                checkPosition,
                Vector3.forward, // Z轴正方向（向上，朝向XY平面）
                out hit,
                groundCheckDistance,
                groundLayer
            );
            
            // 如果检测到地面，且Z坐标接近地面，则认为在地面上
            isGrounded = hasHit && transform.position.z <= hit.point.z + 0.1f;
            
            // 方法2（备选）: 使用BoxCast2D检测脚下区域（更准确，适合有宽度的角色）
            // 如果需要更精确的检测，可以取消注释以下代码
            /*
            if (col != null)
            {
                Bounds bounds = col.bounds;
                Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, groundCheckDistance);
                Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y - groundCheckDistance * 0.5f);
                
                RaycastHit2D boxHit = Physics2D.BoxCast(
                    boxCenter,
                    boxSize,
                    0f,
                    Vector2.down,
                    0f,
                    groundLayer
                );
                
                isGrounded = boxHit.collider != null && rb.linearVelocity.y <= 0.1f;
            }
            */
            
            // 方法3（备选）: 使用OverlapCircle检测脚下圆形区域
            // 如果需要圆形检测，可以取消注释以下代码
            /*
            if (col != null)
            {
                Bounds bounds = col.bounds;
                Vector2 circleCenter = new Vector2(bounds.center.x, bounds.min.y);
                float radius = Mathf.Min(bounds.size.x, bounds.size.y) * 0.5f;
                
                Collider2D overlap = Physics2D.OverlapCircle(
                    circleCenter,
                    radius + groundCheckDistance,
                    groundLayer
                );
                
                isGrounded = overlap != null && rb.linearVelocity.y <= 0.1f;
            }
            */
        }

        /// <summary>
        /// 更新黑板数据
        /// </summary>
        private void UpdateBlackboard()
        {
            // 使用Rigidbody2D的实际速度
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
        public void ApplyKnockback(Vector2 direction, float force)
        {
            // 在XY平面系统中，击退只在Y轴（水平方向），不影响X轴（垂直）
            direction.x = 0; // 不影响垂直（X轴是垂直方向）
            direction.Normalize();
            
            // 先清零当前水平速度（保留垂直速度）
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            velocity.y = 0;
            
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
            direction.x = 0; // 保持水平（不影响垂直，X轴是垂直方向）
            ApplyKnockback(direction, force);
        }

        /// <summary>
        /// 物理碰撞检测 - 与敌人碰撞时停止移动
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.collider, collision, true);
        }

        /// <summary>
        /// 物理碰撞持续检测 - 防止穿过敌人
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleCollision(collision.collider, collision, false);
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
        /// Trigger检测 - 用于检测敌人（CircleCollider2D设置为Trigger）
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                // Trigger检测到敌人，可以用于攻击判定、伤害检测等
                // 物理碰撞由非Trigger Collider处理
                Debug.Log($"检测到敌人: {other.name}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                Debug.Log($"离开敌人范围: {other.name}");
            }
        }

        /// <summary>
        /// 处理碰撞逻辑 - 强制阻止玩家穿过敌人
        /// </summary>
        private void HandleCollision(Collider2D other, Collision2D collision, bool isEnter)
        {
            // 如果正在击退或忽略碰撞，不处理
            if (ignoreCharacterCollisions || isKnockedBack)
                return;

            // 检测与敌人的碰撞
            if (other.CompareTag("Enemy"))
            {
                if (isEnter)
                {
                    blockingColliders.Add(other);
                }

                // 强制阻止玩家穿过敌人 - 计算碰撞法线并推回玩家
                if (collision != null && collision.contactCount > 0)
                {
                    // 获取碰撞接触点
                    ContactPoint2D contact = collision.GetContact(0);
                    Vector2 normal = contact.normal;
                    
                    // 计算玩家相对于敌人的位置
                    Vector2 toEnemy = (Vector2)(other.transform.position - transform.position);
                    
                    // 如果玩家正在朝向敌人移动，强制停止并推回
                    if (Vector2.Dot(moveDirection.normalized, toEnemy.normalized) > 0.1f)
                    {
                        // 立即停止水平移动
                        velocity.x = 0;
                        
                        // 推回玩家，防止重叠
                        Vector2 pushBack = -normal * 0.1f; // 推回距离
                        rb.MovePosition(rb.position + pushBack);
                        
                        // 强制设置速度为0
                        Vector2 currentVel = rb.linearVelocity;
                        currentVel.x = 0;
                        rb.linearVelocity = currentVel;
                    }
                }
            }
        }

        /// <summary>
        /// 绘制 Gizmos（用于调试地面检测）
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 绘制移动方向（X轴水平）
            if (Application.isPlaying && moveDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, moveDirection * 2f);
            }
            
            // 绘制地面检测射线（XY平面为地面，Z轴是垂直方向）
            Vector3 checkPosition = transform.position + new Vector3(groundCheckOffset.x, groundCheckOffset.y, 0);
            if (col != null)
            {
                Bounds bounds = col.bounds;
                checkPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z) + new Vector3(groundCheckOffset.x, groundCheckOffset.y, 0);
            }
            
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(checkPosition, Vector3.forward * groundCheckDistance); // Z轴正方向（向上，朝向XY平面）
        }
    }
}

