using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 黑板系统 - 用于存储状态的动态数据
/// 支持任意类型的键值对存储
/// </summary>
public class Blackboard
{
    private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

    /// <summary>
    /// 设置数据
    /// </summary>
    public void Set<T>(string key, T value)
    {
        _data[key] = value;
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    public T Get<T>(string key, T defaultValue = default)
    {
        if (_data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// 尝试获取数据
    /// </summary>
    public bool TryGet<T>(string key, out T value)
    {
        if (_data.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// 检查是否包含指定键
    /// </summary>
    public bool Contains(string key)
    {
        return _data.ContainsKey(key);
    }

    /// <summary>
    /// 移除数据
    /// </summary>
    public bool Remove(string key)
    {
        return _data.Remove(key);
    }

    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        _data.Clear();
    }

    /// <summary>
    /// 获取所有键
    /// </summary>
    public IEnumerable<string> Keys => _data.Keys;
}


