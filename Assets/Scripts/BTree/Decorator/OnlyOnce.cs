namespace BTree.Decorator
{
#nullable enable
    /// <summary>
    /// 只执行一次。
    /// 1.适用那些不论成功与否只执行一次的行为。
    /// 2.在调用<see cref="Task{T}.ResetForRestart()"/>后可再次运行。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class OnlyOnce<T> : Decorator<T> where T : class
    {
        public OnlyOnce()
        {
        }

        public OnlyOnce(Task<T> child) : base(child)
        {
        }

        protected override int Execute()
        {
            if (child.IsCompleted)
            {
                return child.Status;
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
                Template_StartChild(child, true, ref inlineHelper);
            }
            return child.Status;
        }
    }
}