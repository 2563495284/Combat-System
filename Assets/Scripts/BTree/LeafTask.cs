using System;
using Commons;

namespace BTree
{
    /// <summary>
    /// 叶子节点的超类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LeafTask<T> : Task<T> where T : class
    {
#nullable disable

        #region child

        public override void VisitChildren(TaskVisitor<T> visitor, object param)
        {
        }

        public sealed override int IndexChild(Task<T> task)
        {
            return -1;
        }

        public override int ChildCount => 0;

        public sealed override Task<T> GetChild(int index)
        {
            throw new IndexOutOfRangeException("Leaf task can not have any children");
        }

        protected sealed override int AddChildImpl(Task<T> task)
        {
            throw new IllegalStateException("Leaf task can not have any children");
        }

        protected sealed override Task<T> SetChildImpl(int index, Task<T> task)
        {
            throw new IllegalStateException("Leaf task can not have any children");
        }

        protected sealed override Task<T> RemoveChildImpl(int index)
        {
            throw new IndexOutOfRangeException("Leaf task can not have any children");
        }

        #endregion
    }
}