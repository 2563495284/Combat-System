using UnityEngine;
using CombatSystem.Core;
using Character3C;

namespace Character3C.Enemy.Tasks
{
    /// <summary>
    /// 敌人攻击任务
    /// 对目标发起攻击
    /// </summary>
    public class EnemyAttackTask : TaskEntry<EnemyBlackboard>
    {
        private Enemy25DController controller;
        
        private float attackDuration = 1f;
        private float attackTimer = 0f;
        private bool hitExecuted = false;
        
        // 攻击配置
        private Vector3 attackOffset = new Vector3(1f, 0.5f, 0f);
        private Vector3 attackSize = new Vector3(2f, 1.5f, 2f);
        private LayerMask targetLayer;
        
        public EnemyAttackTask(Enemy25DController controller)
        {
            this.controller = controller;
            
            // 默认攻击玩家层
            this.targetLayer = LayerMask.GetMask("Player", "Default");
        }
        
        protected override void OnStart()
        {
            attackTimer = 0f;
            hitExecuted = false;
            
            // 禁用移动
            Blackboard.CanMove = false;
            Blackboard.IsAttacking = true;
            Blackboard.MoveDirection = Vector3.zero;
            
            // 面向目标
            if (Blackboard.Target != null)
            {
                Vector3 direction = Blackboard.TargetPosition - Blackboard.Position;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.01f)
                {
                    Blackboard.FacingDirection = direction.normalized;
                }
            }
            
            Debug.Log($"{controller.name} - 开始攻击");
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            attackTimer += deltaTime;
            
            // 在攻击动画的中间帧执行判定
            float hitTiming = attackDuration * 0.4f;
            
            if (!hitExecuted && attackTimer >= hitTiming)
            {
                ExecuteAttackHit();
                hitExecuted = true;
            }
            
            // 检查攻击是否完成
            if (attackTimer >= attackDuration)
            {
                Complete();
            }
        }
        
        /// <summary>
        /// 执行攻击判定
        /// </summary>
        private void ExecuteAttackHit()
        {
            if (Blackboard.Transform == null)
                return;
            
            // 计算攻击判定框位置
            Vector3 attackPos = Blackboard.Transform.position;
            Vector3 offset = attackOffset;
            
            // 根据朝向调整偏移
            Vector3 forward = Blackboard.FacingDirection;
            if (forward.sqrMagnitude > 0.01f)
            {
                // 将偏移转换到面向方向的局部空间
                Quaternion rotation = Quaternion.LookRotation(forward);
                offset = rotation * new Vector3(attackOffset.x, attackOffset.y, 0);
            }
            
            attackPos += offset;
            
            // 检测碰撞（使用 3D 物理）
            Collider[] hits = Physics.OverlapBox(attackPos, attackSize * 0.5f, Quaternion.identity, targetLayer);
            
            if (hits.Length > 0)
            {
                // 播放击中特效
                PlayHitEffect(attackPos);
                
                // 对每个命中的目标造成伤害
                foreach (var hit in hits)
                {
                    DealDamageToTarget(hit);
                }
                
                Debug.Log($"{controller.name} - 攻击命中 {hits.Length} 个目标");
            }
            
            // 绘制调试信息
            DebugDrawAttackBox(attackPos);
        }
        
        /// <summary>
        /// 对目标造成伤害
        /// </summary>
        private void DealDamageToTarget(Collider target)
        {
            // 获取目标的战斗实体
            var targetEntity = target.GetComponent<CombatEntity>();
            if (targetEntity != null && Blackboard.CombatEntity != null)
            {
                // 使用战斗系统的伤害计算
                Blackboard.CombatEntity.DealDamage(
                    targetEntity, 
                    Blackboard.AttackDamage, 
                    CombatSystem.DamageType.Physical
                );
                
                Debug.Log($"{controller.name} 对 {target.name} 造成 {Blackboard.AttackDamage} 点伤害");
            }
            
            // 应用击退效果
            ApplyKnockback(target.transform);
        }
        
        /// <summary>
        /// 应用击退效果
        /// </summary>
        private void ApplyKnockback(Transform target)
        {
            Vector3 knockbackDir = Blackboard.FacingDirection;
            knockbackDir.y = 0;
            knockbackDir.Normalize();
            float knockbackForce = 3f;

            // 优先使用控制器接口（如果存在）
            var characterController = target.GetComponent<Character25DController>();
            var enemyController = target.GetComponent<Enemy25DController>();
            
            if (characterController != null)
            {
                characterController.ApplyKnockback(knockbackDir, knockbackForce);
            }
            else if (enemyController != null)
            {
                enemyController.ApplyKnockback(knockbackDir, knockbackForce);
            }
            else
            {
                // 备用方案：直接使用 Rigidbody（兼容其他对象）
                if (target.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
                }
            }
        }
        
        /// <summary>
        /// 播放击中特效
        /// </summary>
        private void PlayHitEffect(Vector3 position)
        {
            // 播放击中音效
            // AudioManager.Instance?.PlaySound("EnemyHit");
            
            // 播放击中粒子特效
            // ParticleManager.Instance?.PlayEffect("EnemyHitEffect", position);
        }
        
        /// <summary>
        /// 调试绘制攻击判定框
        /// </summary>
        private void DebugDrawAttackBox(Vector3 center)
        {
#if UNITY_EDITOR
            Vector3 halfSize = attackSize * 0.5f;
            
            // 底部四条边
            Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), Color.red, 0.5f);
            
            // 顶部四条边
            Debug.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), center + new Vector3(halfSize.x, halfSize.y, halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(halfSize.x, halfSize.y, halfSize.z), center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), Color.red, 0.5f);
            
            // 四条竖边
            Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), center + new Vector3(halfSize.x, halfSize.y, halfSize.z), Color.red, 0.5f);
            Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), Color.red, 0.5f);
#endif
        }
        
        protected override void OnComplete()
        {
            // 恢复移动能力
            Blackboard.CanMove = true;
            Blackboard.IsAttacking = false;
            
            // 更新攻击时间
            Blackboard.LastAttackTime = Time.time;
            
            Debug.Log($"{controller.name} - 攻击完成");
        }
        
        protected override void OnStop()
        {
            // 确保恢复状态
            Blackboard.CanMove = true;
            Blackboard.IsAttacking = false;
            
            Debug.Log($"{controller.name} - 攻击状态停止");
        }
    }
}

