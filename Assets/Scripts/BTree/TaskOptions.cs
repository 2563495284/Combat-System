namespace BTree
{
    /// <summary>
    /// 行为树对外的选项
    ///
    /// 通过静态导入引入到Task类，以避免重复定义
    /// </summary>
    public static class TaskOptions
    {
        public const int MASK_SLOW_START = 1 << 24;
        public const int MASK_AUTO_RESET_CHILDREN = 1 << 25;
        public const int MASK_MANUAL_CHECK_CANCEL = 1 << 26;
        public const int MASK_AUTO_LISTEN_CANCEL = 1 << 27;
        public const int MASK_CANCEL_TOKEN_PER_CHILD = 1 << 28;
        public const int MASK_BLACKBOARD_PER_CHILD = 1 << 29;
        public const int MASK_INVERTED_GUARD = 1 << 30;
        public const int MASK_BREAK_INLINE = 1 << 31;
        /** 高8位为流程控制特征值（对外开放）*/
        public const int MASK_CONTROL_FLOW_OPTIONS = (-1) << 24;
    }
}