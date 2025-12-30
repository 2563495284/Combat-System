namespace BTree.Decorator
{
    /// <summary>
    /// 子树引用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [TaskInlinable]
    public class SubtreeRef<T> : Decorator<T> where T : class
    {
#nullable disable
        private string subtreeName;
#nullable enable

        public SubtreeRef()
        {
        }

        public SubtreeRef(string subtreeName)
        {
            this.subtreeName = subtreeName;
        }

        protected override void BeforeEnter()
        {
            base.BeforeEnter();
            if (child == null)
            {
                Task<T> rootTask = TaskEntry.TreeLoader.LoadRootTask<T>(subtreeName);
                AddChild(rootTask);
            }
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
            return child.Status;
        }

        /// <summary>
        /// 子树的名字
        /// </summary>
        public string SubtreeName
        {
            get => subtreeName;
            set => subtreeName = value;
        }
    }
}