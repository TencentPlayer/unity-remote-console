using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using RConsole.Common;
using UnityEngine;

namespace RConsole.Runtime
{
    // 纯逻辑客户端：不依赖 MonoBehaviour，可在任意代码中手动创建/启动/停止
    public class RConsoleClient : IDisposable
    {
        public string EditorHost { get; set; } = "127.0.0.1";
        public int EditorPort { get; set; } = 13337;
        public string EditorPath { get; set; } = "/remote-console";

        private ClientWebSocket _ws;
        private readonly ConcurrentQueue<ArraySegment<byte>> _sendQueue = new ConcurrentQueue<ArraySegment<byte>>();
        private Task _sendLoopTask;
        private string _sessionId;
        private bool _capturingLogs;

        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        public async Task StartAsync(bool captureUnityLogs = true)
        {
            await ConnectAsync();
            if (captureUnityLogs && !_capturingLogs)
            {
                Application.logMessageReceivedThreaded += OnLogMessage;
                _capturingLogs = true;
            }
        }

        public void Stop()
        {
            if (_capturingLogs)
            {
                Application.logMessageReceivedThreaded -= OnLogMessage;
                _capturingLogs = false;
            }

            try
            {
                _ws?.Dispose();
            }
            catch { }
            finally
            {
                _sendLoopTask = null;
                _ws = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task ConnectAsync()
        {
            try
            {
                _ws = new ClientWebSocket();
                var uri = new Uri($"ws://{EditorHost}:{EditorPort}{EditorPath}");
                Debug.Log($"客户端发起连接：{uri}");
                await _ws.ConnectAsync(uri, CancellationToken.None);

                // 发送握手
                _sessionId = Guid.NewGuid().ToString();
                Debug.Log($"会话ID：{_sessionId}");
                var hs = new ClientInfoModel
                {
                    deviceName = SystemInfo.deviceName,
                    deviceModel = SystemInfo.deviceModel,
                    deviceId = SystemInfo.deviceUniqueIdentifier,
                    platform = Application.platform.ToString(),
                    appName = Application.productName,
                    appVersion = Application.version,
                    sessionId = _sessionId
                };
                EnqueueEnvelope(EnvelopeModel.FromHandshake(hs));

                // 启动发送循环
                _sendLoopTask = Task.Run(SendLoopAsync);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"RemoteConsoleClient connect failed: {ex.Message}");
            }
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            var lr = new LogRecordModel
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                level = type,
                tag = "Unity",
                message = condition,
                stackTrace = stackTrace,
                threadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };
            EnqueueEnvelope(EnvelopeModel.FromLog(lr));
        }

        public void EnqueueEnvelope(EnvelopeModel env)
        {
#if RC_USE_GOOGLE_PROTOBUF
            // TODO: 使用 Google.Protobuf 生成的 Envelope 类型进行序列化
            // var pbEnv = ConvertToProto(env);
            // var bytes = pbEnv.ToByteArray();
            // _sendQueue.Enqueue(new ArraySegment<byte>(bytes));
#else
            var bytes = env.ToBinary();
            _sendQueue.Enqueue(new ArraySegment<byte>(bytes));
#endif
        }

        private async Task SendLoopAsync()
        {
            while (IsConnected)
            {
                try
                {
                    if (_ws.State != WebSocketState.Open)
                    {
                        await Task.Delay(500);
                        continue;
                    }

                    if (_sendQueue.TryDequeue(out var seg))
                    {
                        await _ws.SendAsync(seg, WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                    else
                    {
                        await Task.Delay(10);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"RemoteConsoleClient send error: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }
    }
}