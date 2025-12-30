namespace BTree.Branch
{
    /// <summary>
    /// 分支选择接口
    /// </summary>
    public interface ISwitchHandler<T> where T : class
    {
        /// <summary>
        /// 选择要执行的子节点
        /// </summary>
        /// <param name="branchTask">要测试的分支</param>
        /// <returns>选中的分支索引，-1表示未选中</returns>
        int Select(BranchTask<T> branchTask);
    }
}