using System.Collections.Generic;

/// <summary>
/// 状态发布/查询组件（历史命名：SkillComponent）
/// 作用：把“需要被外部快速查询的状态”发布出来（技能/被动/Buff都属于State）
/// - Skill：fgCastingSkill/castingSkills（保留兼容）
/// - Passive/Buff：提供列表以便统一查询
/// </summary>
public class SkillComponent
{
    /// <summary>
    /// 当前执行的所有技能（包含被动）
    /// </summary>
    public readonly List<State> castingSkills = new List<State>();

    /// <summary>
    /// 当前执行的主动技能（前台技能）
    /// </summary>
    public State fgCastingSkill;

    /// <summary>
    /// 当前发布的所有状态（技能/被动/Buff）
    /// </summary>
    public readonly List<State> publishedStates = new List<State>();

    /// <summary>
    /// 当前发布的所有Buff状态
    /// </summary>
    public readonly List<State> buffStates = new List<State>();

    /// <summary>
    /// 当前发布的所有被动技能状态
    /// </summary>
    public readonly List<State> passiveStates = new List<State>();

    /// <summary>
    /// 组件拥有者
    /// </summary>
    public CombatEntity Owner { get; private set; }

    public SkillComponent(CombatEntity owner)
    {
        Owner = owner;
    }

    private class ComboRuntime
    {
        public int comboIndex;
        public float lastCastTime;
    }

    // 记录“同一个技能”的连击运行时数据（用于三连击等单技能内部FSM）
    private readonly Dictionary<int, ComboRuntime> _comboRuntimeBySkillId = new Dictionary<int, ComboRuntime>();

    private ComboRuntime GetOrCreateComboRuntime(int skillId)
    {
        if (!_comboRuntimeBySkillId.TryGetValue(skillId, out var rt))
        {
            rt = new ComboRuntime { comboIndex = 0, lastCastTime = -999f };
            _comboRuntimeBySkillId[skillId] = rt;
        }
        return rt;
    }

    /// <summary>
    /// 获取本次施放应当使用的连击段（不会推进段数）
    /// </summary>
    public int PeekComboIndex(int skillId, float comboTimeoutSeconds, int maxComboCount)
    {
        var rt = GetOrCreateComboRuntime(skillId);

        if (maxComboCount <= 0) return 0;

        float dt = UnityEngine.Time.time - rt.lastCastTime;
        if (rt.lastCastTime < 0f || dt > comboTimeoutSeconds)
        {
            rt.comboIndex = 0;
        }

        rt.comboIndex = (rt.comboIndex % maxComboCount + maxComboCount) % maxComboCount;
        return rt.comboIndex;
    }

    /// <summary>
    /// 在技能正常完成后推进连击段
    /// </summary>
    public void CommitComboOnSuccess(int skillId, int maxComboCount)
    {
        if (maxComboCount <= 0) return;

        var rt = GetOrCreateComboRuntime(skillId);
        rt.lastCastTime = UnityEngine.Time.time;
        rt.comboIndex = (rt.comboIndex + 1) % maxComboCount;
    }

    public void ResetCombo(int skillId)
    {
        if (_comboRuntimeBySkillId.TryGetValue(skillId, out var rt))
        {
            rt.comboIndex = 0;
            rt.lastCastTime = -999f;
        }
    }

    /// <summary>
    /// 发布状态（技能/被动/Buff）
    /// 当状态启动时调用，将其添加到发布列表，提供额外查询能力。
    /// </summary>
    public void PublishState(State state)
    {
        if (state == null || state.Cfg == null)
            return;

        // 发布总表（去重）
        if (!publishedStates.Contains(state))
        {
            publishedStates.Add(state);
        }

        // 分类发布
        if (state.Cfg.isBuff)
        {
            if (!buffStates.Contains(state))
                buffStates.Add(state);
        }

        if (state.Cfg.isPassiveSkill)
        {
            if (!passiveStates.Contains(state))
                passiveStates.Add(state);
        }

        if (state.Cfg.isActiveSkill || state.Cfg.isPassiveSkill)
        {
            if (!castingSkills.Contains(state))
                castingSkills.Add(state);
        }

        // 主动技能作为前台技能
        if (state.Cfg.isActiveSkill)
        {
            fgCastingSkill = state;
        }
    }

    /// <summary>
    /// 取消发布状态（技能/被动/Buff）
    /// 当状态结束/被移除时调用。
    /// </summary>
    public void UnpublishState(State state)
    {
        if (state == null)
            return;

        publishedStates.Remove(state);
        buffStates.Remove(state);
        passiveStates.Remove(state);
        castingSkills.Remove(state);

        if (fgCastingSkill == state)
        {
            fgCastingSkill = null;
        }
    }

    /// <summary>
    /// 兼容旧接口：发布技能状态
    /// </summary>
    public void PublishSkill(State state) => PublishState(state);

    /// <summary>
    /// 兼容旧接口：取消发布技能状态
    /// </summary>
    public void UnpublishSkill(State state) => UnpublishState(state);

    /// <summary>
    /// 是否正在施放主动技能
    /// </summary>
    public bool IsCastingActiveSkill()
    {
        return fgCastingSkill != null && fgCastingSkill.Active;
    }

    /// <summary>
    /// 是否正在施放指定技能
    /// </summary>
    public bool IsCastingSkill(int skillId)
    {
        return castingSkills.Exists(s => s.Cfg.cid == skillId && s.Active);
    }

    /// <summary>
    /// 获取正在施放的技能
    /// </summary>
    public State GetCastingSkill(int skillId)
    {
        return castingSkills.Find(s => s.Cfg.cid == skillId && s.Active);
    }

    /// <summary>
    /// 中断当前主动技能
    /// </summary>
    public void InterruptActiveSkill()
    {
        if (fgCastingSkill != null)
        {
            // 生命周期统一由 StateComponent 管理：这里应当移除状态，而不是只 Stop/Unpublish
            Owner?.StateComp?.RemoveState(fgCastingSkill, BTree.TaskStatus.CANCELLED, "InterruptActiveSkill");
        }
    }

    /// <summary>
    /// 清除所有技能
    /// </summary>
    public void Clear()
    {
        // 只清理索引/缓存，不直接 Stop 状态（生命周期由 StateComponent 统一管理）
        publishedStates.Clear();
        buffStates.Clear();
        passiveStates.Clear();
        castingSkills.Clear();
        fgCastingSkill = null;
        _comboRuntimeBySkillId.Clear();
    }
}

