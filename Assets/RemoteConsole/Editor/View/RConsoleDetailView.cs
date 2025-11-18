using System;
using System.IO;
using System.Text.RegularExpressions;
using RConsole.Common;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RConsole.Editor
{
    // 渲染选中条目的详情区域（头部与消息+堆栈）
    internal class RConsoleDetailView
    {
        private readonly GUIContent _iconLog;
        private readonly GUIContent _iconWarn;
        private readonly GUIContent _iconErr;
        private readonly GUIStyle _styleStack;
        private readonly GUIStyle _styleLink;
        private Vector2 _scroll = Vector2.zero;
        private LogRecordModel _selectedItem = null;

        public RConsoleDetailView(GUIContent iconLog, GUIContent iconWarn, GUIContent iconErr, GUIStyle styleStack, GUIStyle styleLink)
        {
            _iconLog = iconLog;
            _iconWarn = iconWarn;
            _iconErr = iconErr;
            _styleStack = styleStack;
            _styleLink = styleLink;
        }

        public void OnEnable()
        {
            
        }

        public void OnDisable()
        {
            _selectedItem = null;
        }



        public void ResetScroll()
        {
            _scroll = Vector2.zero;

        }

        public void Clear() { 
            _selectedItem = null;
        }

        public void Draw(float detailHeight, LogRecordModel selectedItem)
        {
            _selectedItem = selectedItem;
            if (_selectedItem == null) return;

            EditorGUILayout.BeginVertical(GUILayout.Height(detailHeight));
            // 头部：图标 + 时间 + 标签
            EditorGUILayout.BeginHorizontal();
            var icon = _selectedItem.level == LogType.Log ? _iconLog : _selectedItem.level == LogType.Warning ? _iconWarn : _iconErr;
            Texture iconTex = icon?.image;
            if (iconTex != null)
            {
                GUILayout.Label(iconTex, GUILayout.Width(16), GUILayout.Height(16));
            }
            
            // 设备
            var txt = "[/]";
            if (_selectedItem.clientInfoModel != null)
            {
                txt = $"{_selectedItem.clientInfoModel.deviceName}({_selectedItem.clientInfoModel.deviceModel})";
            }
            EditorGUILayout.LabelField(txt, EditorStyles.miniLabel, GUILayout.Width(180));
            // 时间
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(_selectedItem.timestamp).ToLocalTime();
            var timeStr = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
            EditorGUILayout.LabelField(timeStr, EditorStyles.miniLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField(_selectedItem.tag ?? string.Empty, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            // 内容滚动视口：消息 + 堆栈
            float headerHeight = EditorGUIUtility.singleLineHeight + 6f;
            float contentViewportHeight = Mathf.Max(20f, detailHeight - headerHeight - 4f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(contentViewportHeight));
            EditorGUILayout.SelectableLabel(_selectedItem.message ?? string.Empty, _styleStack, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(_selectedItem.stackTrace))
            {
                EditorGUILayout.Space(4);
                var lines = _selectedItem.stackTrace.Split('\n');
                foreach (var line in lines)
                {
                    // 先匹配 "(at path:line)"，不成功则匹配 " in path:line"
                    var m = Regex.Match(line, "\\(at (.+?):(\\d+)\\)");
                    if (m.Success)
                    {
                        DrawLinkLine(line, m, prefixText: "(at ", suffixText: ")");
                    }
                    else
                    {
                        var m2 = Regex.Match(line, "(\\s+in\\s+)(.+?):(\\d+)");
                        if (m2.Success)
                        {
                            DrawLinkLine(line, m2, prefixText: m2.Groups[1].Value, suffixText: string.Empty);
                        }
                        else
                        {
                            EditorGUILayout.SelectableLabel(line, _styleStack, GUILayout.ExpandWidth(true));
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLinkLine(string fullLine, Match match, string prefixText, string suffixText)
        {
            var before = fullLine.Substring(0, match.Index);
            var path = match.Groups[match.Groups.Count - 2].Value.Trim();
            var lineStr = match.Groups[match.Groups.Count - 1].Value.Trim();
            var after = fullLine.Substring(match.Index + match.Length);

            var leftText = before + prefixText;
            var linkText = path + ":" + lineStr;
            Vector2 leftSize = _styleStack.CalcSize(new GUIContent(leftText));
            Vector2 linkSize = _styleLink.CalcSize(new GUIContent(linkText));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(leftText, _styleStack, GUILayout.Width(leftSize.x));
            GUILayout.Label(linkText, _styleLink, GUILayout.Width(linkSize.x));
            Rect linkRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);
            EditorGUILayout.LabelField(suffixText + after, _styleStack, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && linkRect.Contains(Event.current.mousePosition))
            {
                int lineNum = 1;
                int.TryParse(lineStr, out lineNum);
                var finalPath = path;
                if (!Path.IsPathRooted(finalPath))
                {
                    var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                    finalPath = Path.Combine(projectRoot, finalPath);
                }
                if (File.Exists(finalPath))
                {
                    InternalEditorUtility.OpenFileAtLineExternal(finalPath, Mathf.Max(1, lineNum));
                }
                else
                {
                    Debug.LogWarning($"文件未找到，无法打开：{finalPath}");
                }
                Event.current.Use();
            }
        }
    }
}