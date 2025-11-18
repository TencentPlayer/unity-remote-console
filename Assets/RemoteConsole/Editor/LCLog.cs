using System;
using RConsole.Common;
using UnityEditor;
using UnityEngine;

namespace RConsole.Editor
{

    // 简易日志入口：将日志写入 RConsoleMainWindow 所用的存储，并触发刷新显示
    public class LCLog
    {

        private static RConsoleViewModel _model;

        public static RConsoleViewModel ViewModel => _model ??= new RConsoleViewModel();

        public static void Log(string message)
        {
            Write(LogType.Log, message);
        }

        public static void LogWarning(string message)
        {
            Write(LogType.Warning, message);
        }

        public static void LogError(string message)
        {
            Write(LogType.Error, message);
        }

        public static void LogAssertion(string message)
        {
            Write(LogType.Assert, message);
        }

        public static void LogException(Exception exception)
        {
            Write(LogType.Exception, exception.ToString());
        }

        private static void Write(LogType level, string message, string tag = "RCLog")
        {
            var model = new LogRecordModel
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level = (LogType)(int)level,
                tag = tag,
                message = message,
                stackTrace = Environment.StackTrace,
                threadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };

            ViewModel.Add(model);
        }
    }
}