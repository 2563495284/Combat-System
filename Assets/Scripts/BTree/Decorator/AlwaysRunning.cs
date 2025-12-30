using System;

namespace BTree.Decorator
{
    /// <summary>
    /// 在子节点完成之后仍返回运行。
    /// 注意：在运行期间只运行一次子节点
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class AlwaysRunning<T> : Decorator<T> where T : class
    {
        [NonSerialized] private bool started;

        public AlwaysRunning()
        {
        }

        public AlwaysRunning(Task<T> child) : base(child)
        {
        }

        protected override void BeforeEnter()
        {
            base.BeforeEnter();
            started = false;
            if (child == null)
            {
                IsBreakInline = true;
            }
        }

        protected override int Execute()
        {
            Task<T> child = this.child;
            if (child == null)
            {
                return TaskStatus.RUNNING;
            }
            if (started && child.IsCompleted)
            { // 勿轻易调整
                return TaskStatus.RUNNING;
            }
            Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
            if (inlinedChild != null)
            {
                inlinedChild.Template_ExecuteInlined(ref inlineHelper, child);
            }
            else if (child.IsRunning)
            {
                child.Template_Execute(true);
            }
            else
            {
                started = true;
                Template_StartChild(child, true, ref inlineHelper);
            }
            if (child.IsCompleted)
            {
                IsBreakInline = true;
            }
            // 需要响应取消
            return child.IsCancelled ? TaskStatus.CANCELLED : TaskStatus.RUNNING;
        }
    }
}