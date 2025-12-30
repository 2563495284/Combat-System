namespace BTree.Decorator
{
    /// <summary>
    /// 反转装饰器，它用于反转子节点的执行结果。
    /// 如果被装饰的任务失败，它将返回成功；
    /// 如果被装饰的任务成功，它将返回失败；
    /// 如果被装饰的任务取消，它将返回取消。
    ///
    /// 对于普通的条件节点，可以通过控制流标记直接取反<see cref="Task{T}.IsInvertedGuard"/>，避免增加封装。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class Inverter<T> : Decorator<T> where T : class
    {
        public Inverter()
        {
        }

        public Inverter(Task<T> child) : base(child)
        {
        }

        protected override int Enter()
        {
            if (IsCheckingGuard())
            {
                Template_CheckGuard(child);
                return TaskStatus.Invert(child.Status);
            }
            return TaskStatus.RUNNING;
        }

        protected override int Execute()
        {
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
                Template_StartChild(child, true, ref inlineHelper);
            }
            return child.IsCompleted ? TaskStatus.Invert(child.Status) : TaskStatus.RUNNING;
        }
    }
}