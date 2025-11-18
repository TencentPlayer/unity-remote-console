using System;
using System.Collections.Generic;
using RConsole.Common;
using UnityEngine;

namespace RConsole.Editor
{
    // 存储接收到的日志，供 EditorWindow 使用
    public class RConsoleViewModel
    {
        // internal class Item
        // {
        //     public long ts;
        //     public LogType level;
        //     public string tag;
        //     public string message;
        //     public string stack;
        //     public string deviceId;
        //     public string appName;
        //     public string sessionId;
        //     public int threadId;
        // }

        /// <summary>
        /// 服务是否已启动
        /// </summary>
        private bool _isServerStarted = false;
        public bool IsServerStarted => _isServerStarted;


        /// <summary>
        /// 已连接的客户端列表
        /// </summary>
        private List<ClientInfoModel> _connectedClients = new List<ClientInfoModel>();
        public IReadOnlyList<ClientInfoModel> ConnectedClients => _connectedClients;
        public void AddConnectedClient(ClientInfoModel client)
        {
            _connectedClients.Add(client);
            Emit();
        }
        public void RemoveConnectedClient(ClientInfoModel client)
        {
            _connectedClients.Remove(client);
            Emit();
        }
        public void ClearConnectedClients()
        {
            _connectedClients.Clear();
            Emit();
        }


        /// <summary>
        /// 当前筛选的客户端
        /// </summary>
        private ClientInfoModel _filterClientInfoModel = null;
        public ClientInfoModel FilterClientInfoModel => _filterClientInfoModel;
        public void SetFilterClientInfoModel(ClientInfoModel client)
        {
            _filterClientInfoModel = client;
            Emit();
        }   
        


        private readonly List<LogRecordModel> _items = new List<LogRecordModel>();
        private readonly object _lock = new object();

        public Action<RConsoleViewModel> OnModelChanged = null;

        public void Add(LogRecordModel m)
        {
            lock (_lock)
            {
                _items.Add(m);
                if (_items.Count > 5000)
                {
                    _items.RemoveRange(0, _items.Count - 5000);
                }
            }
            Emit();
        }

        public IReadOnlyList<LogRecordModel> Snapshot()
        {
            lock (_lock)
            {
                return _items.ToArray();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _items.Clear();
            }
            Emit();
        }

        public void SetServerStarted(bool started)
        {
            _isServerStarted = started;
            Emit();
        }

        public void ServerDisconnected()
        {
            SetServerStarted(false);
            ClearConnectedClients();
            SetFilterClientInfoModel(null);
        }

        private void Emit()
        {
            OnModelChanged?.Invoke(this);
        }
    }
}