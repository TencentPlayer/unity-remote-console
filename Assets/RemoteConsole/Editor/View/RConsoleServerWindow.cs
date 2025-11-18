
using RConsole.Common;
using UnityEditor;
using UnityEngine;

namespace RConsole.Editor
{
    // 贴附在按钮下方的服务控制下拉窗（使用 ShowAsDropDown，位置更稳定）
    public class RConsoleServerWindow : EditorWindow
    {
        private RConsoleMainWindow _owner;
        private static RConsoleServer _server;
        private int _port;
        private string[] _ips;
        private Vector2 _scroll;
        private ClientInfoModel _selectedClient;

        private const string PrefKeyPort = "RemoteConsole.Port";

        public static void Open(Rect anchorGuiRect, RConsoleMainWindow owner)
        {
            var win = CreateInstance<RConsoleServerWindow>();
            win._owner = owner;
            // 转为屏幕坐标以保证位置正确贴附按钮
            var screenRect = GUIUtility.GUIToScreenRect(anchorGuiRect);
            win.ShowAsDropDown(screenRect, new Vector2(420, 260));
        }

        private void OnEnable()
        {
            _ips = NETUtils.GetIPv4Addresses();
            if (_server == null) _server = new RConsoleServer();
            _port = EditorPrefs.GetInt(PrefKeyPort, _server.Port);
            _selectedClient = LCLog.ViewModel.FilterClientInfoModel;
        }

        private void OnGUI()
        {
            // 顶部展示已连接客户端
            EditorGUILayout.LabelField("已连接客户端", EditorStyles.boldLabel);
            var clients = LCLog.ViewModel.ConnectedClients;
            if (clients.Count == 0)
            {
                EditorGUILayout.LabelField("无客户端连接", EditorStyles.miniLabel);
            }
            else
            {
                // 清除筛选按钮
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_selectedClient == null))
                {
                    if (GUILayout.Button("显示全部日志", EditorStyles.miniButton, GUILayout.Width(100)))
                    {
                        _selectedClient = null;
                        LCLog.ViewModel.SetFilterClientInfoModel(null);
                    }
                }
                EditorGUILayout.EndHorizontal();

                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(100));
                var i = 0;
                foreach (var c in clients)
                {
                    EditorGUILayout.BeginVertical("box");
                    var isSel = _selectedClient != null && _selectedClient.deviceId == c.deviceId;
                    EditorGUILayout.LabelField($"#{++i} {c.deviceName} ({c.deviceModel})", EditorStyles.label);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"设备: {c.deviceId}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (isSel)
                    {
                        GUILayout.Label("当前筛选中", EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                    else
                    {
                        if (GUILayout.Button("只看此客户端", EditorStyles.miniButton, GUILayout.Width(100)))
                        {
                            _selectedClient = c;
                            LCLog.ViewModel.SetFilterClientInfoModel(c);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }

            // 分割线
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(4);

            // 显示服务器当前状态（运行中/已停止 + 地址）
            var statusBaseStyle = EditorStyles.miniLabel ?? EditorStyles.label ?? new GUIStyle();
            var statusStyle = new GUIStyle(statusBaseStyle);
            statusStyle.wordWrap = true;
            statusStyle.normal.textColor = _server.IsStarted ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
            EditorGUILayout.LabelField($"服务器状态: {_server.Status}", statusStyle);

            // 下方为服务地址与启动/停止控制
            EditorGUILayout.LabelField("服务地址", EditorStyles.boldLabel);
            if ((_ips?.Length ?? 0) > 0)
            {
                foreach (var ip in _ips)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(ip, EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("复制", EditorStyles.miniButton, GUILayout.Width(44)))
                    {
                        EditorGUIUtility.systemCopyBuffer = ip;
                        ShowNotification(new GUIContent("已复制 IP"));
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(4);
            _port = EditorGUILayout.IntField("服务端口", _port);
            _port = Mathf.Clamp(_port, 1, 65535);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (!_server.IsStarted)
            {
                if (GUILayout.Button("启动服务", EditorStyles.miniButton))
                {
                    _server.Port = _port;
                    EditorPrefs.SetInt(PrefKeyPort, _port);
                    LCLog.Log($"开始启动服务，端口：{_port}");
                    _server.Start();
                }
            }
            else
            {
                if (GUILayout.Button("停止服务", EditorStyles.miniButton))
                {
                    _server.Stop();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}