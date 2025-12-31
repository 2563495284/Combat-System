using UnityEngine;
using CombatSystem.Core;
using System.Collections.Generic;
using BTree;
namespace Character3C.Enemy
{
    /// <summary>
    /// 2.5D 敌人控制器
    /// 管理敌人的移动、AI行为和战斗逻辑
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(CombatEntity))]
    public class Enemy25DController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float moveSpeed = 3f;
        // [SerializeField] private float acceleration = 15f;
        // [SerializeField] private float deceleration = 15f;
        // [SerializeField] private float gravity = 20f;

        [Header("地面检测")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        // [Header("目标设置")]
        // [SerializeField] private string targetTag = "Player";

        [Header("Sprite设置")]
        [SerializeField] private Transform spriteTransform;

        // 黑板数据
        public EnemyBlackboard Blackboard { get; private set; }

        // 任务管理
        private TaskEntry<EnemyBlackboard> currentTask;

        // 组件引用
        private Rigidbody2D rb;
        private BoxCollider2D col;
        private CombatEntity combatEntity;

        // 移动状态
        private Vector3 velocity;
        private Vector3 moveDirection;
        private bool isGrounded;

        // 击退状态管理
        private Vector3 knockbackVelocity;
        private float knockbackDecay = 10f; // 击退衰减速度
        private bool isKnockedBack = false;

        // 帧计数器
        private int currentFrame = 0;

        public float MoveSpeed => moveSpeed;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            combatEntity = GetComponent<CombatEntity>();

            // 配置Rigidbody2D - XY平面游戏，不使用重力
            rb.mass = 100f; // 质量远大于玩家（玩家mass=1f），防止玩家推动敌人
            rb.linearDamping = 8f; // 阻力大，移动不灵活
            rb.angularDamping = 0f;
            rb.gravityScale = 0f; // 禁用重力（XY平面游戏不需要重力）
            rb.bodyType = RigidbodyType2D.Dynamic; // 动态模式
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 平滑移动
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 连续碰撞检测，防止高速穿透
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 锁定旋转

            // 配置碰撞体 - 使用真实物理碰撞（非Trigger）
            // 这样可以与地面产生真实的物理碰撞，同时通过代码控制角色间碰撞
            col.isTrigger = false;

            // 初始化黑板
            Blackboard = new EnemyBlackboard
            {
                Transform = transform,
                Rigidbody = rb,
                Collider = col,
                CombatEntity = combatEntity,
            };

            // 配置战斗实体
            combatEntity.EntityType = CombatSystem.EntityType.Monster;
            combatEntity.Camp = (int)CombatSystem.CampType.Enemy;
        }

        private void Start()
        {
            // 初始化AI任务 - 已禁用
            SetTask(new Tasks.IdleTask(this));

            // 禁用移动
            Blackboard.CanMove = false;
            Blackboard.MoveDirection = Vector3.zero;

            // 注册死亡事件
            combatEntity.EventBus.Register<CombatSystem.Events.DeathEvent>(OnDeath);
        }

        private void Update()
        {
            if (Blackboard.IsDead)
                return;

            currentFrame++;

            // 更新目标检测 - 已禁用
            // UpdateTargetDetection();

            // 更新黑板数据
            UpdateBlackboard();

            // 更新当前任务 - 已禁用
            currentTask?.Update(currentFrame);

            // 确保敌人不移动
            Blackboard.CanMove = false;
            Blackboard.MoveDirection = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (Blackboard.IsDead)
                return;

            // 检测地面
            CheckGrounded();

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
                }
            }

            // 敌人不受玩家碰撞影响，所以不需要检查blockingColliders
            // 但如果有其他敌人阻挡，可以在这里处理

            if (!Blackboard.CanMove && !isKnockedBack)
            {
                // 不能移动且没有击退时，水平速度归零（X轴）
                velocity.x = 0;
                return;
            }

            moveDirection = Blackboard.MoveDirection;

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                // 直接设置目标速度（X轴水平）
                Vector3 targetVelocity = new Vector3(moveDirection.x * moveSpeed, 0, 0);
                velocity.x = targetVelocity.x;
            }
            else
            {
                // 停止移动
                velocity.x = 0;
            }

            // 更新面向方向
            if (moveDirection.sqrMagnitude > 0.01f && !isKnockedBack)
            {
                Blackboard.FacingDirection = moveDirection.normalized;
            }
        }

        /// <summary>
        /// 计算垂直移动速度（Y轴，手动控制模式）
        /// </summary>
        private void CalculateVerticalMovement()
        {
            // XY平面游戏不使用重力，X轴速度（垂直）由代码手动控制
            // 在地面上时，垂直速度强制为0（防止穿透地面）
            if (isGrounded)
            {
                velocity.x = 0;
            }
            else
            {
                // 在空中时，保持当前的垂直速度（如果没有其他力作用，应该为0）
                velocity.x = rb.linearVelocity.x;
            }
        }

        /// <summary>
        /// 应用移动 - 使用Rigidbody2D.velocity直接控制（XY平面系统，无重力）
        /// </summary>
        private void ApplyMovement()
        {
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
            if (isGrounded)
            {
                // 在地面上时，强制垂直速度为0（防止穿透地面）
                currentVelocity.x = 0;
            }
            else
            {
                // 在空中时，使用计算的垂直速度
                currentVelocity.x = velocity.x;
            }

            rb.linearVelocity = currentVelocity;

            // 如果敌人不应该移动（CanMove为false），强制速度为0
            if (!Blackboard.CanMove && !isKnockedBack)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }

        /// <summary>
        /// 检测角色是否在地面上（Unity 2D XY平面）
        /// 使用Raycast2D向下检测
        /// </summary>
        private void CheckGrounded()
        {
            isGrounded = true;
            return;
            // // 计算检测起点（角色底部中心）
            // // 地面是XY平面，Z轴是垂直方向
            // Vector3 checkPosition = transform.position;

            // // 如果有Collider2D，使用碰撞体底部作为检测起点
            // if (col != null)
            // {
            //     Bounds bounds = col.bounds;
            //     // 底部是Z轴的最小值（bounds.min.z），XY保持中心
            //     checkPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
            // }

            // // 向Z轴正方向（向上）发射射线检测地面（地面是XY平面）
            // RaycastHit hit;
            // bool hasHit = Physics.Raycast(
            //     checkPosition,
            //     Vector3.forward, // Z轴正方向（向上，朝向XY平面）
            //     out hit,
            //     groundCheckDistance,
            //     groundLayer
            // );

            // // 如果检测到地面，且Z轴速度向下或接近0，则认为在地面上
            // isGrounded = hasHit && transform.position.z <= hit.point.z + 0.1f;
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
        public void SetTask(TaskEntry<EnemyBlackboard> task)
        {
            currentTask?.Stop();
            currentTask = task;

            if (currentTask != null)
            {
                currentTask.Blackboard = Blackboard;
                // TaskEntry 会在第一次 Update 时自动启动
            }
        }

        /// <summary>
        /// 获取当前任务
        /// </summary>
        public TaskEntry<EnemyBlackboard> GetTask()
        {
            return currentTask;
        }

        /// <summary>
        /// 死亡处理
        /// </summary>
        private void OnDeath(CombatSystem.Events.DeathEvent evt)
        {
            if (evt.entity != combatEntity)
                return;

            Blackboard.IsDead = true;
            Blackboard.CanMove = false;
            Blackboard.CanAttack = false;
            Blackboard.ChangeState(EnemyAIState.Dead);

            // 停止当前任务
            currentTask?.Stop();

            // 禁用物理
            rb.bodyType = RigidbodyType2D.Kinematic;
            col.enabled = false;

            Debug.Log($"{combatEntity.EntityName} 已死亡");

            // 延迟销毁
            Destroy(gameObject, 2f);
        }

        /// <summary>
        /// 应用击退效果（供外部调用，如技能系统）
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            // 在XY平面系统中，击退只在Y轴（水平方向），不影响X轴（垂直）
            direction.x = 0; // 不影响垂直（X轴是垂直方向）
            direction.Normalize();

            // 先清零当前水平速度（保留垂直速度）
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            velocity.y = 0;

            // 应用击退力
            rb.AddForce(direction * force, ForceMode2D.Impulse);

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
            direction.x = 0; // 保持水平（不影响垂直，X轴是垂直方向）
            ApplyKnockback(direction, force);
        }

        /// <summary>
        /// 物理碰撞检测 - 敌人不受玩家碰撞影响，但需要处理与其他敌人的碰撞
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 如果碰撞到玩家，立即重置速度，防止被推走
            if (collision.collider.CompareTag("Player") && !isKnockedBack)
            {
                // 保持当前应该有的速度（由AI控制的速度，X轴水平）
                Vector2 targetVel = new Vector2(velocity.x, rb.linearVelocity.y);
                rb.linearVelocity = targetVel;
            }
        }

        /// <summary>
        /// 物理碰撞持续检测
        /// </summary>
        private void OnCollisionStay2D(Collision2D collision)
        {
            // 如果持续碰撞到玩家，持续重置速度，防止被推走
            if (collision.collider.CompareTag("Player") && !isKnockedBack)
            {
                // 保持当前应该有的速度（由AI控制的速度，Y轴水平）
                Vector2 targetVel = new Vector2(rb.linearVelocity.x, velocity.y);
                rb.linearVelocity = targetVel;
            }
        }

        /// <summary>
        /// 碰撞离开 - 移除阻挡标记
        /// </summary>
        private void OnCollisionExit2D(Collision2D collision)
        {
        }

        /// <summary>
        /// Trigger检测 - 用于检测玩家（CircleCollider2D设置为Trigger）
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // Trigger检测到玩家，可以用于AI感知、攻击判定等
                // 物理碰撞由非Trigger Collider处理
                Debug.Log($"敌人检测到玩家: {other.name}");
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"玩家离开敌人感知范围: {other.name}");
            }
        }
        /// <summary>
        /// 绘制 Gizmos（用于调试地面检测）
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 绘制地面检测射线（XY平面为地面，Z轴是垂直方向）
            Vector3 checkPosition = transform.position;
            if (col != null)
            {
                Bounds bounds = col.bounds;
                checkPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
            }

            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(checkPosition, Vector3.forward * groundCheckDistance); // Z轴正方向（向上，朝向XY平面）
        }

        private void OnDestroy()
        {
            // 取消事件注册
            combatEntity?.EventBus?.Unregister<CombatSystem.Events.DeathEvent>(OnDeath);
        }
    }
}

