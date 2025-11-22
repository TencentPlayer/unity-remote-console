using System;
using System.Collections.Generic;
using RConsole.Common;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace RConsole.Editor
{
    /// <summary>
    /// 控制台控制器
    /// </summary>
    public class RConsoleCtrl
    {
        private static RConsoleCtrl _instance;
        public static RConsoleCtrl Instance => _instance ??= new RConsoleCtrl();

        private readonly RConsoleServer _server;

        private RConsoleViewModel _model;
        public RConsoleViewModel ViewModel => _model ??= new RConsoleViewModel();


        private Dictionary<string, ClientModel> _connections = new Dictionary<string, ClientModel>();

        private RConsoleCtrl()
        {
            _server = new RConsoleServer();
        }

        public void OnEnable()
        {
            RConsoleServer.On(EnvelopeKind.C2SLog, (byte)SubLog.Log, OnLogReceived);
            RConsoleServer.On(EnvelopeKind.C2SHandshake, (byte)SubHandshake.Handshake, OnHandshakeReceived);
        }

        public void OnDisable()
        {
            RConsoleServer.Off(EnvelopeKind.C2SLog, (byte)SubLog.Log, OnLogReceived);
            RConsoleServer.Off(EnvelopeKind.C2SHandshake, (byte)SubHandshake.Handshake, OnHandshakeReceived);
        }

        public void Connect()
        {
            _server.Start();
        }

        public void Disconnect()
        {
            _server.Stop();
        }

        #region 网络相关 - 主动请求相关数据

        /// <summary>
        /// 请求查看当前连接的客户端信息
        /// </summary>
        public void FetchLookin()
        {
            var connection = GetSelectConnection();
            var body = new LookInViewModel { Path = "/" };
            connection?.Reqeust(EnvelopeKind.S2CLookin, (byte)SubLookIn.LookIn, body, envelope =>
            {
                var lookInRespModel = envelope.Model as LookInViewModel;
                if (lookInRespModel == null) return;
                LCLog.Log("服务端请求 Lookin 数据成功返回");
                BringLookInToEditor(lookInRespModel);
            });
        }

        public void FetchDirectory(FileModel model)
        {
            var connection = GetSelectConnection();
            connection?.Reqeust(EnvelopeKind.S2CFile, (byte)SubFile.FetchDirectory, model, envelope =>
            {
                var resp = envelope.Model as FileModel;
                if (resp == null) return;
                UpdateFileBrowser(resp);
            });
        }


        public void RequestFileMD5(FileModel model)
        {
            var connection = GetSelectConnection();
            connection?.Reqeust(EnvelopeKind.S2CFile, (byte)SubFile.MD5, model, envelope =>
            {
                var resp = envelope.Model as FileModel;
                if (resp == null) return;
                OnFileMD5Changed?.Invoke(resp);
            });
        }

        public RConsoleConnection GetSelectConnection()
        {
            if (!_server.IsStarted)
            {
                LCLog.LogWarning("服务未启动");
                return null;
            }

            var selectModel = ViewModel.FilterClientModel;
            if (selectModel == null)
            {
                LCLog.LogWarning("未选择客户端");
                return null;
            }

            if (!RConsoleServer.Connections.TryGetValue(selectModel.connectID, out var connection))
            {
                LCLog.LogWarning("未找到选择的客户端连接");
                return null;
            }

            return connection;
        }

        #endregion

        #region 网络相关 - 被动监听回调

        public Envelope OnLogReceived(RConsoleConnection connection, Envelope envelope)
        {
            var logModel = envelope.Model as LogModel;
            if (logModel == null) return null;
            var clientModel = connection.ClientModel;
            if (clientModel != null) logModel.clientModel = clientModel;
            Log(logModel);
            return null;
        }

        public Envelope OnHandshakeReceived(RConsoleConnection connection, Envelope envelope)
        {
            var model = envelope.Model as ClientModel;
            AddConnectedClient(model);
            return null;
        }

        #endregion

        public void Log(LogType level, string message, string tag = "RCLog")
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
            Log(model);
        }

        public void Log(LogModel log)
        {
            ViewModel.Add(log);
        }

        public void AddConnectedClient(ClientModel model)
        {
            _connections[model.connectID] = model;
            _model.AddConnectedClient(model);
        }

        public void RemoveConnectedClient(ClientModel model)
        {
            _connections.Remove(model.connectID);
            _model.RemoveConnectedClient(model);
        }

        public void SetServerStarted(bool started)
        {
            _model.SetServerStarted(started);
            if (started)
            {
                OnEnable();
            }
            else
            {
                OnDisable();
            }
        }

        public void ServerDisconnected()
        {
            _model.ServerDisconnected();
        }

        public void SetFilterClientInfoModel(ClientModel client)
        {
            _model.SetFilterClientInfoModel(client);
        }

        public void ClearLog()
        {
            _model.Clear();
        }

        #region Lookin相关

        /// <summary>
        /// .bring-lookin-to-editor
        /// </summary>
        public void BringLookInToEditor(LookInViewModel lookInViewModel)
        {
            if (lookInViewModel == null) return;
            var scene = SceneManager.GetActiveScene();
            GameObject lookinRoot = null;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "LookIn")
                {
                    lookinRoot = roots[i];
                    break;
                }
            }

            if (lookinRoot != null)
            {
                Undo.DestroyObjectImmediate(lookinRoot);
            }

            var connection = GetSelectConnection();
            lookinRoot = new GameObject($"LookIn({connection.ClientModel.deviceName})");
            Undo.RegisterCreatedObjectUndo(lookinRoot, "Create LookIn Root");

            BuildEditorNodes(lookinRoot.transform, lookInViewModel);
            EditorSceneManager.MarkSceneDirty(scene);
            LCLog.Log($"当前设备: {connection.ClientModel.deviceName}，已将 Lookin 视图添加到场景中");
        }

        private void BuildEditorNodes(Transform parent, LookInViewModel model)
        {
            var go = new GameObject(string.IsNullOrEmpty(model.Name) ? "Node" : model.Name);
            go.SetActive(model.IsActive);
            Undo.RegisterCreatedObjectUndo(go, "Create LookIn Node");
            var t = go.transform;
            t.SetParent(parent, false);
            var children = model.Children;
            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    BuildEditorNodes(t, children[i]);
                }
            }
        }

        #endregion

        private FileModel _fileRoot;
        public FileModel FileRoot => _fileRoot;
        public Action<FileModel> OnFileBrowserChanged;
        public Action<FileModel> OnFileMD5Changed;

        public void UpdateFileBrowser(FileModel resp)
        {
            if (resp == null) return;
            if (_fileRoot == null)
            {
                _fileRoot = resp;
            }
            else
            {
                var isRootResp = string.Equals(resp.Path, _fileRoot.Path, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(resp.Path, _fileRoot.RootPath,
                                     StringComparison.OrdinalIgnoreCase);
                if (isRootResp)
                {
                    _fileRoot = resp;
                }
                else
                {
                    var target = FindNodeByPath(_fileRoot, resp.Path);
                    if (target != null)
                    {
                        target.Name = resp.Name;
                        target.IsDirectory = resp.IsDirectory;
                        target.Length = resp.Length;
                        target.LastWriteTime = resp.LastWriteTime;
                        target.Children = resp.Children ?? new List<FileModel>();
                    }
                }
            }

            OnFileBrowserChanged?.Invoke(_fileRoot);
        }

        private FileModel FindNodeByPath(FileModel node, string path)
        {
            if (node == null) return null;
            if (string.Equals(node.Path, path, StringComparison.OrdinalIgnoreCase)) return node;
            if (node.Children == null) return null;
            for (int i = 0; i < node.Children.Count; i++)
            {
                var found = FindNodeByPath(node.Children[i], path);
                if (found != null) return found;
            }

            return null;
        }
    }
}