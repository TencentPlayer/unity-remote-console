using System;
using System.Linq;
using RConsole.Common;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RConsole.Editor
{
    public class RConsoleMainWindow : EditorWindow
    {
        private static RConsoleMainWindow selfWindows = null;
        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _showLog = true, _showWarning = true, _showError = true;
        private bool _autoScrollToBottom = false;
        private SearchField _searchField;
        // 缓存图标与样式，避免在大量日志时频繁分配
        private GUIContent _iconLog, _iconWarn, _iconErr;
        private GUIStyle _styleLog, _styleWarn, _styleError, _styleStack, _styleTime, _styleIp, _styleLink;
        private RConsoleListItemView _listItemView;
        private RConsoleDetailView _detailView;
        // 选中项（用于下半部分显示详情）
        private LogRecordModel _selectedItem;
        // 详情区域高度比例（0.2~0.8），用于调节下半部分高度
        private float _detailsHeightRatio = 0.5f;
        // 是否正在拖动分隔条调节高度
        private bool _isResizingDetails = false;
        private RConsoleServer _server;

        [MenuItem("Window/Remote Console")]
        public static void ShowWindow()
        {
            var win = GetWindow<RConsoleMainWindow>(title: "Remote Console");
            win.Init();
            win.Show();
        }

        private void Awake() {
        }

        private void OnEnable() 
        {
            _server ??= new RConsoleServer();
            LCLog.ViewModel.OnModelChanged += OnModelChanged;
            var wins = Resources.FindObjectsOfTypeAll<RConsoleMainWindow>();
            if (wins != null && wins.Length > 0)
            {
                selfWindows = wins[0];
            }
            _detailView?.OnEnable();
        }

        private void OnDisable()
        {
            LCLog.ViewModel.OnModelChanged -= OnModelChanged;
            selfWindows = null;
            _selectedItem = null;
            _detailView?.OnDisable();
        }

        private void OnModelChanged(RConsoleViewModel model)
        {
            RefreshIfOpen();
        }

        // 在收到新日志时，如果窗口已打开则请求重绘（不抢占焦点）
        public static void RefreshIfOpen()
        {
            if (selfWindows == null) return;
            selfWindows.Repaint();
        }

        private void Init()
        {
            // 服务由界面控制
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += () => { Repaint(); };

            // 初始化图标缓存
            _iconLog = EditorGUIUtility.IconContent("console.infoicon");
            _iconWarn = EditorGUIUtility.IconContent("console.warnicon");
            _iconErr = EditorGUIUtility.IconContent("console.erroricon");

            // 初始化样式缓存（更紧凑，不换行）
            _styleLog = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            _styleLog.normal.textColor = Color.white;

            _styleWarn = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            _styleWarn.normal.textColor = new Color(0.8f, 0.6f, 0.0f);

            _styleError = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            _styleError.normal.textColor = Color.red;

            _styleStack = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            _styleStack.normal.textColor = Color.white;

            _styleTime = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            _styleTime.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            _styleIp = new GUIStyle(EditorStyles.miniLabel) { wordWrap = false };
            _styleIp.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            // 可点击超链接样式（蓝色，支持换行）
            _styleLink = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            _styleLink.normal.textColor = new Color(0.2f, 0.5f, 1f);

            // 初始化列表项渲染器
            _listItemView = new RConsoleListItemView(
                _iconLog,
                _iconWarn,
                _iconErr,
                _styleTime,
                _styleIp,
                _styleLog,
                _styleWarn,
                _styleError
            );

            // 初始化详情视图渲染器
            _detailView = new RConsoleDetailView(
                _iconLog,
                _iconWarn,
                _iconErr,
                _styleStack,
                _styleLink
            );
        }


        private void OnGUI()
        {
            Init();
            Toolbar();
            DrawList();
        }

        private void Toolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 将 Clear 按钮放到最左侧
            var iconClear = EditorGUIUtility.IconContent("TreeEditor.Trash");
            // 显示图标 + 文本 "Clear"
            if (GUILayout.Button(new GUIContent("Clear", iconClear.image, "Clear"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                LCLog.ViewModel.Clear();
                // 清空面板上的值
                _selectedItem = null;
                _detailView?.Clear();
                Repaint();
            }

            // Play 按钮
            var isServerStarted = LCLog.ViewModel.IsServerStarted;
            var playText = isServerStarted ? "关闭服务" : "启动服务";
            var ips = NETUtils.GetIPv4Addresses();
            var tips = $"服务地址: {string.Join(", ", ips)}";
            var icon = EditorGUIUtility.IconContent(isServerStarted ? "PauseButton" : "d_PlayButton");
            if (GUILayout.Button(new GUIContent(playText,icon.image, tips), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                // 播放按钮点击事件
                if (isServerStarted)
                {
                    _server.Stop();
                }
                else
                {
                    _server.Start();
                }
            }

            // Server 按钮紧随其后，宽度不扩展
            var clients = LCLog.ViewModel.ConnectedClients;
            var text = $"连接设备({clients.Count})";
            var filterClient = LCLog.ViewModel.FilterClientInfoModel;
            if (filterClient != null)
            {
                text += $"[{filterClient.deviceName}]";
            }
            var serverContent = new GUIContent(text);
            if (GUILayout.Button(serverContent, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false)))
            {
                var serverBtnRect = GUILayoutUtility.GetLastRect();
                RConsoleClientPop.Open(serverBtnRect, this);
            }

            // 搜索框紧随其后，宽度固定为 200
            var searchRect = GUILayoutUtility.GetRect(200, EditorGUIUtility.singleLineHeight, EditorStyles.toolbarSearchField, GUILayout.Width(200), GUILayout.ExpandWidth(false));
            _search = _searchField.OnGUI(searchRect, _search);

            // 计算各类型日志数量，用图标+数量显示
            var items = LCLog.ViewModel.Snapshot();
            var countLog = items.Count(i => i.level == LogType.Log);
            var countWarn = items.Count(i => i.level == LogType.Warning);
            var countErr = items.Count(i => i.level == LogType.Error);
            var countTotal = items.Count;

            var iconLog = EditorGUIUtility.IconContent("console.infoicon");
            var iconWarn = EditorGUIUtility.IconContent("console.warnicon");
            var iconErr = EditorGUIUtility.IconContent("console.erroricon");

            // if (GUILayout.Button("Test", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            // {
            //     LCLog.Log("Test Info NullReferenceException: Object reference not set to an instance of an object\r\n Line2");
            //     LCLog.LogWarning("Test Warn NullReferenceException: Object reference not set to an instance of an object\r\n Line2");
            //     LCLog.LogError("Test Error NullReferenceException: Object reference not set to an instance of an object\r\n Line2");
            // }
            _showLog = GUILayout.Toggle(_showLog, new GUIContent(countLog.ToString(), iconLog.image, "Log"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            _showWarning = GUILayout.Toggle(_showWarning, new GUIContent(countWarn.ToString(), iconWarn.image, "Warning"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            _showError = GUILayout.Toggle(_showError, new GUIContent(countErr.ToString(), iconErr.image, "Error"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            // Clear 按钮已置于最左侧

            // 右侧添加“始终滚动到底部”图标开关（带回退，避免空引用）
            GUILayout.FlexibleSpace();
            var iconAutoBottom = EditorGUIUtility.IconContent("dropdown");
            Texture iconAutoBottomTex = iconAutoBottom != null ? iconAutoBottom.image : null;
            var autoBottomTooltip = _autoScrollToBottom ? "当前始终滚动到底部" : "选中后始终滚动到底部";
            var autoBottomContent = iconAutoBottomTex != null
                ? new GUIContent(iconAutoBottomTex, autoBottomTooltip)
                : new GUIContent("底部", autoBottomTooltip);
            _autoScrollToBottom = GUILayout.Toggle(
                _autoScrollToBottom,
                autoBottomContent,
                EditorStyles.toolbarButton,
                GUILayout.ExpandWidth(false));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawList()
        {
            var items = LCLog.ViewModel.Snapshot();
            var filtered = items.Where(i => PassFilter(i)).ToArray();

            bool hasSelection = _selectedItem != null;
            var prevSelected = _selectedItem;
            // 上半部分：列表（仅显示首行，支持点击选中）
            float listHeight;
            float detailHeight = 0f;
            if (hasSelection)
            {
                detailHeight = Mathf.Clamp(position.height * _detailsHeightRatio, 60f, position.height - 100f);
                listHeight = Mathf.Max(60f, position.height - detailHeight - 16f);
            }
            else
            {
                // 无选中时，详情隐藏，列表占满
                listHeight = Mathf.Max(80f, position.height - 8f);
            }

            // 键盘导航：上下箭头切换选中项，并使其滚动到可见区域
            bool selectionChangedByKey = false;
            if (Event.current.type == EventType.KeyDown && filtered.Length > 0)
            {
                int selIndex = Array.IndexOf(filtered, _selectedItem); // 找到当前选中项的索引
                int newIndex = selIndex;
                float rowHeight = 18f;
                int pageStep = Mathf.Max(1, Mathf.FloorToInt(listHeight / rowHeight) - 1);
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    newIndex = selIndex >= 0 ? Mathf.Max(0, selIndex - 1) : filtered.Length - 1;
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    newIndex = selIndex >= 0 ? Mathf.Min(filtered.Length - 1, selIndex + 1) : 0;
                }
                else if (Event.current.keyCode == KeyCode.Home)
                {
                    newIndex = 0;
                }
                else if (Event.current.keyCode == KeyCode.End)
                {
                    newIndex = filtered.Length - 1;
                }
                else if (Event.current.keyCode == KeyCode.PageUp)
                {
                    newIndex = selIndex >= 0 ? Mathf.Max(0, selIndex - pageStep) : 0;
                }
                else if (Event.current.keyCode == KeyCode.PageDown)
                {
                    newIndex = selIndex >= 0 ? Mathf.Min(filtered.Length - 1, selIndex + pageStep) : filtered.Length - 1;
                }

                if (newIndex != selIndex)
                {
                    _selectedItem = filtered[newIndex];
                    _detailView?.ResetScroll();
                    selectionChangedByKey = true;

                    // 将选中项滚动到可见范围
                    float targetY = newIndex * rowHeight;
                    float viewTop = _scroll.y;
                    float viewBottom = _scroll.y + listHeight - rowHeight;
                    if (targetY < viewTop)
                        _scroll.y = targetY;
                    else if (targetY > viewBottom)
                        _scroll.y = Mathf.Max(0f, targetY - (listHeight - rowHeight));

                    Repaint();
                    Event.current.Use();
                }
            }
            if (_autoScrollToBottom && !selectionChangedByKey)
            {
                _scroll.y = float.MaxValue;
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(listHeight));
            foreach (var it in filtered)
            {
                // 预留行矩形用于绘制与点击
                Rect rowRect = EditorGUILayout.GetControlRect(false, 18);

                // 处理点击选中
                if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                {
                    if (!ReferenceEquals(prevSelected, it))
                    {
                        _selectedItem = it;
                        _detailView?.ResetScroll();
                    }
                    hasSelection = true;
                    Repaint();
                    Event.current.Use();
                }

                // 委托给列表项渲染类进行绘制
                _listItemView.DrawRow(rowRect, it, _selectedItem == it);
            }
            EditorGUILayout.EndScrollView();

            // 分隔条 + 调节详情高度（仅在有选中时显示）
            if (hasSelection)
            {
                Rect splitterRect = GUILayoutUtility.GetRect(1, 6, GUILayout.ExpandWidth(true));
                // 背景线条
                EditorGUI.DrawRect(new Rect(splitterRect.x, splitterRect.y, splitterRect.width, 1), Color.black);
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

                // 开始拖动：按下分隔条区域
                if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
                {
                    _isResizingDetails = true;
                    Event.current.Use();
                }
                // 拖动中：无论鼠标是否仍在分隔条上，都调整高度
                if (_isResizingDetails && Event.current.type == EventType.MouseDrag)
                {
                    float mouseY = Event.current.mousePosition.y;
                    float totalH = position.height;
                    _detailsHeightRatio = Mathf.Clamp((totalH - mouseY) / totalH, 0.2f, 0.8f);
                    Repaint();
                    Event.current.Use();
                }
                // 结束拖动
                if (_isResizingDetails && Event.current.type == EventType.MouseUp)
                {
                    _isResizingDetails = false;
                    Event.current.Use();
                }
            }

            // 下半部分：详情区域（显示选中项完整信息）
            if (hasSelection)
            {
                _detailView.Draw(detailHeight, _selectedItem);
            }
        }

        private bool PassFilter(LogRecordModel i)
        {
            if (!_showLog && i.level == LogType.Log) return false;
            if (!_showWarning && i.level == LogType.Warning) return false;
            if (!_showError && i.level >= LogType.Error) return false;

            // 客户端设备筛选
            var filterClient = LCLog.ViewModel.FilterClientInfoModel;
            if (filterClient != null)
            {
                var did = i.clientInfoModel?.deviceId;
                if (string.IsNullOrEmpty(did) || did != filterClient.deviceId) return false;
            }

            if (!string.IsNullOrEmpty(_search))
            {
                var s = _search.ToLowerInvariant();
                if (!(i.message?.ToLowerInvariant().Contains(s) == true || i.tag?.ToLowerInvariant().Contains(s) == true))
                    return false;
            }
            return true;
        }
    }
}