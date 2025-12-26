using CombatSystem.Core;
using UnityEngine;

namespace Character3C
{
    /// <summary>
    /// 角色黑板数据
    /// 存储角色运动、状态相关的数据
    /// </summary>
    public class CharacterBlackboard : Blackboard
    {
        // 输入相关
        public Vector2 InputMove { get; set; }
        public bool InputJump { get; set; }
        public bool InputAttack { get; set; }
        public bool InputDash { get; set; }
        public bool InputInteract { get; set; }

        // 移动相关
        public Vector2 Velocity { get; set; }
        public bool IsGrounded { get; set; }
        public bool IsFacingRight { get; set; } = true;
        public float LastGroundedTime { get; set; }

        // 跳跃相关
        public int JumpCount { get; set; }
        public float JumpBufferTime { get; set; }
        public float CoyoteTime { get; set; }

        // 冲刺相关
        public bool IsDashing { get; set; }
        public float DashTimeLeft { get; set; }
        public Vector2 DashDirection { get; set; }
        public float LastDashTime { get; set; }

        // 攻击相关
        public bool IsAttacking { get; set; }
        public int ComboIndex { get; set; }
        public float LastAttackTime { get; set; }

        // 动画相关
        public string CurrentAnimation { get; set; }
        public float AnimationSpeed { get; set; } = 1f;

        // 状态相关
        public bool CanMove { get; set; } = true;
        public bool CanJump { get; set; } = true;
        public bool CanDash { get; set; } = true;
        public bool CanAttack { get; set; } = true;

        // Transform 引用
        public Transform Transform { get; set; }
        public Rigidbody2D Rigidbody { get; set; }
        public Collider2D Collider { get; set; }

        /// <summary>
        /// 重置输入状态（每帧调用）
        /// </summary>
        public void ResetInputFlags()
        {
            InputJump = false;
            InputAttack = false;
            InputDash = false;
            InputInteract = false;
        }
    }
}

