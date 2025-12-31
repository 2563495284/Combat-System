using CombatSystem.Attributes;
using CombatSystem.Core;
using UnityEngine;

namespace CombatSystem.Components
{
    /// <summary>
    /// 移动组件
    /// 处理实体的移动逻辑
    /// </summary>
    public class MoveComponent
    {
        /// <summary>
        /// 组件拥有者
        /// </summary>
        public CombatEntity Owner { get; private set; }

        /// <summary>
        /// 当前移动方向
        /// </summary>
        public Vector3 MoveDirection { get; set; }

        /// <summary>
        /// 目标位置（用于导航）
        /// </summary>
        public Vector3? TargetPosition { get; set; }

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving => MoveDirection.sqrMagnitude > 0.01f || TargetPosition.HasValue;

        /// <summary>
        /// 移动模式
        /// </summary>
        public MoveMode Mode { get; set; }

        /// <summary>
        /// 是否可以移动
        /// </summary>
        public bool CanMove { get; private set; } = true;

        /// <summary>
        /// 击退速度
        /// </summary>
        private Vector3 _knockbackVelocity;

        /// <summary>
        /// 击退衰减率
        /// </summary>
        private float _knockbackDecay = 10f;

        public MoveComponent(CombatEntity owner)
        {
            Owner = owner;
            Mode = MoveMode.Direction;
        }

        /// <summary>
        /// 更新移动
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!Owner.IsAlive())
                return;

            // 更新击退
            UpdateKnockback(deltaTime);

            // 如果不能移动,直接返回
            if (!CanMove)
                return;

            switch (Mode)
            {
                case MoveMode.Direction:
                    UpdateDirectionMove(deltaTime);
                    break;

                case MoveMode.Navigation:
                    UpdateNavigationMove(deltaTime);
                    break;
            }
        }

        /// <summary>
        /// 方向移动更新
        /// </summary>
        private void UpdateDirectionMove(float deltaTime)
        {
            if (MoveDirection.sqrMagnitude < 0.01f)
                return;

            float speed = Owner.AttrComp.GetAttr(AttrType.MoveSpeed);
            Vector3 movement = MoveDirection.normalized * speed * deltaTime;
            Owner.transform.position += movement;

            // 更新朝向
            if (movement.sqrMagnitude > 0.01f)
            {
                Owner.transform.forward = movement.normalized;
            }
        }

        /// <summary>
        /// 导航移动更新
        /// </summary>
        private void UpdateNavigationMove(float deltaTime)
        {
            if (!TargetPosition.HasValue)
                return;

            Vector3 direction = TargetPosition.Value - Owner.transform.position;
            float distance = direction.magnitude;

            // 到达目标
            if (distance < 0.1f)
            {
                TargetPosition = null;
                return;
            }

            // 移动
            float speed = Owner.AttrComp.GetAttr(AttrType.MoveSpeed);
            float moveDistance = speed * deltaTime;

            if (moveDistance >= distance)
            {
                Owner.transform.position = TargetPosition.Value;
                TargetPosition = null;
            }
            else
            {
                Owner.transform.position += direction.normalized * moveDistance;
                Owner.transform.forward = direction.normalized;
            }
        }

        /// <summary>
        /// 设置移动方向
        /// </summary>
        public void SetDirection(Vector3 direction)
        {
            Mode = MoveMode.Direction;
            MoveDirection = direction;
            TargetPosition = null;
        }

        /// <summary>
        /// 移动到目标位置
        /// </summary>
        public void MoveTo(Vector3 target)
        {
            Mode = MoveMode.Navigation;
            TargetPosition = target;
            MoveDirection = Vector3.zero;
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            MoveDirection = Vector3.zero;
            TargetPosition = null;
        }

        /// <summary>
        /// 朝向目标
        /// </summary>
        public void LookAt(Vector3 target)
        {
            Vector3 direction = target - Owner.transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                Owner.transform.forward = direction.normalized;
            }
        }

        /// <summary>
        /// 设置是否可以移动
        /// </summary>
        public void SetCanMove(bool canMove)
        {
            CanMove = canMove;
            if (!canMove)
            {
                Stop();
            }
        }

        /// <summary>
        /// 应用击退
        /// </summary>
        public void ApplyKnockback(Vector3 velocity)
        {
            _knockbackVelocity = velocity;
        }

        /// <summary>
        /// 更新击退
        /// </summary>
        private void UpdateKnockback(float deltaTime)
        {
            if (_knockbackVelocity.sqrMagnitude > 0.01f)
            {
                // 应用击退位移
                Owner.transform.position += _knockbackVelocity * deltaTime;

                // 衰减击退速度
                _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, _knockbackDecay * deltaTime);

                // 如果速度很小,直接停止
                if (_knockbackVelocity.sqrMagnitude < 0.01f)
                {
                    _knockbackVelocity = Vector3.zero;
                }
            }
        }
    }

    /// <summary>
    /// 移动模式
    /// </summary>
    public enum MoveMode
    {
        Direction,      // 方向移动
        Navigation,     // 导航移动（移动到目标点）
    }
}

