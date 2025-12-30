namespace BTree.Branch.Join
{
    /// <summary>
    /// Join版本的Sequence
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JoinSequence<T> : JoinPolicy<T> where T : class
    {
        /** 单例 */
        private static readonly JoinSequence<T> INST = new JoinSequence<T>();

        public static JoinSequence<T> GetInstance() => INST;

        public void ResetForRestart()
        {
        }

        public void BeforeEnter(Join<T> join)
        {
        }

        public int Enter(Join<T> join)
        {
            if (join.ChildCount == 0)
            {
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        public int OnChildCompleted(Join<T> join, Task<T> child)
        {
            if (!child.IsSucceeded)
            {
                return child.Status;
            }
            if (join.IsAllChildSucceeded)
            {
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        public void OnEvent(Join<T> join, object eventObj)
        {
        }
    }
}