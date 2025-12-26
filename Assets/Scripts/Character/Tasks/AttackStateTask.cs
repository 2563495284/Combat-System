using UnityEngine;
using CombatSystem.Core;

namespace Character3C.Tasks
{
    /// <summary>
    /// 攻击状态任务
    /// 处理角色的攻击状态逻辑
    /// </summary>
    public class AttackStateTask : TaskEntry<CharacterBlackboard>
    {
        private CharacterController2D character;
        private CharacterAnimator animator;

        private float attackDuration = 0.5f;
        private float attackTimer = 0f;
        private bool hitExecuted = false;

        // 攻击配置
        private Vector2 attackOffset = new Vector2(1f, 0f);
        private Vector2 attackSize = new Vector2(1.5f, 1f);
        private LayerMask enemyLayer;

        public AttackStateTask(CharacterController2D character)
        {
            this.character = character;
            this.animator = character.GetComponent<CharacterAnimator>();

            // 默认敌人层
            this.enemyLayer = LayerMask.GetMask("Enemy");
        }

        protected override void OnStart()
        {
            attackTimer = 0f;
            hitExecuted = false;

            // 禁用移动
            Blackboard.CanMove = false;
            Blackboard.IsAttacking = true;

            // 播放攻击音效
            // AudioManager.Instance?.PlaySound("Attack");

            Debug.Log($"开始攻击 - 连击: {Blackboard.ComboIndex}");
        }

        protected override void OnUpdate(float deltaTime)
        {
            attackTimer += deltaTime;

            // 在攻击动画的中间帧执行判定
            float hitTiming = attackDuration * 0.4f; // 40% 处执行判定

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

            // 检查连击输入
            if (Blackboard.InputAttack && attackTimer > attackDuration * 0.6f)
            {
                // 允许在攻击后期输入下一段连击
                Blackboard.Set("ComboBuffered", true);
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
            Vector2 attackPos = (Vector2)Blackboard.Transform.position;
            Vector2 offset = attackOffset;

            // 根据朝向调整偏移
            if (!Blackboard.IsFacingRight)
            {
                offset.x *= -1;
            }

            attackPos += offset;

            // 检测碰撞
            Collider2D[] hits = Physics2D.OverlapBoxAll(attackPos, attackSize, 0f, enemyLayer);

            if (hits.Length > 0)
            {
                // 播放击中特效
                PlayHitEffect(attackPos);

                // 触发相机震动
                Blackboard.Set("TriggerCameraShake", true);
                Blackboard.Set("ShakeIntensity", 0.15f);
                Blackboard.Set("ShakeDuration", 0.1f);

                // 对每个命中的敌人造成伤害
                foreach (var hit in hits)
                {
                    DealDamageToEnemy(hit);
                }

                Debug.Log($"攻击命中 {hits.Length} 个目标");
            }

            // 绘制调试信息
            DebugDrawAttackBox(attackPos);
        }

        /// <summary>
        /// 对敌人造成伤害
        /// </summary>
        private void DealDamageToEnemy(Collider2D enemy)
        {
            // 计算伤害
            float baseDamage = 10f;
            float damage = baseDamage * (1f + Blackboard.ComboIndex * 0.2f); // 连击加成

            // 这里可以调用敌人的受伤接口
            // var enemyEntity = enemy.GetComponent<CombatEntity>();
            // enemyEntity?.TakeDamage(damage);

            // 应用击退效果
            ApplyKnockback(enemy.transform);

            Debug.Log($"对 {enemy.name} 造成 {damage} 点伤害");
        }

        /// <summary>
        /// 应用击退效果
        /// </summary>
        private void ApplyKnockback(Transform target)
        {
            if (target.TryGetComponent<Rigidbody2D>(out var rb))
            {
                Vector2 knockbackDir = Blackboard.IsFacingRight ? Vector2.right : Vector2.left;
                float knockbackForce = 5f;

                rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
            }
        }

        /// <summary>
        /// 播放击中特效
        /// </summary>
        private void PlayHitEffect(Vector2 position)
        {
            // 播放击中音效
            // AudioManager.Instance?.PlaySound("Hit");

            // 播放击中粒子特效
            // ParticleManager.Instance?.PlayEffect("HitEffect", position);
        }

        /// <summary>
        /// 调试绘制攻击判定框
        /// </summary>
        private void DebugDrawAttackBox(Vector2 center)
        {
#if UNITY_EDITOR
            Debug.DrawLine(
                center + new Vector2(-attackSize.x * 0.5f, -attackSize.y * 0.5f),
                center + new Vector2(attackSize.x * 0.5f, -attackSize.y * 0.5f),
                Color.red, 0.5f
            );
            Debug.DrawLine(
                center + new Vector2(attackSize.x * 0.5f, -attackSize.y * 0.5f),
                center + new Vector2(attackSize.x * 0.5f, attackSize.y * 0.5f),
                Color.red, 0.5f
            );
            Debug.DrawLine(
                center + new Vector2(attackSize.x * 0.5f, attackSize.y * 0.5f),
                center + new Vector2(-attackSize.x * 0.5f, attackSize.y * 0.5f),
                Color.red, 0.5f
            );
            Debug.DrawLine(
                center + new Vector2(-attackSize.x * 0.5f, attackSize.y * 0.5f),
                center + new Vector2(-attackSize.x * 0.5f, -attackSize.y * 0.5f),
                Color.red, 0.5f
            );
#endif
        }

        protected override void OnComplete()
        {
            // 恢复移动能力
            Blackboard.CanMove = true;
            Blackboard.IsAttacking = false;

            // 更新连击索引
            Blackboard.LastAttackTime = Time.time;

            // 检查是否缓存了连击输入
            if (Blackboard.Get<bool>("ComboBuffered", false))
            {
                Blackboard.ComboIndex = (Blackboard.ComboIndex + 1) % 3;
                Blackboard.Set("ComboBuffered", false);
            }
            else
            {
                // 没有连击，延迟后重置
                Blackboard.Set("ResetComboTime", Time.time + 0.5f);
            }

            Debug.Log("攻击完成");
        }

        protected override void OnStop()
        {
            // 确保恢复状态
            Blackboard.CanMove = true;
            Blackboard.IsAttacking = false;

            Debug.Log("攻击状态停止");
        }

        /// <summary>
        /// 设置攻击配置
        /// </summary>
        public void SetAttackConfig(Vector2 offset, Vector2 size, LayerMask layer)
        {
            this.attackOffset = offset;
            this.attackSize = size;
            this.enemyLayer = layer;
        }
    }
}

