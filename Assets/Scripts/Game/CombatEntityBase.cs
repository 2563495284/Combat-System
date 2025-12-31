using UnityEngine;

/// <summary>
/// 战斗实体基类定义
/// 用于解决循环依赖问题
/// </summary>
public abstract partial class CombatEntity : MonoBehaviour
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// 实体名称
    /// </summary>
    public string EntityName { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// 阵营
    /// </summary>
    public int Camp { get; set; }
}

