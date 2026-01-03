using UnityEngine;

/// <summary>
/// 玩家战斗实体
/// 实现玩家特定的战斗逻辑和行为
/// </summary>
[RequireComponent(typeof(AnimationComponent))]
public class PlayerCombatEntity : CombatEntity
{
    #region 玩家特定配置

    [Header("玩家配置")]
    [SerializeField] private float invincibilityDuration = 0.5f; // 受击无敌时间
    private float _invincibilityTimer = 0f;

    #endregion

    #region 生命周期重写

    protected override void InitializeComponents()
    {
        base.InitializeComponents();

        // 玩家必须有移动和动画组件
        if (MoveComp == null)
        {
            MoveComp = new MoveComponent(this);
        }

        if (AnimComp == null)
        {
            AnimComp = GetComponent<AnimationComponent>();
        }
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 设置玩家特定属性
        EntityType = EntityType.Player;
        Camp = (int)CampType.Friendly;

        Debug.Log($"[PlayerCombatEntity] {EntityName} 初始化完成");
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

        // 更新无敌时间
        if (_invincibilityTimer > 0f)
        {
            _invincibilityTimer -= deltaTime;
        }
    }

    #endregion

    #region 战斗逻辑重写

    protected override void OnDamageReceived(DamageEvent evt)
    {
        base.OnDamageReceived(evt);

        // 设置无敌时间
        _invincibilityTimer = invincibilityDuration;

        // 震动反馈（可选）
        // CameraShake.Instance?.Shake(0.2f, 0.3f);
    }

    protected override bool CheckDodge(CombatEntity target)
    {
        // 无敌时间内自动闪避
        if (_invincibilityTimer > 0f)
        {
            return true;
        }

        return base.CheckDodge(target);
    }

    protected override void OnEntityDeath(DeathEvent evt)
    {
        base.OnEntityDeath(evt);

        Debug.Log($"[PlayerCombatEntity] 玩家 {EntityName} 已死亡");

        // 玩家死亡特殊处理
        // 例如：显示死亡UI、重生倒计时等
        // GameManager.Instance?.OnPlayerDeath(this);
    }

    #endregion

    #region 技能相关重写

    protected override bool CanCastSkill(StateCfg skillCfg)
    {
        if (!base.CanCastSkill(skillCfg))
            return false;

        // 玩家特定的技能施放条件检查
        // 例如：检查法力值、冷却时间等

        return true;
    }

    protected override void OnSkillCast(State skillState, CombatEntity target)
    {
        base.OnSkillCast(skillState, target);

        // 玩家技能施放特效
        Debug.Log($"[PlayerCombatEntity] {EntityName} 施放技能: {skillState.Cfg.name}");
    }

    #endregion

    #region 玩家特定方法

    /// <summary>
    /// 是否处于无敌状态
    /// </summary>
    public bool IsInvincible()
    {
        return _invincibilityTimer > 0f;
    }

    /// <summary>
    /// 获取当前输入方向（由外部输入系统调用）
    /// </summary>
    public Vector2 GetInputDirection()
    {
        // 这里可以对接输入系统
        return Vector2.zero;
    }

    #endregion
}
