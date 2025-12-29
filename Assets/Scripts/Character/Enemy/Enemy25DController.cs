using UnityEngine;
using CombatSystem.Core;
using System.Collections.Generic;

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

        // 碰撞处理
        private HashSet<Collider> blockingColliders = new HashSet<Collider>(); // 阻挡敌人移动的碰撞体
        private bool ignoreCharacterCollisions = false; // 是否忽略角色间碰撞（用于技能强制位移）

        public float MoveSpeed => moveSpeed;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            combatEntity = GetComponent<CombatEntity>();

            // 配置Rigidbody - 使用物理模式（非Kinematic）
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 平滑移动
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 只锁定旋转，Z轴由重力控制

            // 配置碰撞体 - 使用真实物理碰撞（非Trigger）
            // 这样可以与地面产生真实的物理碰撞，同时通过代码控制角色间碰撞
            col.isTrigger = false;

            // 初始化黑板
            Blackboard = new EnemyBlackboard
            {
                Transform = transform,
                Rigidbody = null,
                Collider = null,
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

            float deltaTime = Time.deltaTime;

            // 更新目标检测 - 已禁用
            // UpdateTargetDetection();

            // 更新黑板数据
            UpdateBlackboard();

            // 更新当前任务 - 已禁用
            currentTask?.Update(deltaTime);

            // 确保敌人不移动
            Blackboard.CanMove = false;
            Blackboard.MoveDirection = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (Blackboard.IsDead)
                return;

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
        /// 计算垂直移动速度（Y轴，使用Unity系统重力）
        /// </summary>
        private void CalculateVerticalMovement()
        {
            // 使用Unity系统重力，不需要手动计算重力
            // 只需要在地面上时重置垂直速度
            // 注意：在XY平面系统中，Y轴是垂直方向（重力向下），Z轴是深度方向（用于2.5D渲染层次）
            if (isGrounded)
            {
                // 在地面上时，如果垂直速度向下（负Y），重置为0（防止穿透地面）
                if (rb.linearVelocity.y < 0)
                {
                    velocity.y = 0;
                }
                else
                {
                    // 保持当前的垂直速度
                    velocity.y = rb.linearVelocity.y;
                }
            }
            else
            {
                // 在空中时，使用Rigidbody的实际垂直速度（由Unity重力控制）
                velocity.y = rb.linearVelocity.y;
            }
        }

        /// <summary>
        /// 应用移动 - 使用Rigidbody.velocity直接控制（XY平面系统）
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

            // 获取当前Rigidbody的速度（包含Unity重力系统影响的垂直速度）
            Vector3 currentVelocity = rb.linearVelocity;

            // 设置水平速度（X轴）
            currentVelocity.x = horizontalVelocity.x;

            // 在地面上时，强制垂直速度为0（防止穿透地面）
            if (isGrounded)
            {
                currentVelocity.y = 0;
            }
            // 否则保持Unity重力系统计算的垂直速度（已经在 currentVelocity.y 中）

            // Z轴是深度方向，保持原值（用于2.5D渲染层次，不影响物理）
            // currentVelocity.z 保持不变

            rb.linearVelocity = currentVelocity;

            // 如果敌人不应该移动（CanMove为false），强制速度为0
            if (!Blackboard.CanMove && !isKnockedBack)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
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
                currentTask.Start();
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
            rb.isKinematic = true;
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
            // 在XY平面系统中，击退只在X轴（水平方向），不影响Y轴（垂直）和Z轴（深度）
            direction.y = 0; // 不影响垂直
            direction.z = 0; // 不影响深度
            direction.Normalize();

            // 先清零当前水平速度（保留垂直速度和深度）
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            velocity.x = 0;

            // 清除阻挡碰撞体（允许击退时穿透）
            blockingColliders.Clear();
            ignoreCharacterCollisions = true; // 临时忽略角色间碰撞

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
            direction.y = 0; // 保持水平（不影响垂直）
            direction.z = 0; // 保持水平（不影响深度）
            ApplyKnockback(direction, force);
        }

        /// <summary>
        /// 物理碰撞检测 - 敌人不受玩家碰撞影响，但需要处理与其他敌人的碰撞
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision.collider, true);

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
        private void OnCollisionStay(Collision collision)
        {
            HandleCollision(collision.collider, false);

            // 如果持续碰撞到玩家，持续重置速度，防止被推走
            if (collision.collider.CompareTag("Player") && !isKnockedBack)
            {
                // 保持当前应该有的速度（由AI控制的速度，X轴水平）
                Vector2 targetVel = new Vector2(velocity.x, rb.linearVelocity.y);
                rb.linearVelocity = targetVel;
            }
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
        /// 处理碰撞逻辑
        /// </summary>
        private void HandleCollision(Collider other, bool isEnter)
        {
            // 如果正在击退或忽略碰撞，不处理
            if (ignoreCharacterCollisions || isKnockedBack)
                return;

            // 如果碰撞到玩家，强制保持当前速度，防止被物理引擎推走
            if (other.CompareTag("Player"))
            {
                // 保持敌人的当前速度，不被物理碰撞影响
                // 在下一帧的ApplyMovement中会重新设置速度
                // 这里不做任何处理，让敌人保持固定
            }

            // 敌人不受玩家碰撞影响（玩家会自己停下）
            // 敌人也不受其他敌人碰撞影响，保持固定不动
            if (other.CompareTag("Enemy") && other.gameObject != gameObject)
            {
                if (isEnter)
                {
                    blockingColliders.Add(other);
                }

                // 不推离敌人，保持固定不动
                // 如果需要防止重叠，可以在玩家那边处理
            }
        }

        private void OnDestroy()
        {
            // 取消事件注册
            combatEntity?.EventBus?.Unregister<CombatSystem.Events.DeathEvent>(OnDeath);
        }
    }
}

