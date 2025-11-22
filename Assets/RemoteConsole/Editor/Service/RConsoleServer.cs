using System;
using System.Collections.Generic;
using System.Net;
using RConsole.Common;
using WebSocketSharp.Server;

namespace RConsole.Editor
{
    // 使用 System.Net.WebSockets + HttpListener 的内置 WebSocket 服务实现
    public class RConsoleServer
    {
        public int Port { get; set; } = 13337;
        public string Path { get; set; } = "/remote-console";
        public bool IsStarted { get; private set; }

        public string Status => IsStarted ? $"Running ws://0.0.0.0:{Port}{Path}" : "Stopped";

        private WebSocketServer _wsServer;

        public delegate Envelope BroadcastHandler(RConsoleConnection connection, Envelope env);

        public static Dictionary<string, List<RConsoleServer.BroadcastHandler>> _broadcastHandlers =
            new Dictionary<string, List<RConsoleServer.BroadcastHandler>>();

        public static Dictionary<string, RConsoleConnection> Connections = new Dictionary<string, RConsoleConnection>();


        public void Start()
        {
            if (IsStarted)
            {
                LCLog.LogWarning("服务器已启动，无需多次启动");
                return;
            }

            try
            {
                // 确保主线程调度器已初始化
                MainThreadDispatcher.Initialize();
                // 使用 WebSocketSharp 启动服务器
                _wsServer = new WebSocketServer(IPAddress.Any, Port);
                var route = string.IsNullOrEmpty(Path) ? "/" : (Path.StartsWith("/") ? Path : "/" + Path);
                _wsServer.AddWebSocketService<RConsoleConnection>(route);
                _wsServer.Start();

                IsStarted = true;
                RConsoleCtrl.Instance.SetServerStarted(IsStarted);
                LCLog.Log($"服务启动成功，请在输入下列地址连接：");
                var ips = NETUtils.GetIPv4Addresses();
                for (int i = 0; i < ips.Length; i++)
                {
                    LCLog.Log($"{ips[i]}");
                }
            }
            catch (Exception ex)
            {
                LCLog.LogError($"RemoteConsole Server start failed: {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (!IsStarted) return;

            try
            {
                try
                {
                    _wsServer?.Stop();
                }
                catch
                {
                }

                _wsServer = null;
                RConsoleCtrl.Instance.ServerDisconnected();
            }
            catch (Exception ex)
            {
                LCLog.LogWarning($"RemoteConsole Server stop error: {ex.Message}");
            }
            finally
            {
                IsStarted = false;
                RConsoleCtrl.Instance.SetServerStarted(IsStarted);
                LCLog.Log("RemoteConsole Server stopped");
            }
        }


        public static void On(EnvelopeKind kind, byte subCommandId, BroadcastHandler handler)
        {
            var key = $"{kind}_{subCommandId}";
            if (!_broadcastHandlers.ContainsKey(key))
            {
                _broadcastHandlers[key] = new List<BroadcastHandler>();
            }

            _broadcastHandlers[key].Add(handler);
        }

        public static void Off(EnvelopeKind kind, byte subCommandId, BroadcastHandler handler = null)
        {
            var key = $"{kind}_{subCommandId}";
            if (handler != null)
            {
                var handlers = _broadcastHandlers[key];
                if (handlers != null)
                {
                    // 找到移除
                    for (int i = handlers.Count - 1; i >= 0; i--)
                    {
                        if (handlers[i] == handler)
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

        public static void Emit(RConsoleConnection connection, Envelope env)
        {
            var key = $"{env.Kind}_{env.SubCommandId}";
            if (_broadcastHandlers.ContainsKey(key))
            {
                var handlers = _broadcastHandlers[key];
                if (handlers != null)
                {
                    foreach (var handler in handlers)
                    {
                        var data = handler?.Invoke(connection, env);
                        if (data != null)
                        {
                            connection.SendEnvelop(data);
                        }
                    }
                }
            }
        }
    }
}