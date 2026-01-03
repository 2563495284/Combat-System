using UnityEngine;

/// <summary>
/// 通用战斗实体基类
/// 实现所有实体共享的战斗逻辑
/// </summary>
public partial class CombatEntity : MonoBehaviour
{
    #region 核心组件（必需）

    /// <summary>
    /// 事件总线
    /// </summary>
    public CombatEventBus EventBus { get; protected set; }

    /// <summary>
    /// 状态组件 - 管理技能和Buff
    /// </summary>
    public StateComponent StateComp { get; protected set; }

    /// <summary>
    /// 属性组件 - 管理生命值、攻击力等属性
    /// </summary>
    public AttrComponent AttrComp { get; protected set; }

    /// <summary>
    /// 技能组件 - 管理当前施放的技能
    /// </summary>
    public SkillComponent SkillComp { get; protected set; }

    #endregion

    /// <summary>
    /// 移动组件
    /// </summary>
    public MoveComponent MoveComp { get; protected set; }

    /// <summary>
    /// 动画组件（可选）- 管理动画播放和动画事件
    /// </summary>
    public AnimationComponent AnimComp { get; protected set; }


    #region 生命周期

    private void Awake()
    {
        InitializeCore();
        InitializeComponents();
        OnInitialize();
    }

    /// <summary>
    /// 初始化核心组件（必需）
    /// </summary>
    private void InitializeCore()
    {
        EventBus = new CombatEventBus();
        StateComp = new StateComponent(this);
        AttrComp = new AttrComponent(this);
        SkillComp = new SkillComponent(this);

        // 注册核心事件监听
        RegisterEventListeners();
    }

    /// <summary>
    /// 初始化可选组件（由子类决定）
    /// </summary>
    protected virtual void InitializeComponents()
    {
        // 默认实现：尝试获取常用组件
        // MoveComp 由子类根据需要创建（因为目前只支持 PlayerCombatEntity）
        AnimComp = GetComponent<AnimationComponent>();
    }

    /// <summary>
    /// 子类自定义初始化（在所有组件初始化之后）
    /// </summary>
    protected virtual void OnInitialize()
    {
        // 留给子类实现
    }

    #endregion

    /// <summary>
    /// 注册事件监听
    /// </summary>
    private void RegisterEventListeners()
    {
        EventBus.Register<DamageEvent>(OnDamage);
        EventBus.Register<HealEvent>(OnHeal);
        EventBus.Register<DeathEvent>(OnDeath);
    }

    private int _currentFrame = 0;

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        _currentFrame++;

        // 更新核心组件
        UpdateCore(deltaTime);

        // 子类自定义更新
        OnUpdate(deltaTime);
    }

    /// <summary>
    /// 更新核心组件
    /// </summary>
    protected virtual void UpdateCore(float deltaTime)
    {
        // 更新状态组件
        StateComp?.Update(_currentFrame);

        // 更新移动组件（如果有）
        MoveComp?.Update(deltaTime);
    }

    /// <summary>
    /// 子类自定义更新
    /// </summary>
    protected virtual void OnUpdate(float deltaTime)
    {
        // 留给子类实现
    }

    #region 战斗相关方法

    /// <summary>
    /// 造成伤害
    /// </summary>
    public void DealDamage(CombatEntity target, float damage, DamageType damageType = DamageType.Physical)
    {
        if (target == null || !IsTargetAlive(target))
            return;

        // 计算实际伤害
        float actualDamage = CalculateDamage(damage, damageType, target);

        // 检查闪避
        if (CheckDodge(target))
        {
            OnDodge(target);
            return;
        }

        // 检查暴击
        bool isCrit = CheckCrit();
        if (isCrit)
        {
            actualDamage *= AttrComp.GetAttr(AttrType.CritDamage) / 100f;
        }

        // 触发伤害事件
        var damageEvent = new DamageEvent
        {
            attacker = this,
            target = target,
            damage = actualDamage,
            damageType = damageType,
            isCrit = isCrit
        };

        // 向目标发送伤害事件
        var targetEntity = target as CombatEntity;
        targetEntity?.EventBus.Fire(damageEvent);
    }

    /// <summary>
    /// 检查闪避
    /// </summary>
    protected virtual bool CheckDodge(CombatEntity target)
    {
        var targetEntity = target as CombatEntity;
        if (targetEntity == null) return false;

        float hitRate = AttrComp.GetAttr(AttrType.HitRate);
        float dodgeRate = targetEntity.AttrComp.GetAttr(AttrType.DodgeRate);

        return Random.value * 100f > (hitRate - dodgeRate);
    }

    /// <summary>
    /// 闪避回调
    /// </summary>
    protected virtual void OnDodge(CombatEntity target)
    {
        Debug.Log($"{target.EntityName} 闪避了 {EntityName} 的攻击");
    }

    /// <summary>
    /// 检查暴击
    /// </summary>
    protected virtual bool CheckCrit()
    {
        return Random.value < AttrComp.GetAttr(AttrType.CritRate) / 100f;
    }

    /// <summary>
    /// 计算伤害
    /// </summary>
    protected virtual float CalculateDamage(float baseDamage, DamageType damageType, CombatEntity target)
    {
        var targetEntity = target as CombatEntity;
        if (targetEntity == null) return baseDamage;

        float damage = baseDamage;

        switch (damageType)
        {
            case DamageType.Physical:
                float attack = AttrComp.GetAttr(AttrType.Attack);
                float defense = targetEntity.AttrComp.GetAttr(AttrType.Defense);
                float physicalRes = targetEntity.AttrComp.GetAttr(AttrType.PhysicalRes);
                damage = Mathf.Max(1, (attack + baseDamage - defense * 0.5f) * (1 - physicalRes / 100f));
                break;

            case DamageType.Magic:
                float magicAttack = AttrComp.GetAttr(AttrType.MagicAttack);
                float magicDefense = targetEntity.AttrComp.GetAttr(AttrType.MagicDefense);
                float magicRes = targetEntity.AttrComp.GetAttr(AttrType.MagicRes);
                damage = Mathf.Max(1, (magicAttack + baseDamage - magicDefense * 0.5f) * (1 - magicRes / 100f));
                break;

            case DamageType.True:
                damage = baseDamage;
                break;
        }

        return damage;
    }

    /// <summary>
    /// 受到伤害处理
    /// </summary>
    private void OnDamage(DamageEvent evt)
    {
        if (evt.target != this)
            return;

        // 扣除生命值
        AttrComp.ModifyCurrentValue(AttrType.Hp, -evt.damage);

        Debug.Log($"{EntityName} 受到 {evt.damage} 点{(evt.isCrit ? "暴击" : "")}伤害，剩余HP: {AttrComp.GetAttr(AttrType.Hp)}");

        // 子类自定义伤害处理
        OnDamageReceived(evt);

        // 非致死受击：自动施加受击硬直（主状态槽，由状态系统处理互斥/优先级）
        if (IsAlive() && evt.damage > 0)
        {
            ApplyMainState(9004);
        }

        // 检查死亡
        if (!IsAlive())
        {
            Die(evt.attacker);
        }
    }

    /// <summary>
    /// 受到伤害时的自定义处理（由子类实现）
    /// </summary>
    protected virtual void OnDamageReceived(DamageEvent evt)
    {
        // 留给子类实现（如播放受击动画）
    }

    /// <summary>
    /// 治疗
    /// </summary>
    public void Heal(CombatEntity target, float healAmount)
    {
        if (target == null || !IsTargetAlive(target))
            return;

        var healEvent = new HealEvent
        {
            healer = this,
            target = target,
            healAmount = healAmount
        };

        var targetEntity = target as CombatEntity;
        targetEntity?.EventBus.Fire(healEvent);
    }

    /// <summary>
    /// 受到治疗处理
    /// </summary>
    private void OnHeal(HealEvent evt)
    {
        if (evt.target != this)
            return;

        AttrComp.ModifyCurrentValue(AttrType.Hp, evt.healAmount);
        Debug.Log($"{EntityName} 恢复 {evt.healAmount} 点生命，当前HP: {AttrComp.GetAttr(AttrType.Hp)}");

        // 子类自定义治疗处理
        OnHealReceived(evt);
    }

    /// <summary>
    /// 受到治疗时的自定义处理（由子类实现）
    /// </summary>
    protected virtual void OnHealReceived(HealEvent evt)
    {
        // 留给子类实现（如播放治疗特效）
    }

    /// <summary>
    /// 死亡
    /// </summary>
    private void Die(CombatEntity killer)
    {
        var deathEvent = new DeathEvent
        {
            entity = this,
            killer = killer
        };

        EventBus.Fire(deathEvent);
        Debug.Log($"{EntityName} 已死亡");
    }

    /// <summary>
    /// 死亡处理
    /// </summary>
    private void OnDeath(DeathEvent evt)
    {
        if (evt.entity != this)
            return;

        // 清理所有状态
        StateComp.Clear();
        SkillComp.Clear();

        // 挂载“死亡状态”（主状态槽屏障，用于互斥与查询）
        var deathCfg = StateCfgManager.Instance.GetConfig(9001);
        if (deathCfg != null)
        {
            ApplyState(deathCfg, this, 1, null, null);
        }

        // 子类自定义死亡处理
        OnEntityDeath(evt);
    }

    /// <summary>
    /// 实体死亡时的自定义处理（由子类实现）
    /// </summary>
    protected virtual void OnEntityDeath(DeathEvent evt)
    {
        // 默认实现：播放死亡动画（如果有动画组件）
        // AnimComp?.PlayDeathAnimation();
    }

    /// <summary>
    /// 是否存活
    /// </summary>
    public bool IsAlive()
    {
        return AttrComp.IsAlive();
    }

    /// <summary>
    /// 判断目标是否存活
    /// </summary>
    protected bool IsTargetAlive(CombatEntity target)
    {
        var targetEntity = target as CombatEntity;
        return targetEntity != null && targetEntity.IsAlive();
    }

    #endregion

    #region 技能和状态相关方法

    /// <summary>
    /// 统一入口：应用一个状态（技能/被动/Buff都属于State）
    /// 差异应尽量下沉到 StateCfg + State脚本本身，而不是拆分成多个系统。
    /// </summary>
    public State ApplyState(StateCfg cfg, CombatEntity caster = null, int level = 1, object input = null, System.Collections.Generic.List<StateValueFloat> valueOverrides = null)
    {
        if (cfg == null)
        {
            Debug.LogWarning("无效的状态配置");
            return null;
        }

        // 主动技能的默认互斥仍走 CanCastSkill
        if (cfg.isActiveSkill && !CanCastSkill(cfg))
        {
            return null;
        }

        var state = StateUtil.CreateState(cfg.cid, level);
        if (state == null) return null;

        state.Owner = this;
        state.Caster = caster ?? this;
        state.Level = level;
        state.input = input;

        // 覆盖命名参数
        if (valueOverrides != null)
        {
            state.values.Clear();
            state.values.AddRange(valueOverrides);
        }

        // 确保任务启动前 blackboard 已经具备 input/values/target 等运行时数据
        StateUtil.SyncRuntimeDataToBlackboard(state);

        // 挂载状态并启动（统一发布/事件在 StateComponent 内部完成）
        var added = StateComp.AddState(state);

        // 主动技能沿用旧的技能施放回调（兼容）
        if (added != null && cfg.isActiveSkill)
        {
            OnSkillCast(added, added.Blackboard.Get<CombatEntity>("Target", null));
        }

        return added;
    }

    /// <summary>
    /// 施放技能
    /// </summary>
    public State CastSkill(StateCfg skillCfg, CombatEntity target = null)
    {
        if (skillCfg == null || !skillCfg.isActiveSkill)
        {
            Debug.LogWarning("无效的技能配置");
            return null;
        }

        // 检查是否可以施放技能
        if (!CanCastSkill(skillCfg))
        {
            return null;
        }

        // 统一走 ApplyState（重要：目标需要在状态启动/派发 SkillCastEvent 之前进入黑板）
        object input = null;
        if (target != null)
        {
            input = new SkillInput { target = target };
        }
        return ApplyState(skillCfg, this, 1, input, null);
    }

    /// <summary>
    /// 判断是否可以施放技能（由子类实现）
    /// </summary>
    protected virtual bool CanCastSkill(StateCfg skillCfg)
    {
        // 默认检查：是否存活
        if (!IsAlive())
            return false;

        // 默认互斥：同一时刻仅允许一个“主动技能”(前台技能)
        if (skillCfg != null && skillCfg.isActiveSkill && SkillComp != null && SkillComp.IsCastingActiveSkill())
            return false;

        return true;
    }

    /// <summary>
    /// 技能施放时的自定义处理（由子类实现）
    /// </summary>
    protected virtual void OnSkillCast(State skillState, CombatEntity target)
    {
        // 留给子类实现
    }

    /// <summary>
    /// 添加Buff
    /// </summary>
    public State AddBuff(StateCfg buffCfg, CombatEntity caster = null)
    {
        if (buffCfg == null || !buffCfg.isBuff)
        {
            Debug.LogWarning("无效的Buff配置");
            return null;
        }

        // 统一走 ApplyState
        var buff = ApplyState(buffCfg, caster ?? this, 1, null, null);

        // 子类自定义Buff添加处理
        if (buff != null)
        {
            OnBuffAdded(buff);
        }

        return buff;
    }

    /// <summary>
    /// 施加主状态（示例）：眩晕/冰冻/死亡等
    /// </summary>
    public State ApplyMainState(int stateId, int level = 1)
    {
        var cfg = StateCfgManager.Instance.GetConfig(stateId);
        if (cfg == null)
        {
            Debug.LogWarning($"找不到主状态配置: {stateId}");
            return null;
        }
        return ApplyState(cfg, this, level, null, null);
    }

    /// <summary>
    /// Buff添加时的自定义处理（由子类实现）
    /// </summary>
    protected virtual void OnBuffAdded(State buff)
    {
        // 留给子类实现
    }

    /// <summary>
    /// 移除Buff
    /// </summary>
    public bool RemoveBuff(int buffId)
    {
        bool removed = StateComp.RemoveStateById(buffId);

        if (removed)
        {
            OnBuffRemoved(buffId);
        }

        return removed;
    }

    /// <summary>
    /// Buff移除时的自定义处理（由子类实现）
    /// </summary>
    protected virtual void OnBuffRemoved(int buffId)
    {
        // 留给子类实现
    }

    #endregion

    #region 阵营和关系判断

    /// <summary>
    /// 是否为敌人
    /// </summary>
    public virtual bool IsEnemy(CombatEntity other)
    {
        if (other == null) return false;
        return Camp != other.Camp;
    }

    /// <summary>
    /// 是否为友方
    /// </summary>
    public virtual bool IsFriendly(CombatEntity other)
    {
        if (other == null) return false;
        return Camp == other.Camp;
    }

    #endregion

    private void OnDestroy()
    {
        // 清理资源
        EventBus?.Clear();
        StateComp?.Clear();
        SkillComp?.Clear();
    }
}

