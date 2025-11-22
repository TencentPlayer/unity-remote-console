using System;
using System.Collections.Generic;
using RConsole.Common;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RConsole.Editor
{
    public class RConsoleFileBrowser : EditorWindow
    {
        private RConsoleTreeView _tree;
        private TreeViewState _treeState;

        private FileModel _root;

        // private string _selectedPath;
        private RConsoleTreeViewItem _selecct;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private float _splitLeft = 260f;
        private bool _resizing;

        public static void ShowWindow()
        {
            var connection = RConsoleCtrl.Instance.GetSelectConnection();
            var window = GetWindow<RConsoleFileBrowser>($"{connection.ClientModel.deviceName}({connection.ClientModel.deviceModel})");
            window.Show();
            var rect = window.position;
            rect.width = 1000f;
            rect.height = 400f;
            window.position = rect;
            window._splitLeft = rect.width * 0.45f;
        }

        private void OnEnable()
        {
            _treeState ??= new TreeViewState();
            _tree = new RConsoleTreeView(_treeState)
            {
                OnItemClicked = OnItemClicked
            };
            RConsoleCtrl.Instance.OnFileBrowserChanged += OnFileBrowserChanged;
            RConsoleCtrl.Instance.OnFileMD5Changed += OnFileMD5Changed;
            RConsoleCtrl.Instance.FetchDirectory(new FileModel("/"));
        }

        private void OnDisable()
        {
            RConsoleCtrl.Instance.OnFileBrowserChanged -= OnFileBrowserChanged;
            RConsoleCtrl.Instance.OnFileMD5Changed -= OnFileMD5Changed;
        }

        private void OnFileBrowserChanged(FileModel resp)
        {
            _root = resp;
            _tree.SetData(resp);
            Repaint();
        }

        private void OnGUI()
        {
            float minLeft = 160f;
            float minRight = 240f;
            _splitLeft = Mathf.Clamp(_splitLeft, minLeft, Mathf.Max(minLeft, position.width - minRight));
            float leftWidth = _splitLeft;
            float splitterW = 6f;
            Rect leftRect = new Rect(0, 0, leftWidth - splitterW * 0.5f, position.height);
            Rect splitterRect = new Rect(leftWidth - splitterW * 0.5f, 0, splitterW, position.height);
            Rect rightRect = new Rect(leftWidth + splitterW * 0.5f, 0, position.width - leftWidth - splitterW * 0.5f, position.height);

            GUILayout.BeginArea(leftRect);
            _leftScroll = GUILayout.BeginScrollView(_leftScroll);
            _tree?.OnGUI(new Rect(0, 0, leftRect.width, position.height));
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            EditorGUI.DrawRect(splitterRect, new Color(0, 0, 0, 0.15f));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                _resizing = true;
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseDrag && _resizing)
            {
                _splitLeft = Mathf.Clamp(Event.current.mousePosition.x, minLeft, Mathf.Max(minLeft, position.width - minRight));
                Repaint();
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp && _resizing)
            {
                _resizing = false;
                Event.current.Use();
            }

            GUILayout.BeginArea(rightRect);
            _rightScroll = GUILayout.BeginScrollView(_rightScroll);
            DrawDetails();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawDetails()
        {
            if (_root == null || _selecct == null) return;

            var m = _selecct.FileModel;
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(m.LastWriteTime).ToLocalTime();
            DrawRow("名称", m.Name);
            DrawRow("路径", m.Path.Replace(m.RootPath, ""));
            DrawRow("类型", m.IsDirectory ? "目录" : "文件");
            DrawRow("大小", m.IsDirectory ? "-" : FormatSize(m.Length));
            DrawRow("最后修改", dt.ToString("yyyy-MM-dd HH:mm:ss"));
            if (!m.IsDirectory && !string.IsNullOrEmpty(m.MD5))
            {
                DrawRow("MD5", m.MD5);
            }
            EditorGUILayout.Space();
        }

        private void DrawRow(string title, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.Width(120));
            EditorGUILayout.LabelField(value, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 0) bytes = 0;
            double v = bytes;
            string u = "B";
            if (v >= 1024) { v /= 1024; u = "KB"; }
            if (v >= 1024) { v /= 1024; u = "MB"; }
            if (v >= 1024) { v /= 1024; u = "GB"; }
            if (u == "B") return bytes + " B";
            var fmt = v >= 100 ? "F0" : v >= 10 ? "F1" : "F2";
            return v.ToString(fmt) + " " + u;
        }

        private void OnItemClicked(RConsoleTreeViewItem item)
        {
            _selecct = item;
            // _currentMD5 = string.Empty;
            Repaint();
        }

        private List<FileModel> GetChildrenByPath(string dirPath)
        {
            var result = new List<FileModel>();
            if (_root == null) return result;
            var target = FindNodeByPath(_root, dirPath);
            if (target != null && target.Children != null)
            {
                result.AddRange(target.Children);
            }
            return result;
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

        private void OnFileMD5Changed(FileModel resp)
        {
            if (_selecct == null || resp == null) return;
            var currentPath = _selecct.FileModel?.Path;
            if (string.Equals(currentPath, resp.Path, StringComparison.OrdinalIgnoreCase))
            {
                if (_selecct.FileModel != null) _selecct.FileModel.MD5 = resp.MD5;
                Repaint();
            }
        }
    }
}