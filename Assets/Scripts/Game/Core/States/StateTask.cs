using UnityEngine;
using BTree;

/// <summary>
/// 通用状态任务基类（非技能/非Buff也可以用它）
/// </summary>
public abstract class StateTask : LeafTask<Blackboard>, IStateTask
{
    protected State State { get; private set; }
    protected CombatEntity Owner => State?.Owner;
    protected CombatEntity Caster => State?.Caster;

    private float _lastUpdateTime;

    public void SetState(State state)
    {
        State = state;
    }

    protected float DeltaTime
    {
        get
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;
            return deltaTime;
        }
    }

    protected sealed override int Execute()
    {
        return OnUpdate(DeltaTime);
    }

    protected override int Enter()
    {
        _lastUpdateTime = Time.time;
        return OnEnter();
    }

    protected virtual int OnEnter() => TaskStatus.RUNNING;

    protected abstract int OnUpdate(float deltaTime);

    protected override void OnEventImpl(object eventObj)
    {
        OnStateEvent(eventObj);
    }

    protected virtual void OnStateEvent(object evt) { }
}


