namespace BTree.Decorator
{
    /// <summary>
    /// 在子节点完成之后固定返回成功
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class AlwaysSuccess<T> : Decorator<T> where T : class
    {
        public AlwaysSuccess()
        {
        }

        public AlwaysSuccess(Task<T> child) : base(child)
        {
        }

        protected override int Execute()
        {
            if (child == null)
            {
                return TaskStatus.SUCCESS;
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
            return child.IsCompleted ? TaskStatus.SUCCESS : TaskStatus.RUNNING;
        }
    }
}