using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Enemy
{
    /// <summary>
    /// 敌人黑板数据
    /// 存储敌人AI状态和行为数据
    /// </summary>
    public class EnemyBlackboard : Blackboard
    {
        // AI状态
        public EnemyAIState CurrentState { get; set; } = EnemyAIState.Idle;
        public EnemyAIState PreviousState { get; set; } = EnemyAIState.Idle;

        // 目标相关
        public Transform Target { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float DistanceToTarget { get; set; }
        
        // 检测相关
        public float DetectionRadius { get; set; } = 10f;
        public float AttackRadius { get; set; } = 2f;
        public float LoseTargetRadius { get; set; } = 15f;
        
        // 3D 移动相关
        public Vector3 Velocity { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 MoveDirection { get; set; }
        public Vector3 FacingDirection { get; set; } = Vector3.forward;
        public bool IsGrounded { get; set; }
        
        // 攻击相关
        public bool IsAttacking { get; set; }
        public float LastAttackTime { get; set; }
        public float AttackCooldown { get; set; } = 2f;
        public float AttackDamage { get; set; } = 15f;
        
        // 巡逻相关
        public Vector3 SpawnPosition { get; set; }
        public Vector3 PatrolTarget { get; set; }
        public float PatrolRadius { get; set; } = 5f;
        public float PatrolWaitTime { get; set; } = 2f;
        public float PatrolTimer { get; set; }
        
        // 状态相关
        public bool CanMove { get; set; } = true;
        public bool CanAttack { get; set; } = true;
        public bool IsDead { get; set; }
        
        // Transform 引用
        public Transform Transform { get; set; }
        public Rigidbody Rigidbody { get; set; }
        public Collider Collider { get; set; }
        
        // CombatEntity 引用
        public CombatEntity CombatEntity { get; set; }
        
        /// <summary>
        /// 切换AI状态
        /// </summary>
        public void ChangeState(EnemyAIState newState)
        {
            if (CurrentState != newState)
            {
                PreviousState = CurrentState;
                CurrentState = newState;
            }
        }
        
        /// <summary>
        /// 检查是否可以攻击
        /// </summary>
        public bool CanAttackTarget()
        {
            if (!CanAttack || IsAttacking || IsDead)
                return false;
                
            if (Time.time - LastAttackTime < AttackCooldown)
                return false;
                
            return Target != null && DistanceToTarget <= AttackRadius;
        }
        
        /// <summary>
        /// 检查目标是否在检测范围内
        /// </summary>
        public bool IsTargetInDetectionRange()
        {
            return Target != null && DistanceToTarget <= DetectionRadius;
        }
        
        /// <summary>
        /// 检查目标是否丢失
        /// </summary>
        public bool IsTargetLost()
        {
            return Target == null || DistanceToTarget > LoseTargetRadius;
        }
    }
    
    /// <summary>
    /// 敌人AI状态枚举
    /// </summary>
    public enum EnemyAIState
    {
        Idle,       // 空闲
        Patrol,     // 巡逻
        Chase,      // 追击
        Attack,     // 攻击
        Dead        // 死亡
    }
}

