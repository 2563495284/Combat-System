namespace BTree.Branch.Join
{
    /// <summary>
    /// Join版本的Selector
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JoinSelector<T> : JoinPolicy<T> where T : class
    {
        /** 单例 */
        private static readonly JoinSelector<T> INST = new JoinSelector<T>();

        public static JoinSelector<T> GetInstance() => INST;

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
                return TaskStatus.CHILDLESS;
            }
            return TaskStatus.RUNNING;
        }

        public int OnChildCompleted(Join<T> join, Task<T> child)
        {
            if (child.IsSucceeded)
            {
                return TaskStatus.SUCCESS;
            }
            if (join.IsAllChildCompleted)
            {
                return TaskStatus.ERROR;
            }
            return TaskStatus.RUNNING;
        }

        public void OnEvent(Join<T> join, object eventObj)
        {
        }
    }
}