using UnityEngine;


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

    // 3D 移动相关
    public Vector3 Velocity { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 MoveDirection { get; set; }
    public Vector3 FacingDirection { get; set; } = Vector3.forward;
    public bool IsGrounded { get; set; }

    // 攻击相关
    public bool IsAttacking { get; set; }
    public float LastAttackTime { get; set; }

    // 状态相关
    public bool CanMove { get; set; } = true;
    public bool CanAttack { get; set; } = true;
    public bool IsDead { get; set; }

    // Transform 引用
    public Transform Transform { get; set; }
    public Rigidbody2D Rigidbody { get; set; }
    public BoxCollider2D Collider { get; set; }

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


