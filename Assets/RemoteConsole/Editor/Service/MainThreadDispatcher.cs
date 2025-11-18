using System;
using System.Collections.Generic;
using UnityEditor;

namespace RConsole.Editor
{
    // 简单的主线程调度器：在 Editor 主循环中执行排队的操作
    internal static class MainThreadDispatcher
    {
        private static readonly object _lock = new object();
        private static readonly Queue<Action> _queue = new Queue<Action>();
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        public static void Enqueue(Action action)
        {
            if (action == null) return;
            Initialize();
            lock (_lock)
            {
                _queue.Enqueue(action);
            }
        }

        private static void Update()
        {
            // 每帧尽量清空队列，避免积压
            while (true)
            {
                Action next = null;
                lock (_lock)
                {
                    if (_queue.Count > 0)
                    {
                        next = _queue.Dequeue();
                    }
                }
                if (next == null) break;
                try
                {
                    next();
                }
                catch (Exception)
                {
                    // 静默异常，避免中断 Editor 循环
                }
            }
        }
    }
}