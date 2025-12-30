using System.Collections.Generic;

namespace BTree.Branch
{
#nullable enable
/// <summary>
/// 主动选择节点
/// 每次运行时都会重新测试节点的运行条件，选择一个新的可运行节点。
/// 如果新选择的运行节点与之前的运行节点不同，则取消之前的任务。
/// (ActiveSelector也是比较常用的节点，做内联支持是合适的)
/// </summary>
/// <typeparam name="T"></typeparam>
public class ActiveSelector<T> : SingleRunningChildBranch<T> where T : class
{
    public ActiveSelector() {
    }

    public ActiveSelector(List<Task<T>>? children) : base(children) {
    }

    protected override int Execute() {
        Task<T>? childToRun = null;
        int childIndex = -1;
        for (int idx = 0; idx < children.Count; idx++) {
            Task<T> child = children[idx];
            if (!Template_CheckGuard(child.Guard)) {
                continue; // 不能调用SetGuardFailed，会中断当前运行中的child
            }
            childToRun = child;
            childIndex = idx;
            break;
        }

        if (childToRun == null) {
            Stop(this.runningChild); // 不清理index，允许退出后查询最后一次运行的child
            return TaskStatus.ERROR;
        }

        Task<T> runningChild = this.runningChild;
        if (runningChild == childToRun) {
            Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
            if (inlinedChild != null) {
                inlinedChild.Template_ExecuteInlined(ref inlineHelper, runningChild);
            } else if (runningChild.IsRunning) {
                runningChild.Template_Execute(true);
            } else {
                Template_StartChild(runningChild, false, ref inlineHelper);
            }
        } else {
            if (runningChild != null) {
                runningChild.Stop();
                inlineHelper.StopInline();
            }
            this.runningChild = childToRun;
            this.runningIndex = childIndex;
            Template_StartChild(childToRun, false, ref inlineHelper);
        }
        return childToRun.Status;
    }

    protected override int OnChildCompleted(Task<T> child) {
        throw new System.NotImplementedException();
    }
}
}