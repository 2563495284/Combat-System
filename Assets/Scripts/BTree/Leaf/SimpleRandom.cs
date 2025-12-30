
using Commons;
using Commons.Collections;

namespace BTree.Leaf
{
    /// <summary>
    /// 简单随机节点
    /// 在正式的项目中，Random应当从实体上获取。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleRandom<T> : LeafTask<T> where T : class
    {
        private float p;

        public SimpleRandom()
        {
        }

        public SimpleRandom(float p = 0.5f)
        {
            this.p = p;
        }

        protected override int Execute()
        {
            if (CollectionUtil.SharedRandom.NextDouble() <= p)
            {
                return TaskStatus.SUCCESS;
            }
            else
            {
                return TaskStatus.ERROR;
            }
        }

        protected override void OnEventImpl(object _)
        {
        }

        /// <summary>
        /// 概率
        /// </summary>
        public float P
        {
            get => p;
            set => p = value;
        }
    }
}