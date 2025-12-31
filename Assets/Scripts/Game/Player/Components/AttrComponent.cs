using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性组件
/// 管理实体的各种属性
/// </summary>
public class AttrComponent
{
    /// <summary>
    /// 属性字典
    /// </summary>
    private readonly Dictionary<AttrType, float> _attrs = new Dictionary<AttrType, float>();

    /// <summary>
    /// 属性修改器字典（来自状态、装备等）
    /// </summary>
    private readonly Dictionary<AttrType, List<AttrModifier>> _modifiers = new Dictionary<AttrType, List<AttrModifier>>();

    /// <summary>
    /// 组件拥有者
    /// </summary>
    public CombatEntity Owner { get; private set; }

    public AttrComponent(CombatEntity owner)
    {
        Owner = owner;
        InitializeDefaultAttrs();
    }

    /// <summary>
    /// 初始化默认属性
    /// </summary>
    private void InitializeDefaultAttrs()
    {
        SetBaseAttr(AttrType.MaxHp, 100);
        SetBaseAttr(AttrType.Hp, 100);
        SetBaseAttr(AttrType.MaxMp, 100);
        SetBaseAttr(AttrType.Mp, 100);
        SetBaseAttr(AttrType.Attack, 10);
        SetBaseAttr(AttrType.Defense, 5);
        SetBaseAttr(AttrType.MoveSpeed, 5);
        SetBaseAttr(AttrType.CritRate, 5);
        SetBaseAttr(AttrType.CritDamage, 150);
        SetBaseAttr(AttrType.HitRate, 100);
    }

    /// <summary>
    /// 设置基础属性
    /// </summary>
    public void SetBaseAttr(AttrType type, float value)
    {
        _attrs[type] = value;
    }

    /// <summary>
    /// 获取属性值（包含修改器）
    /// </summary>
    public float GetAttr(AttrType type)
    {
        float baseValue = _attrs.TryGetValue(type, out var val) ? val : 0;

        if (_modifiers.TryGetValue(type, out var modifierList))
        {
            float addValue = 0;
            float mulValue = 1;

            foreach (var modifier in modifierList)
            {
                if (modifier.isPercent)
                {
                    mulValue *= (1 + modifier.value / 100f);
                }
                else
                {
                    addValue += modifier.value;
                }
            }

            return (baseValue + addValue) * mulValue;
        }

        return baseValue;
    }

    /// <summary>
    /// 添加属性修改器
    /// </summary>
    public void AddModifier(AttrType type, AttrModifier modifier)
    {
        if (!_modifiers.ContainsKey(type))
        {
            _modifiers[type] = new List<AttrModifier>();
        }
        _modifiers[type].Add(modifier);
    }

    /// <summary>
    /// 移除属性修改器
    /// </summary>
    public bool RemoveModifier(AttrType type, AttrModifier modifier)
    {
        if (_modifiers.TryGetValue(type, out var list))
        {
            return list.Remove(modifier);
        }
        return false;
    }

    /// <summary>
    /// 修改当前值（如扣血、回血）
    /// </summary>
    public void ModifyCurrentValue(AttrType type, float delta)
    {
        if (_attrs.ContainsKey(type))
        {
            _attrs[type] = Mathf.Max(0, _attrs[type] + delta);

            // 限制不超过最大值
            if (type == AttrType.Hp)
            {
                _attrs[type] = Mathf.Min(_attrs[type], GetAttr(AttrType.MaxHp));
            }
            else if (type == AttrType.Mp)
            {
                _attrs[type] = Mathf.Min(_attrs[type], GetAttr(AttrType.MaxMp));
            }
        }
    }

    /// <summary>
    /// 是否存活
    /// </summary>
    public bool IsAlive()
    {
        return GetAttr(AttrType.Hp) > 0;
    }

    /// <summary>
    /// 清除所有修改器
    /// </summary>
    public void ClearModifiers()
    {
        _modifiers.Clear();
    }
}


