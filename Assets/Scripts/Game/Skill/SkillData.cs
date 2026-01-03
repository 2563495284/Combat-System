using System;
using System.Collections.Generic;

/// <summary>
/// 技能数据（通常来自配置/网络/存档）
/// - Id: 技能对应的 StateCfg.cid
/// - lv: 技能等级
/// - values: 覆盖默认按等级计算出的数值参数（命名参数）
/// </summary>
[Serializable]
public class SkillData
{
    public int Id;
    public int lv = 1;
    public List<StateValueFloat> values = new List<StateValueFloat>();
}


