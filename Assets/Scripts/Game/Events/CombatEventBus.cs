using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗事件总线
/// 用于实体内部的事件通信
/// </summary>
public class CombatEventBus
{
    /// <summary>
    /// 事件监听器字典
    /// </summary>
    private readonly Dictionary<Type, List<Delegate>> _listeners = new Dictionary<Type, List<Delegate>>();

    /// <summary>
    /// 注册事件监听
    /// </summary>
    public void Register<T>(Action<T> handler) where T : class
    {
        Type eventType = typeof(T);
        if (!_listeners.ContainsKey(eventType))
        {
            _listeners[eventType] = new List<Delegate>();
        }

        _listeners[eventType].Add(handler);
    }

    /// <summary>
    /// 取消注册事件监听
    /// </summary>
    public void Unregister<T>(Action<T> handler) where T : class
    {
        Type eventType = typeof(T);
        if (_listeners.TryGetValue(eventType, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0)
            {
                _listeners.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    public void Fire<T>(T evt) where T : class
    {
        Type eventType = typeof(T);
        if (_listeners.TryGetValue(eventType, out var list))
        {
            // 复制列表防止迭代中修改
            var listCopy = new List<Delegate>(list);
            foreach (var handler in listCopy)
            {
                try
                {
                    (handler as Action<T>)?.Invoke(evt);
                }
                catch (Exception e)
                {
                    Debug.LogError($"事件处理异常: {eventType.Name}, 错误: {e}");
                }
            }
        }
    }

    /// <summary>
    /// 清除所有监听器
    /// </summary>
    public void Clear()
    {
        _listeners.Clear();
    }

    /// <summary>
    /// 获取监听器数量
    /// </summary>
    public int GetListenerCount<T>() where T : class
    {
        Type eventType = typeof(T);
        return _listeners.TryGetValue(eventType, out var list) ? list.Count : 0;
    }
}

