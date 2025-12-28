using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Enemy
{
    /// <summary>
    /// 2.5D 敌人控制器
    /// 管理敌人的移动、AI行为和战斗逻辑
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CombatEntity))]
    public class Enemy25DController : MonoBehaviour
    {
        [Header("移动参数")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float acceleration = 15f;
        [SerializeField] private float deceleration = 15f;
        [SerializeField] private float gravity = 20f;
        
        [Header("AI参数")]
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float attackRadius = 2f;
        [SerializeField] private float loseTargetRadius = 15f;
        
        [Header("攻击参数")]
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 2f;
        
        [Header("巡逻参数")]
        [SerializeField] private bool enablePatrol = true;
        [SerializeField] private float patrolRadius = 5f;
        [SerializeField] private float patrolWaitTime = 2f;
        
        [Header("地面检测")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayer;
        
        [Header("目标设置")]
        [SerializeField] private string targetTag = "Player";
        
        [Header("Sprite设置")]
        [SerializeField] private bool autoSetupBillboard = true;
        [SerializeField] private Transform spriteTransform;
        
        // 黑板数据
        public EnemyBlackboard Blackboard { get; private set; }
        
        // 任务管理
        private TaskEntry<EnemyBlackboard> currentTask;
        
        // 组件引用
        private Rigidbody rb;
        private CapsuleCollider col;
        private CombatEntity combatEntity;
        
        // 移动状态
        private Vector3 velocity;
        private Vector3 moveDirection;
        private bool isGrounded;
        
        // 击退状态管理
        private Vector3 knockbackVelocity;
        private float knockbackDecay = 10f; // 击退衰减速度
        private bool isKnockedBack = false;
        
        public float MoveSpeed => moveSpeed;
        public bool IsGrounded => isGrounded;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();
            combatEntity = GetComponent<CombatEntity>();
            
            // 配置Rigidbody - 使用物理模式（非Kinematic）
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.useGravity = true; // 使用Unity系统重力
            rb.isKinematic = false; // 非运动学模式：受物理力影响
            rb.interpolation = RigidbodyInterpolation.Interpolate; // 平滑移动
            rb.constraints = RigidbodyConstraints.FreezeRotation; // 只锁定旋转，Y轴由重力控制

            // 配置碰撞体 - 设置为Trigger
            col.isTrigger = true;
            
            // 初始化黑板
            Blackboard = new EnemyBlackboard
            {
                Transform = transform,
                Rigidbody = rb,
                Collider = col,
                CombatEntity = combatEntity,
                SpawnPosition = transform.position,
                DetectionRadius = detectionRadius,
                AttackRadius = attackRadius,
                LoseTargetRadius = loseTargetRadius,
                AttackDamage = attackDamage,
                AttackCooldown = attackCooldown,
                PatrolRadius = patrolRadius,
                PatrolWaitTime = patrolWaitTime
            };
            
            // 配置战斗实体
            combatEntity.EntityType = CombatSystem.EntityType.Monster;
            combatEntity.Camp = (int)CombatSystem.CampType.Enemy;
        }
        
        private void Start()
        {
            // 设置Sprite朝向相机
            SetupBillboard();
            
            // 初始化AI任务
            SetTask(new Tasks.EnemyAITask(this));
            
            // 注册死亡事件
            combatEntity.EventBus.Register<CombatSystem.Events.DeathEvent>(OnDeath);
        }
        
        private void Update()
        {
            if (Blackboard.IsDead)
                return;
                
            float deltaTime = Time.deltaTime;
            
            // 更新目标检测
            UpdateTargetDetection();
            
            // 更新黑板数据
            UpdateBlackboard();
            
            // 更新当前任务
            currentTask?.Update(deltaTime);
        }
        
        private void FixedUpdate()
        {
            if (Blackboard.IsDead)
                return;
            
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
                }
            }

            if (!Blackboard.CanMove && !isKnockedBack)
            {
                // 不能移动且没有击退时，速度归零
                velocity.x = 0;
                velocity.z = 0;
                return;
            }
            
            moveDirection = Blackboard.MoveDirection;
            
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                // 直接设置目标速度
                Vector3 targetVelocity = moveDirection * moveSpeed;
                velocity.x = targetVelocity.x;
                velocity.z = targetVelocity.z;
            }
            else
            {
                // 停止移动
                velocity.x = 0;
                velocity.z = 0;
            }
            
            // 更新面向方向
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
            // 只需要在地面上时重置垂直速度
            if (isGrounded)
            {
                // 在地面上时，如果垂直速度向下，重置为0（防止穿透地面）
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
            
            // 在地面上时，强制垂直速度为0（防止穿透地面）
            if (isGrounded)
            {
                currentVelocity.y = 0;
            }
            // 否则保持Unity重力系统计算的垂直速度（已经在 currentVelocity.y 中）
            
            rb.linearVelocity = currentVelocity;
        }
        
        /// <summary>
        /// 更新地面状态
        /// </summary>
        private void UpdateGroundedState()
        {
            // 使用 SphereCast 进行更可靠的地面检测
            Vector3 origin = transform.position + Vector3.up * (col.height * 0.5f);
            float checkDistance = groundCheckDistance + col.height * 0.5f;
            
            RaycastHit hit;
            isGrounded = Physics.SphereCast(origin, col.radius * 0.9f, Vector3.down, out hit, checkDistance, groundLayer);
            
            // 如果检测到地面，确保角色不会穿透
            if (isGrounded)
            {
                // 计算角色底部应该在地面上方的位置
                float groundY = hit.point.y;
                float characterBottomY = transform.position.y - col.height * 0.5f;
                float targetY = groundY + col.height * 0.5f;
                
                // 如果角色穿透了地面，将其推回
                if (characterBottomY < groundY)
                {
                    Vector3 pos = transform.position;
                    pos.y = targetY;
                    transform.position = pos;
                    
                    // 强制垂直速度为0
                    Vector3 vel = rb.linearVelocity;
                    vel.y = 0;
                    rb.linearVelocity = vel;
                }
            }
        }
        
        /// <summary>
        /// 更新目标检测
        /// </summary>
        private void UpdateTargetDetection()
        {
            // 如果已有目标，更新距离
            if (Blackboard.Target != null)
            {
                Blackboard.TargetPosition = Blackboard.Target.position;
                Blackboard.DistanceToTarget = Vector3.Distance(transform.position, Blackboard.Target.position);
            }
            else
            {
                // 尝试寻找目标
                GameObject targetObj = GameObject.FindGameObjectWithTag(targetTag);
                if (targetObj != null)
                {
                    float distance = Vector3.Distance(transform.position, targetObj.transform.position);
                    if (distance <= detectionRadius)
                    {
                        Blackboard.Target = targetObj.transform;
                        Blackboard.TargetPosition = targetObj.transform.position;
                        Blackboard.DistanceToTarget = distance;
                    }
                }
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
            direction.y = 0;
            direction.Normalize();
            
            // 先清零当前速度
            rb.linearVelocity = Vector3.zero;
            velocity = Vector3.zero;
            
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
        /// Trigger碰撞检测 - 与玩家碰撞时停止移动
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 检测与玩家的碰撞
            if (other.CompareTag("Player"))
            {
                // 停止移动速度（但不影响击退）
                if (!isKnockedBack)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                }
            }
        }

        /// <summary>
        /// Trigger持续检测 - 防止穿过玩家
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            // 持续检测，防止穿过
            if (other.CompareTag("Player"))
            {
                if (!isKnockedBack)
                {
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                }
            }
        }
        
        /// <summary>
        /// 设置Billboard组件
        /// </summary>
        private void SetupBillboard()
        {
            if (!autoSetupBillboard)
                return;
            
            // 如果没有指定Sprite Transform，尝试查找
            if (spriteTransform == null)
            {
                // 尝试查找子对象中的SpriteRenderer
                SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteTransform = spriteRenderer.transform;
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: 未找到SpriteRenderer组件，无法自动设置Billboard");
                    return;
                }
            }
            
            // 检查是否已经有Billboard组件
            Billboard billboard = spriteTransform.GetComponent<Billboard>();
            if (billboard == null)
            {
                billboard = spriteTransform.gameObject.AddComponent<Billboard>();
                Debug.Log($"{gameObject.name}: 已添加Billboard组件到Sprite对象");
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
            
            // 绘制检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
            
            // 绘制巡逻范围
            if (enablePatrol)
            {
                Vector3 spawnPos = Application.isPlaying ? Blackboard.SpawnPosition : transform.position;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(spawnPos, patrolRadius);
            }
            
            // 绘制移动方向
            if (Application.isPlaying && moveDirection.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position + Vector3.up, moveDirection * 2f);
            }
        }
        
        private void OnDestroy()
        {
            // 取消事件注册
            combatEntity?.EventBus?.Unregister<CombatSystem.Events.DeathEvent>(OnDeath);
        }
    }
}

