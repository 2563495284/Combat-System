using UnityEngine;

/// <summary>
/// 状态工厂：按 StateCfg 创建 State（“状态即脚本”）
/// </summary>
public static class StateUtil
{
    public static State CreateState(int stateId, int lv = 1)
    {
        var cfg = StateCfgManager.Instance.GetConfig(stateId);
        if (cfg == null)
        {
            Debug.LogError($"[StateUtil] 找不到状态配置: {stateId}");
            return null;
        }

        var state = new State(cfg)
        {
            Level = Mathf.Max(1, lv),
        };
        return state;
    }

    /// <summary>
    /// 将 State 的运行时数据（input/values）同步写入 Blackboard。
    /// 约定：任务树优先从黑板读取命名参数，而不是直接读 State.values。
    /// </summary>
    public static void SyncRuntimeDataToBlackboard(State state)
    {
        if (state == null || state.Blackboard == null) return;

        // input
        var skillInput = state.input as SkillInput;
        if (skillInput != null)
        {
            state.Blackboard.Set("SkillInput", skillInput);
            if (skillInput.target != null)
            {
                state.Blackboard.Set("Target", skillInput.target);
            }
        }

        // values（命名参数）
        if (state.values != null)
        {
            for (int i = 0; i < state.values.Count; i++)
            {
                var kv = state.values[i];
                if (!string.IsNullOrEmpty(kv.key))
                {
                    state.Blackboard.Set(kv.key, kv.value);
                }
            }
        }
    }
}


