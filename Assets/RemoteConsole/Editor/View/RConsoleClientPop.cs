using UnityEditor;
using UnityEngine;

namespace RConsole.Editor
{
    public class RConsoleClientPop : PopupWindowContent
    {
        private Vector2 _windowSize = new Vector2(160, 44);
        public override Vector2 GetWindowSize() => _windowSize;

        private RConsoleMainWindow _owner = null;
        public RConsoleClientPop(RConsoleMainWindow owner)
        {
            _owner = owner;
        }

        public static void Open(Rect rect, RConsoleMainWindow owner)
        {
            // 使用系统内置 GenericMenu 下拉菜单，获得与 Unity Console 相同的对齐与行为
            var menu = new GenericMenu();

            var clients = LCLog.ViewModel.ConnectedClients;
            var currentFilter = LCLog.ViewModel.FilterClientInfoModel;

            if (clients == null || clients.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("暂无客户端连接"));
            }
            else
            {
                // 仅列出客户端：点击未选项 -> 设为筛选；点击已选项 -> 取消筛选（显示全部）
                foreach (var client in clients)
                {
                    bool isSelected = currentFilter != null && currentFilter.deviceId == client.deviceId;
                    var label = new GUIContent($"{client.deviceName} ({client.deviceModel})");
                    menu.AddItem(label, isSelected, () =>
                    {
                        // 保持单选逻辑，并支持点击已选项取消筛选
                        foreach (var c in clients) c.isFiltered = false;
                        if (isSelected)
                        {
                            // 取消筛选：不选任何客户端即显示全部
                            LCLog.ViewModel.SetFilterClientInfoModel(null);
                        }
                        else
                        {
                            client.isFiltered = true;
                            LCLog.ViewModel.SetFilterClientInfoModel(client);
                        }
                        owner.Repaint();
                    });
                }
            }

            menu.DropDown(new Rect(rect.x + 130, rect.y + 15, rect.width, rect.height));
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            EditorGUILayout.BeginVertical();
            var clients = LCLog.ViewModel.ConnectedClients;
            if (clients.Count == 0)
            {
                EditorGUILayout.LabelField("暂无无客户端连接", EditorStyles.miniLabel);
                _windowSize = new Vector2(100, 44);
            }
            else
            {
                _windowSize = new Vector2(160, 44);
                var hasFiltered = false;
                foreach (var client in clients)
                {
                    client.isFiltered = GUILayout.Toggle(client.isFiltered, client.deviceName);
                    if (client.isFiltered)
                    {
                        // 其他置为 false
                        foreach (var c in clients)
                        {
                            c.isFiltered = false;
                        }
                        client.isFiltered = true;
                        LCLog.ViewModel.SetFilterClientInfoModel(client);
                        hasFiltered = true;
                    }
                }
                if (!hasFiltered)
                {
                    LCLog.ViewModel.SetFilterClientInfoModel(null);
                }
            }
            EditorGUILayout.EndVertical();
        }
    }
}