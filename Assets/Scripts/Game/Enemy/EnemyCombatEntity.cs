using UnityEngine;
using CombatSystem.Core;
using CombatSystem.Components;
using CombatSystem.Events;

namespace Game.Enemy
{
    /// <summary>
    /// 敌人战斗实体
    /// 具体实现敌人相关的战斗逻辑和组件管理
    /// </summary>
    public class EnemyCombatEntity : CombatEntity
    {
        [Header("敌人特定配置")]
        [SerializeField] private bool hasAnimation = true;
        [SerializeField] private bool canMove = true;

        [Header("掉落配置")]
        [SerializeField] private int expReward = 10;
        [SerializeField] private GameObject[] dropItems;

        protected override void InitializeComponents()
        {
            // 根据配置初始化组件
            if (canMove)
            {
                MoveComp = new MoveComponent(this);
            }

            if (hasAnimation)
            {
                AnimComp = GetComponent<AnimationComponent>();
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // 敌人特定初始化
            Debug.Log($"[EnemyCombatEntity] {EntityName} 初始化完成");
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            // 敌人AI更新逻辑可以放在这里
            // 或者交给专门的AI组件处理
        }

        protected override void OnEntityDeath(DeathEvent evt)
        {
            base.OnEntityDeath(evt);

            // 敌人死亡特殊处理
            Debug.Log($"[EnemyCombatEntity] {EntityName} 被 {evt.killer?.EntityName} 击败");

            // 给予奖励
            GiveRewards(evt.killer);

            // 延迟销毁
            Destroy(gameObject, 3f);
        }

        /// <summary>
        /// 给予奖励
        /// </summary>
        private void GiveRewards(CombatEntity killer)
        {
            if (killer == null) return;

            // 给予经验值
            Debug.Log($"[EnemyCombatEntity] 奖励 {expReward} 经验值");

            // 掉落物品
            DropItems();
        }

        /// <summary>
        /// 掉落物品
        /// </summary>
        private void DropItems()
        {
            if (dropItems == null || dropItems.Length == 0)
                return;

            foreach (var item in dropItems)
            {
                if (item != null && Random.value < 0.5f) // 50%掉落率
                {
                    Instantiate(item, transform.position, Quaternion.identity);
                }
            }
        }
    }
}

