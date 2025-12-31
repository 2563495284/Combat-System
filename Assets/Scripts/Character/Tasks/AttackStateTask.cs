using UnityEngine;
using CombatSystem.Core;
using CombatSystem.Attributes;
using BTree;
namespace Character3C.Tasks
{
    /// <summary>
    /// 攻击状态任务 (2.5D)
    /// 处理角色的攻击状态逻辑
    /// </summary>
    public class AttackStateTask : TaskEntry<CharacterBlackboard25D>
    {
        private CombatEntity combatEntity;

        private float attackDuration = 0.5f;
        private float attackTimer = 0f;
        private bool hitExecuted = false;
        private State skillState; // 当前施放的技能状态
        private float lastUpdateTime;

        // 攻击配置
        private Vector3 attackOffset = new Vector3(1f, 0.5f, 0f);
        private Vector3 attackSize = new Vector3(1.5f, 1f, 1.5f);
        private LayerMask enemyLayer;

        public AttackStateTask(CombatEntity combatEntity)
        {
            this.combatEntity = combatEntity;

            // 默认敌人层
            this.enemyLayer = LayerMask.GetMask("Enemy");
        }

        protected override void BeforeEnter()
        {
            attackTimer = 0f;
            hitExecuted = false;
            skillState = null;
            lastUpdateTime = Time.time;
        }

        protected override int Enter()
        {
            // 禁用移动
            Blackboard.CanMove = false;
            Blackboard.IsAttacking = true;

            // 通过 CombatEntity 施放普通攻击技能
            if (combatEntity != null)
            {
                var skillCfg = CombatSystem.Configuration.StateCfgManager.Instance.GetConfig(1001); // 普通攻击技能ID
                if (skillCfg != null)
                {
                    // 查找攻击范围内的敌人作为目标
                    CombatEntity target = FindNearestEnemyInRange();

                    skillState = combatEntity.CastSkill(skillCfg, target);
                    if (skillState != null)
                    {
                        // 设置技能参数
                        float baseDamage = combatEntity.AttrComp.GetAttr(AttrType.Attack);
                        float comboMultiplier = 1f + Blackboard.ComboIndex * 0.2f; // 连击加成
                        skillState.Blackboard.Set("Damage", baseDamage * comboMultiplier);
                        skillState.Blackboard.Set("CastTime", skillCfg.duration / 1000f);

                        // 使用技能配置的持续时间
                        attackDuration = skillCfg.duration / 1000f;
                    }
                }
            }

            // 播放攻击音效
            // AudioManager.Instance?.PlaySound("Attack");

            Debug.Log($"开始攻击 - 连击: {Blackboard.ComboIndex}");
            return TaskStatus.RUNNING;
        }

        protected override int Execute()
        {
            float deltaTime = Time.time - lastUpdateTime;
            lastUpdateTime = Time.time;

            attackTimer += deltaTime;

            // 如果使用 CombatEntity 的技能系统，伤害由技能任务处理
            // 这里只处理范围攻击判定（如果有多个目标）
            if (skillState == null)
            {
                // 如果没有技能状态，使用传统的攻击判定
                float hitTiming = attackDuration * 0.4f; // 40% 处执行判定

                if (!hitExecuted && attackTimer >= hitTiming)
                {
                    ExecuteAttackHit();
                    hitExecuted = true;
                }
            }
            else
            {
                // 使用技能系统时，检查技能是否完成
                if (skillState != null && combatEntity.SkillComp != null)
                {
                    // 技能完成后，攻击任务也完成
                    if (combatEntity.SkillComp.GetCastingSkill(skillState.Cfg.cid) == null || combatEntity.SkillComp.GetCastingSkill(skillState.Cfg.cid) != skillState)
                    {
                        return TaskStatus.SUCCESS;
                    }
                }
            }

            // 检查攻击是否完成
            if (attackTimer >= attackDuration)
            {
                return TaskStatus.SUCCESS;
            }

            // 检查连击输入
            if (Blackboard.InputAttack && attackTimer > attackDuration * 0.6f)
            {
                // 允许在攻击后期输入下一段连击
                Blackboard.Set("ComboBuffered", true);
            }

            return TaskStatus.RUNNING;
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

            // 根据朝向调整偏移（使用面向方向）
            if (Blackboard.FacingDirection.x < 0)
            {
                offset.x *= -1;
            }

            attackPos += offset;

            // 检测碰撞（使用 3D 物理）
            Collider[] hits = Physics.OverlapBox(attackPos, attackSize * 0.5f, Quaternion.identity, enemyLayer);

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
        /// 查找攻击范围内的最近敌人
        /// </summary>
        private CombatEntity FindNearestEnemyInRange()
        {
            if (combatEntity == null || Blackboard.Transform == null)
                return null;

            float attackRange = 2f; // 攻击范围
            var colliders = Physics.OverlapSphere(Blackboard.Transform.position, attackRange);
            CombatEntity nearest = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                var target = col.GetComponent<CombatEntity>();
                if (target != null && target != combatEntity && target.IsAlive())
                {
                    // 检查阵营（不同阵营才能攻击）
                    if (target.Camp != combatEntity.Camp)
                    {
                        float dist = Vector3.Distance(combatEntity.transform.position, target.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearest = target;
                        }
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// 对敌人造成伤害（保留用于范围攻击判定）
        /// </summary>
        private void DealDamageToEnemy(Collider enemy)
        {
            var enemyEntity = enemy.GetComponent<CombatEntity>();
            if (enemyEntity != null && combatEntity != null)
            {
                // 使用 CombatEntity 的 DealDamage 方法
                float baseDamage = combatEntity.AttrComp.GetAttr(CombatSystem.Attributes.AttrType.Attack);
                float comboMultiplier = 1f + Blackboard.ComboIndex * 0.2f;
                float damage = baseDamage * comboMultiplier;

                combatEntity.DealDamage(enemyEntity, damage, CombatSystem.DamageType.Physical);
            }

            // 应用击退效果
            ApplyKnockback(enemy.transform);
        }

        /// <summary>
        /// 应用击退效果
        /// </summary>
        private void ApplyKnockback(Transform target)
        {
            Vector3 knockbackDir = Blackboard.FacingDirection;
            knockbackDir.y = 0;
            knockbackDir.Normalize();
            float knockbackForce = 5f;

            // 优先使用控制器接口（如果存在）
            var characterController = target.GetComponent<Character25DController>();
            if (characterController != null)
            {
                characterController.ApplyKnockback(knockbackDir, knockbackForce);
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
            // AudioManager.Instance?.PlaySound("Hit");

            // 播放击中粒子特效
            // ParticleManager.Instance?.PlayEffect("HitEffect", position);
        }

        /// <summary>
        /// 调试绘制攻击判定框
        /// </summary>
        private void DebugDrawAttackBox(Vector3 center)
        {
#if UNITY_EDITOR
            // 绘制3D立方体的边框
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

        protected override void Exit()
        {
            // 恢复移动能力
            Blackboard.CanMove = true;
            Blackboard.IsAttacking = false;

            // 如果是正常完成（不是被打断），更新连击索引
            if (IsSucceeded)
            {
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
            }

            Debug.Log("攻击状态结束");
        }

        /// <summary>
        /// 设置攻击配置
        /// </summary>
        public void SetAttackConfig(Vector3 offset, Vector3 size, LayerMask layer)
        {
            this.attackOffset = offset;
            this.attackSize = size;
            this.enemyLayer = layer;
        }
    }
}

