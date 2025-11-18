using System;
using System.IO;
using RConsole.Common;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RConsole.Editor
{
    // 独立的 WebSocket 服务实现，直接在行为类中处理逻辑
    internal class RConsoleBehaviour : WebSocketBehavior
    {
        public static RConsoleServer Server { get; private set; }

        private readonly object _lock = new object();
        private ClientInfoModel _clientInfo = new ClientInfoModel();

        protected override void OnOpen()
        {
            var id = ID;
            var remoteStr = UserEndPoint.Address.ToString();
            MainThreadDispatcher.Enqueue(() =>
            {
                lock (_lock)
                {
                    _clientInfo = new ClientInfoModel
                    {
                        connectID = id,
                        address = remoteStr,
                        connectedAt = DateTime.UtcNow
                    };
                }
                LCLog.Log($"[服务]客户端连接：{remoteStr} (id={id})");
            });
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var id = ID;
            try
            {
                if (e.IsBinary)
                {
                    var data = e.RawData; // 捕获快照
                    MainThreadDispatcher.Enqueue(() => ProcessEnvelopeBinary(id, data));
                }
                else if (e.IsText)
                {
                    var json = e.Data;
                    MainThreadDispatcher.Enqueue(() => ProcessEnvelopeJson(id, json));
                }
            }
            catch (Exception ex)
            {
                MainThreadDispatcher.Enqueue(() => LCLog.LogWarning($"[服务]客户端消息处理错误：{ex.Message}"));
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            var id = ID;
            MainThreadDispatcher.Enqueue(() =>
            {
                LCLog.ViewModel.RemoveConnectedClient(_clientInfo);
                _clientInfo = null;
                LCLog.Log($"[服务]客户端断开：id={id} ({e.Code})");
            });
        }

        private void ProcessEnvelopeJson(string sessionId, string json)
        {
            try
            {
                var env = JsonUtility.FromJson<EnvelopeModel>(json);
                if (env == null) return;
                HandleEnvelope(sessionId, env);
            }
            catch (Exception ex)
            {
                LCLog.LogError($"[服务]客户端 JSON 解析错误：{ex.Message}");
            }
        }

        private void ProcessEnvelopeBinary(string sessionId, byte[] data)
        {
            try
            {
                EnvelopeModel env = null;

#if RC_USE_GOOGLE_PROTOBUF
                // 使用 Google.Protobuf 生成的类型进行解析（需提供 csharp 生成文件与命名空间）
                // 假设命名空间为 RemoteConsoleProto，类型为 Envelope
                // var pbEnv = RemoteConsoleProto.Envelope.Parser.ParseFrom(data);
                // env = ConvertFromProto(pbEnv);
                R.LogWarning("RC_USE_GOOGLE_PROTOBUF 启用后请提供 ConvertFromProto 与生成的 C# 类型。");
#else
                env = EnvelopeModel.FromData(data);
#endif

                if (env != null)
                {
                    HandleEnvelope(sessionId, env);
                }
            }
            catch (EndOfStreamException)
            {
                LCLog.LogError($"[服务]客户端二进制解析错误：unexpected end of stream");
            }
            catch (Exception ex)
            {
                LCLog.LogError($"[服务]客户端二进制解析错误：{ex.Message}");
            }
        }

        private void HandleEnvelope(string sessionId, EnvelopeModel env)
        {
            if (env.clientInfo != null)
            {
                LCLog.Log(
                    $"[服务]客户端握手：{env.clientInfo.deviceId} {env.clientInfo.platform} {env.clientInfo.appName} {env.clientInfo.appVersion}");
                if (_clientInfo != null)
                {
                    env.clientInfo.address = _clientInfo.address;
                    env.clientInfo.connectedAt = _clientInfo.connectedAt;
                    env.clientInfo.connectID = _clientInfo.connectID;
                    _clientInfo = env.clientInfo;
                }
                LCLog.ViewModel.AddConnectedClient(env.clientInfo);
                LCLog.Log($"[服务]握手成功：{env.clientInfo.deviceName} ");
            }

            if (env.log != null)
            {
                if (_clientInfo != null) env.log.clientInfoModel = _clientInfo;
                LCLog.ViewModel.Add(env.log);
            }
        }
    }
}