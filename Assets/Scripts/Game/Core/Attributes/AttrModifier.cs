
/// <summary>
/// 属性修改器
/// 用于动态修改实体属性（来自Buff、装备等）
/// </summary>
public class AttrModifier
{
    /// <summary>
    /// 修改值
    /// </summary>
    public float value;

    /// <summary>
    /// 是否是百分比
    /// </summary>
    public bool isPercent;

    /// <summary>
    /// 来源（用于追踪和移除）
    /// </summary>
    public object source;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="value">修改值</param>
    /// <param name="isPercent">是否为百分比修改</param>
    /// <param name="source">修改来源（可选）</param>
    public AttrModifier(float value, bool isPercent = false, object source = null)
    {
        this.value = value;
        this.isPercent = isPercent;
        this.source = source;
    }
}

