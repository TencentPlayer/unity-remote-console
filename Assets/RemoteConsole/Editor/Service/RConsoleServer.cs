using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RConsole.Common;
using UnityEngine;
using WebSocketSharp;
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

        // 在部分 macOS/Mono 环境下，HttpListener 的 IsWebSocketRequest 可能不可靠
        // 开启该选项后，将基于 Upgrade/Connection 头部尝试强制握手
        public bool ForceAcceptOnUpgradeHeader { get; set; } = true;

        private WebSocketServer _wsServer;

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
                _wsServer.AddWebSocketService<RConsoleBehaviour>(route);
                _wsServer.Start();

                IsStarted = true;
                LCLog.ViewModel.SetServerStarted(IsStarted);
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
                try { _wsServer?.Stop(); } catch { }
                _wsServer = null;
                LCLog.ViewModel.ServerDisconnected();
            }
            catch (Exception ex)
            {
                LCLog.LogWarning($"RemoteConsole Server stop error: {ex.Message}");
            }
            finally
            {
                IsStarted = false;
                LCLog.ViewModel.SetServerStarted(IsStarted);
                LCLog.Log("RemoteConsole Server stopped");
            }
        }

        // 逻辑由 RConsoleBehaviour 管理
    }
}