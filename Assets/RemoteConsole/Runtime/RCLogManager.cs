

using System;
using RConsole.Common;
using UnityEngine;

namespace RConsole.Runtime
{
    // 单例日志管理器：负责连接远端控制台与转发 Unity 日志
    public class RCLogManager
    {
        private static RCLogManager _instance;
        public static RCLogManager Instance => _instance ??= new RCLogManager();

        private RConsoleClient _client;
        private bool _capturingLogs;
        public bool IsCapturingLogs => _capturingLogs;

        private RCLogManager()
        {
            // 允许默认配置，必要时由 Init 指定
        }

        /// <summary>
        /// 初始化并连接到服务器。
        /// </summary>
        public async void Connect(string ip, int port = 13337, string path = "/remote-console")
        {
            // 释放旧连接
            _client?.Stop();

            _client = new RConsoleClient
            {
                EditorHost = ip,
                EditorPort = port,
                EditorPath = path
            };
            Debug.Log($"RCLogManager Connect to {ip}:{port}{path}");
            // 由 RCLogManager 自行决定是否捕获 Unity 日志，因此此处不启用捕获
            await _client.StartAsync(captureUnityLogs: false);
        }

        public async void Disconnect()
        {
            try
            {
                _client?.Stop();
            }
            catch { }
            finally
            {
                _client = null;
            }
        }

        /// <summary>
        /// 是否已连接到服务器。
        /// </summary>
        public bool IsConnected => _client != null && _client.IsConnected;

        /// <summary>
        /// 开启 Unity 日志转发（通过 RCLogManager 捕获并发送）。
        /// </summary>
        public void ForwardingUnityLog()
        {
            if (_capturingLogs) return;
            Application.logMessageReceivedThreaded += OnUnityLogMessage;
            _capturingLogs = true;
        }

        /// <summary>
        /// 关闭 Unity 日志转发。
        /// </summary>
        public void StopForwardingUnityLog()
        {
            if (!_capturingLogs) return;
            Application.logMessageReceivedThreaded -= OnUnityLogMessage;
            _capturingLogs = false;
        }

        /// <summary>
        /// 主动停止并释放连接。
        /// </summary>
        public void Stop()
        {
            try
            {
                StopForwardingUnityLog();
                _client?.Stop();
            }
            catch { }
            finally
            {
                _client = null;
            }
        }

        /// <summary>
        /// 处理 Unity 日志（线程回调）。
        /// </summary>
        private void OnUnityLogMessage(string logString, string stackTrace, LogType type)
        {
            var lr = new LogRecordModel
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level = type,
                tag = "Unity",
                message = logString,
                stackTrace = stackTrace ?? string.Empty,
                threadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };
            SendLog(lr);
        }

        /// <summary>
        /// 发送业务日志到服务器。
        /// </summary>
        public void SendLog(LogRecordModel logRecord)
        {
            if (logRecord == null) return;
            try
            {
                _client?.EnqueueEnvelope(EnvelopeModel.FromLog(logRecord));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RCLogManager send failed: {ex.Message}");
            }
        }
    }
}