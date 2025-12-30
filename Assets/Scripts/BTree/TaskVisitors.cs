namespace BTree
{
#nullable enable
    public static class TaskVisitors
    {
        public static TaskVisitor<T> RefreshActive<T>() where T : class
        {
            return RefreshActiveVisitor<T>.Inst;
        }

        public static TaskVisitor<T> ResetForRestart<T>() where T : class
        {
            return ResetForRestartVisitor<T>.Inst;
        }

        private class RefreshActiveVisitor<T> : TaskVisitor<T> where T : class
        {
            public static readonly RefreshActiveVisitor<T> Inst = new RefreshActiveVisitor<T>();

            public void VisitChild(Task<T> child, int index, object? param)
            {
                if (child.IsRunning) child.RefreshActiveInHierarchy();
            }

            public void VisitHook(Task<T> child, object? param)
            {
                if (child.IsRunning) child.RefreshActiveInHierarchy();
            }
        }

        private class ResetForRestartVisitor<T> : TaskVisitor<T> where T : class
        {
            public static readonly ResetForRestartVisitor<T> Inst = new ResetForRestartVisitor<T>();

            public void VisitChild(Task<T> child, int index, object? param)
            {
                child.ResetForRestart();
            }

            public void VisitHook(Task<T> child, object? param)
            {
                child.ResetForRestart();
            }
        }
    }
}