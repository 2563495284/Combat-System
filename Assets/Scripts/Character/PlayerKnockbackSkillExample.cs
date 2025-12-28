using UnityEngine;
using CombatSystem.Core;
using CombatSystem.Configuration;

namespace Character3C
{
    /// <summary>
    /// 玩家击退技能示例
    /// 按下技能键（默认K键）释放击退技能
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class PlayerKnockbackSkillExample : MonoBehaviour
    {
        [Header("技能参数")]
        [SerializeField] private KeyCode skillKey = KeyCode.K;
        [SerializeField] private float skillCooldown = 3f;
        [SerializeField] private float skillRange = 3f;
        [SerializeField] private float knockbackForce = 15f;
        [SerializeField] private float skillDamage = 25f;
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;
        
        private CombatEntity combatEntity;
        private float nextSkillTime = 0f;
        
        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
        }
        
        private void Update()
        {
            // 检测技能输入
            if (Input.GetKeyDown(skillKey))
            {
                TryUseKnockbackSkill();
            }
        }
        
        /// <summary>
        /// 尝试使用击退技能
        /// </summary>
        public void TryUseKnockbackSkill()
        {
            // 检查冷却时间
            if (Time.time < nextSkillTime)
            {
                float remainingTime = nextSkillTime - Time.time;
                if (showDebugInfo)
                {
                    Debug.Log($"[击退技能] 冷却中，剩余 {remainingTime:F1} 秒");
                }
                return;
            }
            
            // 释放技能
            UseKnockbackSkill();
            
            // 设置下次可用时间
            nextSkillTime = Time.time + skillCooldown;
        }
        
        /// <summary>
        /// 使用击退技能 - 方法1：通过战斗系统
        /// </summary>
        private void UseKnockbackSkill()
        {
            if (combatEntity == null)
            {
                Debug.LogError("[击退技能] 未找到 CombatEntity 组件");
                return;
            }
            
            // 获取击退技能配置（ID: 1005）
            var skillCfg = StateCfgManager.Instance.GetConfig(1005);
            if (skillCfg == null)
            {
                Debug.LogError("[击退技能] 未找到技能配置 (ID: 1005)");
                return;
            }
            
            // 创建技能状态
            var skillState = new State(skillCfg);
            skillState.Owner = combatEntity;
            skillState.Caster = combatEntity;
            
            // 设置技能参数
            skillState.Blackboard.Set("CastTime", 0.5f);
            skillState.Blackboard.Set("Damage", skillDamage);
            skillState.Blackboard.Set("KnockbackForce", knockbackForce);
            skillState.Blackboard.Set("Radius", skillRange);
            skillState.Blackboard.Set("Center", transform.position);
            
            // 发布技能
            combatEntity.SkillComp.PublishSkill(skillState);
            
            if (showDebugInfo)
            {
                Debug.Log($"[击退技能] 释放成功！范围: {skillRange}m, 击退力: {knockbackForce}, 伤害: {skillDamage}");
            }
        }
        
        /// <summary>
        /// 使用击退技能 - 方法2：直接检测和击退（不通过战斗系统）
        /// 这是一个简化版本，可以独立使用
        /// </summary>
        public void UseKnockbackSkillDirect()
        {
            Vector3 center = transform.position;
            
            // 查找范围内的所有碰撞体
            Collider[] hits = Physics.OverlapSphere(center, skillRange);
            int hitCount = 0;
            
            foreach (var hit in hits)
            {
                // 检测敌人
                if (hit.CompareTag("Enemy"))
                {
                    // 获取敌人控制器
                    var enemyController = hit.GetComponent<Character3C.Enemy.Enemy25DController>();
                    if (enemyController != null)
                    {
                        // 应用击退效果
                        enemyController.TakeKnockback(center, knockbackForce);
                        hitCount++;
                        
                        // 如果有战斗实体，造成伤害
                        var enemyEntity = hit.GetComponent<CombatEntity>();
                        if (enemyEntity != null && combatEntity != null)
                        {
                            combatEntity.DealDamage(enemyEntity, skillDamage, CombatSystem.DamageType.Physical);
                        }
                    }
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[击退技能] 直接释放，命中 {hitCount} 个敌人");
            }
        }
        
        /// <summary>
        /// 绘制技能范围
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, skillRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, skillRange);
        }
    }
}

