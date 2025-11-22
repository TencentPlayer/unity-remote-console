using System;
using System.Collections.Generic;
using RConsole.Common;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RConsole.Editor
{
    public class RConsoleTreeView : TreeView
    {
        private FileModel _rootModel;
        private TreeViewItem _rootItem;
        private Dictionary<string, RConsoleTreeViewItem> _pathToItem = new Dictionary<string, RConsoleTreeViewItem>();
        public Action<RConsoleTreeViewItem> OnItemClicked;

        public RConsoleTreeView(TreeViewState state) : base(state)
        {
            showAlternatingRowBackgrounds = true;
            Reload();
        }
        
        public void SetData(FileModel root)
        {
            _rootModel = root;
            Reload();
        }

        public void UpdateData(FileModel root)
        {
            if (root == null) return;
            if (_pathToItem.TryGetValue(root.Path, out var item))
            {
                item.FileModel = root;
                item.DisplayName = root.Name;
                item.displayName = root.Name;
                item.icon = RConsoleTreeViewItem.GetIconForModel(root);
            }
            else
            {
                _rootModel = root;
            }
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            _pathToItem.Clear();
            RConsoleTreeViewItem.ResetIds();
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var all = new List<TreeViewItem>();
            if (_rootModel != null)
            {
                new RConsoleTreeViewItem(0, _rootModel, all);
            }
            SetupParentsAndChildrenFromDepths(root, all);
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is RConsoleTreeViewItem it && it.FileModel != null)
                {
                    _pathToItem[it.FileModel.Path] = it;
                }
            }
            _rootItem = root;
            return root;
        }

        protected override void SingleClickedItem(int id)
        {
            var item = FindItem(id, _rootItem) as RConsoleTreeViewItem;
            if (item == null) return;
            OnItemClicked?.Invoke(item);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0) return;
            var id = selectedIds[0];
            var item = FindItem(id, _rootItem) as RConsoleTreeViewItem;
            if (item == null) return;
            OnItemClicked?.Invoke(item);
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, _rootItem) as RConsoleTreeViewItem;
            var menu = new GenericMenu();
            var req = new FileModel(item.FileModel.Id, item.FileModel.Path);
            if (item.FileModel.IsDirectory)
            {
                menu.AddItem(new GUIContent("同步"), false,
                    () =>
                    {
                        RConsoleCtrl.Instance.FetchDirectory(req);
                        SetExpanded(item.id, true);
                        Repaint();
                    });
            }
            else
            {
                menu.AddItem(new GUIContent("获取信息"), false, () => { RConsoleCtrl.Instance.RequestFileMD5(req); });
            }
            menu.ShowAsContext();
        }
    }
}