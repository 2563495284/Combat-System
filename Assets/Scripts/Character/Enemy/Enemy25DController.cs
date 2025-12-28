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
            
            // 配置Rigidbody - 设置为Kinematic使敌人不会被推动
            rb.isKinematic = true; // Kinematic模式：不受物理力影响，但可以阻挡其他物体
            rb.useGravity = false; // 使用自定义重力
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
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
            
            // 更新地面检测
            UpdateGroundedState();
            
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
                
            ApplyMovement();
            ApplyGravity();
        }
        
        /// <summary>
        /// 应用移动 (Kinematic模式)
        /// </summary>
        private void ApplyMovement()
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
            
            // 合并移动速度和击退速度
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            if (isKnockedBack)
            {
                // 击退时，移动速度会被击退速度影响
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, knockbackVelocity, 0.7f);
            }
            
            // 使用MovePosition来移动Kinematic Rigidbody (带碰撞检测)
            Vector3 newPosition = rb.position + horizontalVelocity * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
            
            // 更新面向方向
            if (moveDirection.sqrMagnitude > 0.01f && !isKnockedBack)
            {
                Blackboard.FacingDirection = moveDirection.normalized;
            }
        }
        
        /// <summary>
        /// 应用重力 (Kinematic模式)
        /// </summary>
        private void ApplyGravity()
        {
            if (!isGrounded)
            {
                // 应用重力加速度
                velocity.y -= gravity * Time.fixedDeltaTime;
                
                // 移动垂直位置
                Vector3 newPosition = rb.position + new Vector3(0, velocity.y * Time.fixedDeltaTime, 0);
                rb.MovePosition(newPosition);
            }
            else
            {
                // 在地面上时，重置垂直速度
                velocity.y = 0;
            }
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
            // 对于Kinematic Rigidbody，使用我们手动维护的velocity
            Blackboard.Velocity = velocity;
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
            
            // 计算击退速度
            knockbackVelocity = direction * force;
            isKnockedBack = true;
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
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(origin, Vector3.down * (groundCheckDistance + 0.1f));
            
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

