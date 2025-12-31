using UnityEngine;
using CombatSystem.Core;
using CombatSystem.Components;
using CombatSystem.Events;

namespace Game.Player
{
    /// <summary>
    /// 玩家战斗实体
    /// 具体实现玩家相关的战斗逻辑和组件管理
    /// </summary>
    [RequireComponent(typeof(AnimationComponent))]
    [RequireComponent(typeof(CharacterAnimator))]
    public class PlayerCombatEntity : CombatEntity
    {
        [Header("玩家特定组件")]
        [SerializeField] private CharacterAnimator characterAnimator;

        /// <summary>
        /// 角色动画管理器
        /// </summary>
        public CharacterAnimator CharacterAnimator => characterAnimator;

        protected override void InitializeComponents()
        {
            // 初始化必需组件
            MoveComp = new MoveComponent(this);
            AnimComp = GetComponent<AnimationComponent>();
            
            // 获取玩家特定组件
            if (characterAnimator == null)
            {
                characterAnimator = GetComponent<CharacterAnimator>();
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // 玩家特定初始化
            Debug.Log($"[PlayerCombatEntity] {EntityName} 初始化完成");
        }

        protected override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            // 玩家特定更新逻辑（如果需要）
        }

        protected override void OnEntityDeath(DeathEvent evt)
        {
            base.OnEntityDeath(evt);

            // 玩家死亡特殊处理
            Debug.Log($"[PlayerCombatEntity] {EntityName} 死亡");

            // 触发游戏结束或重生逻辑
            // GameManager.Instance?.OnPlayerDeath();
        }
    }
}

