using System;
using UnityEngine;

/// <summary>
/// 技能输入（按你项目需要扩展：方向、点选位置、目标等）
/// 放在 Core 目录是为了让 State/状态层可以直接依赖它。
/// </summary>
[Serializable]
public class SkillInput
{
    public Vector3 direction = Vector3.forward;
    public Vector3 position;
    public CombatEntity target;
}


