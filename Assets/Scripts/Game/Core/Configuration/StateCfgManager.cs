using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态配置管理器
/// 管理所有状态、技能、Buff的配置
/// </summary>
public class StateCfgManager : MonoBehaviour
{
    private static StateCfgManager _instance;
    public static StateCfgManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("StateCfgManager");
                _instance = go.AddComponent<StateCfgManager>();
                DontDestroyOnLoad(go);
                _instance.Initialize();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 配置字典
    /// </summary>
    private readonly Dictionary<int, StateCfg> _configs = new Dictionary<int, StateCfg>();

    private void Initialize()
    {
        // 初始化默认配置
        InitializeDefaultConfigs();
    }

    /// <summary>
    /// 初始化默认配置（示例）
    /// </summary>
    private void InitializeDefaultConfigs()
    {
        // 技能：普通攻击
        AddConfig(new StateCfg
        {
            cid = 1001,
            name = "普通攻击",
            slot = -1,
            duration = 500,  // 0.5秒的攻击动作
            isActiveSkill = true,
            // 注意：这里是“技能系统”的任务类型，不是 PlayerControl 的 AttackStateTask
            taskTypeName = "NormalAttackSkillTask"
        });

        Debug.Log($"[StateCfgManager] 加载了 {_configs.Count} 个配置");
    }

    /// <summary>
    /// 添加配置
    /// </summary>
    public void AddConfig(StateCfg cfg)
    {
        if (cfg == null)
        {
            Debug.LogError("[StateCfgManager] 配置为空");
            return;
        }

        _configs[cfg.cid] = cfg;
    }

    /// <summary>
    /// 获取配置
    /// </summary>
    public StateCfg GetConfig(int cid)
    {
        return _configs.TryGetValue(cid, out var cfg) ? cfg : null;
    }

    /// <summary>
    /// 移除配置
    /// </summary>
    public bool RemoveConfig(int cid)
    {
        return _configs.Remove(cid);
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public Dictionary<int, StateCfg> GetAllConfigs()
    {
        return new Dictionary<int, StateCfg>(_configs);
    }

    /// <summary>
    /// 从JSON加载配置
    /// </summary>
    public void LoadFromJson(string json)
    {
        try
        {
            var configs = JsonUtility.FromJson<StateCfgList>(json);
            if (configs != null && configs.configs != null)
            {
                foreach (var cfg in configs.configs)
                {
                    AddConfig(cfg);
                }
                Debug.Log($"[StateCfgManager] 从JSON加载了 {configs.configs.Length} 个配置");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StateCfgManager] 加载JSON失败: {e.Message}");
        }
    }

    /// <summary>
    /// 导出为JSON
    /// </summary>
    public string ExportToJson()
    {
        var list = new List<StateCfg>(_configs.Values);
        var wrapper = new StateCfgList { configs = list.ToArray() };
        return JsonUtility.ToJson(wrapper, true);
    }

    [System.Serializable]
    private class StateCfgList
    {
        public StateCfg[] configs;
    }
}

