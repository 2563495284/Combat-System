namespace BTree.Decorator
{
    /// <summary>
    /// 每一帧都检查子节点的前置条件，如果前置条件失败，则取消child执行并返回失败。
    /// 这是一个常用的节点类型，我们做内联优化，可以提高效率。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AlwaysCheckGuard<T> : Decorator<T> where T : class
    {
        public AlwaysCheckGuard()
        {
        }

        public AlwaysCheckGuard(Task<T> child) : base(child)
        {
        }

        protected override int Execute()
        {
            if (Template_CheckGuard(child.Guard))
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
                    Template_StartChild(child, false, ref inlineHelper);
                }
                return child.Status;
            }
            else
            {
                child.Stop();
                inlineHelper.StopInline(); // help gc
                return TaskStatus.ERROR;
            }
        }
    }
}