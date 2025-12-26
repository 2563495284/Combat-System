using UnityEngine;
using CombatSystem.Core;

namespace Character3C
{
    /// <summary>
    /// 2.5D 角色黑板数据
    /// 存储3D空间中的角色状态
    /// </summary>
    public class CharacterBlackboard25D : Blackboard
    {
        // 输入相关
        public Vector2 InputMove { get; set; }
        public bool InputJump { get; set; }
        public bool InputAttack { get; set; }
        public bool InputDash { get; set; }
        public bool InputInteract { get; set; }

        // 3D 移动相关
        public Vector3 Velocity { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 MoveDirection { get; set; }
        public Vector3 FacingDirection { get; set; } = Vector3.forward;
        public bool IsGrounded { get; set; }

        // 跳跃相关
        public int JumpCount { get; set; }
        public float LastJumpTime { get; set; }

        // 冲刺相关
        public bool IsDashing { get; set; }
        public float DashTimeLeft { get; set; }
        public Vector3 DashDirection { get; set; }
        public float LastDashTime { get; set; }

        // 攻击相关
        public bool IsAttacking { get; set; }
        public int ComboIndex { get; set; }
        public float LastAttackTime { get; set; }

        // 状态相关
        public bool CanMove { get; set; } = true;
        public bool CanJump { get; set; } = true;
        public bool CanDash { get; set; } = true;
        public bool CanAttack { get; set; } = true;

        // 地图相关
        public Vector2Int GridPosition { get; set; } // 在地图网格中的位置
        public int CurrentMapLayer { get; set; } = 0; // 当前所在地图层

        // Transform 引用
        public Transform Transform { get; set; }
        public Rigidbody Rigidbody { get; set; }
        public Collider Collider { get; set; }

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

