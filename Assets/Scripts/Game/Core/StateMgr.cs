using UnityEngine;

/// <summary>
/// 状态管理器（适配“一切皆状态”调用方式）
/// - 外部用 GameObject + State 交互
/// - 这里只负责“把运行时数据写进 State/Blackboard”，真正的发布/事件/互斥由 StateComponent 统一处理
/// </summary>
public class StateMgr : MonoBehaviour
{
    private static StateMgr _instance;
    public static StateMgr Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("StateMgr");
                _instance = go.AddComponent<StateMgr>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public State AddState(GameObject gobj, State state)
    {
        if (gobj == null || state == null)
        {
            Debug.LogError("[StateMgr] gobj/state 为空");
            return null;
        }

        var entity = gobj.GetComponent<CombatEntity>();
        if (entity == null)
        {
            Debug.LogError($"[StateMgr] GameObject 上找不到 CombatEntity: {gobj.name}");
            return null;
        }

        // 绑定 owner/caster
        state.Owner = state.Owner ?? entity;
        state.Caster = state.Caster ?? entity;

        // 统一将 input/values 同步到黑板（任务树统一从黑板读取）
        StateUtil.SyncRuntimeDataToBlackboard(state);

        // 真正挂载到实体（绑定槽 + 启动）
        var added = entity.StateComp.AddState(state);
        if (added == null) return null;

        return added;
    }
}


