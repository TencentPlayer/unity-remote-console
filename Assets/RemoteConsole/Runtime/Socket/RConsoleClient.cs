using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using RConsole.Common;
using System.Text;
using UnityEngine;

namespace RConsole.Runtime
{
    // 纯逻辑客户端：不依赖 MonoBehaviour，可在任意代码中手动创建/启动/停止
    public class RConsoleClient : IDisposable
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 13337;
        public string Path { get; set; } = "/remote-console";

        private ClientWebSocket _ws;
        private readonly ConcurrentQueue<ArraySegment<byte>> _sendQueue = new ConcurrentQueue<ArraySegment<byte>>();
        private Task _sendLoopTask;
        private Task _receiveLoopTask;
        private string _sessionId;

        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

        public event Action<Envelope> ServerEnvelopeReceived;
        public event Action<string> ServerTextReceived;

        private readonly Dictionary<int, Action<Envelope>> _requestHandlers = new Dictionary<int, Action<Envelope>>();

        public delegate Envelope BroadcastHandler(Envelope env);

        private readonly Dictionary<string, List<BroadcastHandler>> _broadcastHandlers =
            new Dictionary<string, List<BroadcastHandler>>();

        public async Task Connect()
        {
            var errMsg = await ConnectAsync();
            if (!string.IsNullOrEmpty(errMsg))
            {
                Debug.LogWarning("连接失败，错误：" + errMsg);
                return;
            }

            HandlerFactory.OnEnable();

            SendHandshake();
            // 启动发送循环
            _sendLoopTask = Task.Run(SendLoopAsync);
            _receiveLoopTask = Task.Run(ReceiveLoopAsync);
        }

        private void SendHandshake()
        {
            // 发送握手
            _sessionId = Guid.NewGuid().ToString();
            Debug.Log($"会话ID：{_sessionId}");
            var hs = new ClientModel
            {
                deviceName = SystemInfo.deviceName,
                deviceModel = SystemInfo.deviceModel,
                deviceId = SystemInfo.deviceUniqueIdentifier,
                platform = Application.platform.ToString(),
                appName = Application.productName,
                appVersion = Application.version,
                sessionId = _sessionId
            };
            Send(new Envelope(EnvelopeKind.C2SHandshake, (byte)SubHandshake.Handshake, hs));
        }

        public void Disconnect()
        {
            try
            {
                _ws?.Dispose();
                HandlerFactory.OnDisable();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[客户端] 断开连接时发生异常: {ex.Message}");
            }
            finally
            {
                _sendLoopTask = null;
                _receiveLoopTask = null;
                _ws = null;
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        private async Task<string> ConnectAsync()
        {
            try
            {
                HandlerFactory.Initialize();
                _ws = new ClientWebSocket();
                var uri = new Uri($"ws://{Host}:{Port}{Path}");
                Debug.Log($"[客户端]发起连接：{uri}");
                await _ws.ConnectAsync(uri, CancellationToken.None);
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[客户端] 连接失败: {ex.Message}");
                return ex.Message;
            }
        }

        /// <summary>
        /// 发送消息队列
        /// </summary>
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
                    Debug.LogWarning($"[客户端] 发送错误: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// 接收消息队列
        /// </summary>
        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[8192];
            while (IsConnected)
            {
                try
                {
                    if (_ws.State != WebSocketState.Open)
                    {
                        await Task.Delay(500);
                        continue;
                    }

                    var ms = new MemoryStream();
                    WebSocketMessageType msgType = WebSocketMessageType.Binary;
                    while (true)
                    {
                        var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await Task.Delay(200);
                            break;
                        }

                        msgType = result.MessageType;
                        if (result.Count > 0)
                        {
                            ms.Write(buffer, 0, result.Count);
                        }

                        if (result.EndOfMessage) break;
                    }

                    if (ms.Length == 0) continue;
                    var data = ms.ToArray();
                    if (msgType == WebSocketMessageType.Binary)
                    {
                        MainThreadDispatcher.Enqueue(() =>
                        {
#if RC_USE_GOOGLE_PROTOBUF
                            Envelope env = null;
#else
                            var env = new Envelope(data);
#endif
                            HandleServerEnvelope(env);
                            ServerEnvelopeReceived?.Invoke(env);
                        });
                    }
                    else if (msgType == WebSocketMessageType.Text)
                    {
                        var text = Encoding.UTF8.GetString(data);
                        MainThreadDispatcher.Enqueue(() => { ServerTextReceived?.Invoke(text); });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[客户端] 接收错误: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }


        private void Send(Envelope env)
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

        /// <summary>
        /// 处理服务器发送的 Envelope。
        /// </summary>
        /// <param name="env">服务器发送的 Envelope</param>
        private void HandleServerEnvelope(Envelope env)
        {
            var id = env.Id;
            if (_requestHandlers.TryGetValue(id, out var handler))
            {
                handler?.Invoke(env);
                _requestHandlers.Remove(id);
                return;
            }

            var key = $"{env.Kind}_{env.SubCommandId}";
            if (_broadcastHandlers.TryGetValue(key, out var handlers))
            {
                foreach (var h in handlers)
                {
                    var data = h?.Invoke(env);
                    if (data != null)
                    {
                        Send(data);
                    }
                }
            }

            // var handler = HandlerFactory.CreateHandler(env.Kind);
            // var resp = handler?.Handle(env.SubCommandId, env.Model);
            // if (resp == null) return;
            // Send(resp);
        }

        public void Reqeust(EnvelopeKind kind, byte subCommandId, IBinaryModelBase data,
            Action<Envelope> handler = null)
        {
            var env = new Envelope(kind, subCommandId, data);
            if (handler != null)
            {
                _requestHandlers[env.Id] = handler;
            }

            Send(env);
        }

        public void On(EnvelopeKind kind, byte subCommandId, BroadcastHandler handler)
        {
            var key = $"{kind}_{subCommandId}";
            if (!_broadcastHandlers.ContainsKey(key))
            {
                _broadcastHandlers[key] = new List<BroadcastHandler>();
            }

            _broadcastHandlers[key].Add(handler);
        }

        public void Off(EnvelopeKind kind, byte subCommandId, BroadcastHandler complete = null)
        {
            var key = $"{kind}_{subCommandId}";
            if (complete != null)
            {
                var handlers = _broadcastHandlers[key];
                if (handlers != null)
                {
                    // 找到移除
                    for (int i = handlers.Count - 1; i >= 0; i--)
                    {
                        if (handlers[i] == complete)
                        {
                            handlers.RemoveAt(i);
                        }
                    }
                }
            }
            else
            {
                _broadcastHandlers.Remove(key);
            }
        }

        private void Emit(Envelope env)
        {
            var key = $"{env.Kind}_{env.SubCommandId}";
            if (_broadcastHandlers.ContainsKey(key))
            {
                var handlers = _broadcastHandlers[key];
                if (handlers != null)
                {
                    foreach (var handler in handlers)
                    {
                        handler?.Invoke(env);
                    }
                }
            }
        }
    }
}