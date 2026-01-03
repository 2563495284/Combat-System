using UnityEngine;

/// <summary>
/// 技能系统（按你给的伪代码风格封装）
/// </summary>
public class SkillSystem : MonoBehaviour
{
    private static SkillSystem _instance;
    public static SkillSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SkillSystem");
                _instance = go.AddComponent<SkillSystem>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// 派发施展技能事件（外部可订阅）
    /// </summary>
    public event System.Action<GameObject, SkillData> BeforeCastSkillEvent;

    /// <summary>
    /// Hook：派发施展技能事件（保留你伪代码里的 BeforeCastSkill 语义）
    /// </summary>
    protected virtual void BeforeCastSkill(GameObject gobj, SkillData skillData)
    {
        BeforeCastSkillEvent?.Invoke(gobj, skillData);
    }

    /// <summary>
    /// 更贴近“一切皆状态”的统一入口：应用一个状态（数据结构仍沿用 SkillData）
    /// </summary>
    public void ApplyState(GameObject gobj, SkillData skillData, SkillInput input)
    {
        if (gobj == null || skillData == null)
        {
            Debug.LogError("[SkillSystem] gobj/skillData 为空");
            return;
        }

        // 派发施展技能事件
        BeforeCastSkill(gobj, skillData);

        // 创建 State，然后覆盖默认由等级算出的数值
        State state = StateUtil.CreateState(skillData.Id, skillData.lv);
        if (state == null) return;

        state.values.Clear();
        if (skillData.values != null)
        {
            state.values.AddRange(skillData.values);
        }

        state.lv = skillData.lv;
        state.input = input;

        // 添加状态 -- 启动技能
        StateMgr.Instance.AddState(gobj, state);
    }

    // 兼容旧命名：施展技能
    public void CastSkill(GameObject gobj, SkillData skillData, SkillInput input)
    {
        ApplyState(gobj, skillData, input);
    }
}


