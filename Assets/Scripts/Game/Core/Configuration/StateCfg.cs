using System;
using System.Linq;
using UnityEngine;
using BTree;
using System.Reflection;

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
    /// 是否发布到查询组件（SkillComponent / 发布列表）
    /// - true: 可通过 publishedStates/buffStates/fgCastingSkill 等被外部快速查询
    /// - false: 仍会执行，但不会进入发布列表（适合内部状态/纯逻辑状态）
    /// </summary>
    public bool publish = true;

    /// <summary>
    /// 跨槽互斥：互斥组ID（0表示不启用）
    /// 同一互斥组内的状态不能共存；冲突时按 priority 决定是否能挤掉对方。
    /// </summary>
    public int mutexGroup = 0;

    /// <summary>
    /// 跨槽互斥：是否与所有状态互斥
    /// 适用于“死亡/冻结/过场”等需要清场的状态。
    /// 冲突时同样按 priority 决定是否能清掉对方。
    /// </summary>
    public bool mutexAll = false;

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
            // 1) 先尝试 Type.GetType（支持“全名, 程序集名”）
            var type = Type.GetType(taskTypeName);

            // 2) 兼容 Unity asmdef：仅给了类名/全名时，遍历程序集查找
            if (type == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length && type == null; i++)
                {
                    Assembly asm = assemblies[i];
                    // 优先按 FullName 精确匹配，再按 Name 匹配（无 namespace 的旧写法）
                    type = asm.GetType(taskTypeName, false)
                           ?? asm.GetTypes().FirstOrDefault(t => t.Name == taskTypeName);
                }
            }

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

