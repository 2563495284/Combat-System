using System;

#if UNITY_2018_4_OR_NEWER
using UnityEngine;
#endif

namespace BTree
{
#nullable enable
    /// <summary>
    /// 用于行为树记录日志
    /// </summary>
    public static class TaskLogger
    {
        public static void Info(string format, params object?[] args)
        {
#if UNITY_2018_4_OR_NEWER
            Debug.LogFormat(format, args);
#else
        Console.WriteLine(format, args);
#endif
        }

        public static void Info(Exception? ex, string format, params object?[] args)
        {
#if UNITY_2018_4_OR_NEWER
            Debug.LogFormat(format, args);
            if (ex != null)
            {
                Debug.LogException(ex);
            }
#else
        Console.WriteLine(format, args);
        if (ex != null) {
            Console.WriteLine(ex);
        }
#endif
        }

        public static void Warning(string format, params object?[] args)
        {
#if UNITY_2018_4_OR_NEWER
            Debug.LogWarningFormat(format, args);
#else
        Console.WriteLine(format, args);
#endif
        }

        public static void Warning(Exception? ex, string format, params object?[] args)
        {
#if UNITY_2018_4_OR_NEWER
            Debug.LogWarningFormat(format, args);
            if (ex != null)
            {
                Debug.LogException(ex);
            }
#else
        Console.WriteLine(format, args);
        if (ex != null) {
            Console.WriteLine(ex);
        }
#endif
        }
    }
}