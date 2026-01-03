using System;
using System.Linq;
using UnityEngine;
using BTree;

/// <summary>
/// 状态配置
/// </summary>
[Serializable]
public class StateCfg
{
    /// <summary>
    /// 状态ID
    /// </summary>
    public int cid;

    /// <summary>
    /// 绑定的状态槽，-1表示不指定
    /// </summary>
    public int slot = -1;

    /// <summary>
    /// 状态名称
    /// </summary>
    public string name;

    /// <summary>
    /// 状态描述
    /// </summary>
    public string description;

    /// <summary>
    /// 默认持续时间（毫秒）
    /// -1表示永久
    /// </summary>
    public int duration = -1;

    /// <summary>
    /// 最大叠层数
    /// </summary>
    public int maxStack = 1;

    /// <summary>
    /// 是否可以被驱散
    /// </summary>
    public bool canDispel = true;

    /// <summary>
    /// 是否是主动技能
    /// </summary>
    public bool isActiveSkill = false;

    /// <summary>
    /// 是否是被动技能
    /// </summary>
    public bool isPassiveSkill = false;

    /// <summary>
    /// 是否是Buff
    /// </summary>
    public bool isBuff = false;

    /// <summary>
    /// 状态优先级（用于状态槽冲突时的判断）
    /// </summary>
    public int priority = 0;

    /// <summary>
    /// 任务脚本类型名称
    /// </summary>
    public string taskTypeName;

    /// <summary>
    /// 创建任务实例
    /// </summary>
    public Task<Blackboard> CreateTask()
    {
        if (string.IsNullOrEmpty(taskTypeName))
            return null;

        try
        {
            var type = Type.GetType(taskTypeName);
            if (type != null && typeof(Task<Blackboard>).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type) as Task<Blackboard>;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"创建任务失败: {taskTypeName}, 错误: {e.Message}");
        }

        return null;
    }
}

