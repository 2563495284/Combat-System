using System;

namespace BTree.Branch.Join
{
    /// <summary>
    /// Join版本的SelectorN
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JoinSelectorN<T> : JoinPolicy<T> where T : class
    {
        /** 需要达成的次数 */
        private int required = 1;
        /** 是否快速失败 */
        private bool failFast;
        /** 前几个任务必须成功 */
        private int sequence;

        public JoinSelectorN()
        {
        }

        public JoinSelectorN(int required, bool failFast = false)
        {
            this.required = required;
            this.failFast = failFast;
        }

        public void ResetForRestart()
        {
        }

        public void BeforeEnter(Join<T> join)
        {
            sequence = Math.Clamp(sequence, 0, required);
        }

        public int Enter(Join<T> join)
        {
            if (required <= 0)
            {
                return TaskStatus.SUCCESS;
            }
            if (join.ChildCount == 0)
            {
                return TaskStatus.CHILDLESS;
            }
            if (CheckFailFast(join))
            {
                return TaskStatus.INSUFFICIENT_CHILD;
            }
            return TaskStatus.RUNNING;
        }

        public int OnChildCompleted(Join<T> join, Task<T> child)
        {
            if (join.SucceededCount >= required && CheckSequence(join))
            {
                return TaskStatus.SUCCESS;
            }
            if (join.IsAllChildCompleted || CheckFailFast(join))
            {
                return TaskStatus.ERROR;
            }
            return TaskStatus.RUNNING;
        }

        private bool CheckSequence(Join<T> join)
        {
            if (sequence == 0)
            {
                return true;
            }
            for (int idx = sequence - 1; idx >= 0; idx--)
            {
                if (!join.GetChild(idx).IsSucceeded)
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckFailFast(Join<T> join)
        {
            if (!failFast)
            {
                return false;
            }
            if (join.ChildCount - join.CompletedCount < required - join.SucceededCount)
            {
                return true;
            }
            for (int idx = 0; idx < sequence; idx++)
            {
                if (join.GetChild(idx).IsFailed)
                {
                    return true;
                }
            }
            return false;
        }

        public void OnEvent(Join<T> join, object eventObj)
        {
        }

        public int Required
        {
            get => required;
            set => required = value;
        }

        public bool FailFast
        {
            get => failFast;
            set => failFast = value;
        }

        public int Sequence
        {
            get => sequence;
            set => sequence = value;
        }
    }
}