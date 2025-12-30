using System.Collections.Generic;

namespace BTree.Branch
{
#nullable enable
    /// <summary>
    /// 简单并发节点。
    /// 1.其中第一个任务为主要任务，其余任务为次要任务l；
    /// 2.一旦主要任务完成，则节点进入完成状态；次要任务可能被运行多次。
    /// 3.外部事件将派发给主要任务。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleParallel<T> : ParallelBranch<T> where T : class
    {
        public SimpleParallel()
        {
        }

        public SimpleParallel(List<Task<T>>? children) : base(children)
        {
        }

        protected override int Enter()
        {
            InitChildHelpers(false);
            return TaskStatus.RUNNING;
        }

        protected override int Execute()
        {
            List<Task<T>> children = this.children;
            for (int idx = 0; idx < children.Count; idx++)
            {
                Task<T> child = children[idx];
                ParallelChildHelper<T> childHelper = GetChildHelper(child);
                Task<T> inlinedChild = childHelper.GetInlinedChild();
                if (inlinedChild != null)
                {
                    inlinedChild.Template_ExecuteInlined(ref childHelper.Unwrap(), child);
                }
                else if (child.IsRunning)
                {
                    child.Template_Execute(true);
                }
                else
                {
                    SetChildCancelToken(child, childHelper.cancelToken); // 运行前赋值取消令牌
                    Template_StartChild(child, true, ref childHelper.Unwrap());
                }
                if (child.IsCompleted)
                {
                    UnsetChildCancelToken(child); // 运行结束删除令牌
                    if (idx == 0)
                    {
                        return child.Status;
                    }
                }
                if (cancelToken.IsCancelRequested)
                { // 收到取消信号
                    return TaskStatus.CANCELLED;
                }
            }
            return TaskStatus.RUNNING;
        }

        protected override void OnEventImpl(object eventObj)
        {
            Task<T> mainTask = children[0];
            ParallelChildHelper<T> childHelper = GetChildHelper(mainTask);

            Task<T> inlinedChild = childHelper.GetInlinedChild();
            if (inlinedChild != null)
            {
                inlinedChild.OnEvent(eventObj);
            }
            else
            {
                mainTask.OnEvent(eventObj);
            }
        }
    }
}