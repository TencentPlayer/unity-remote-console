using System;
using RConsole.Common;
using UnityEngine;

namespace RConsole.Runtime
{

    // 简易日志入口：将日志写入 RConsoleMainWindow 所用的存储，并触发刷新显示
    public static class RCLog
    {

        public static void Log(string message)
        {
            Send(LogType.Log, message);
        }

        public static void LogWarning(string message)
        {
            Send(LogType.Warning, message);
        }

        public static void LogError(string message)
        {
            Send(LogType.Error, message);
        }

        public static void LogAssertion(string message)
        {
            Send(LogType.Assert, message);
        }

        public static void LogException(Exception exception)
        {
            Send(LogType.Exception, exception.ToString());
        }

        private static void Send(LogType level, string message, string tag = "RCLog")
        {
            var model = new LogModel
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level = (LogType)(int)level,
                tag = tag,
                message = message,
                stackTrace = Environment.StackTrace,
                threadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };

            RConsoleCtrl.Instance.SendLog(model);
        }
    }
}