using System.Collections.Generic;
using System.IO;
using RConsole.Common;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;

namespace RConsole.Editor
{
    public class RConsoleTreeViewItem : TreeViewItem
    {
        public string DisplayName { get; set; } = string.Empty;

        public FileModel FileModel { get; set; } = null;

        private List<TreeViewItem> _allItems = null;

        private static int sNextId = 1;
        public static void ResetIds()
        {
            sNextId = 1;
        }

        public RConsoleTreeViewItem(int d, FileModel fileModel, List<TreeViewItem> all)
        {
            depth = d;
            _allItems = all;
            _allItems.Add(this);
            if (fileModel != null)
            {
                UpdateData(fileModel);
            }
        }

        private void ParseChildren()
        {
            if (FileModel?.Children == null) return;
            for (int i = 0; i < FileModel.Children.Count; i++)
            {
                var child = FileModel.Children[i];
                new RConsoleTreeViewItem(depth + 1, child, _allItems);
            }
        }

        public void UpdateData(FileModel fileModel)
        {
            FileModel = fileModel;
            DisplayName = fileModel.Name;
            displayName = DisplayName;
            id = !string.IsNullOrEmpty(fileModel.Path) ? fileModel.Path.GetHashCode() : sNextId++;
            icon = GetIconForModel(fileModel);
            ParseChildren();
        }

        public static Texture2D GetIconForModel(FileModel m)
        {
            if (m == null) return null;
            if (m.IsDirectory) return EditorGUIUtility.FindTexture("Folder Icon") as Texture2D;

            Texture2D tex = null;
            var ext = Path.GetExtension(m.Path)?.ToLowerInvariant();
            switch (ext)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".gif":
                case ".bmp":
                case ".tif":
                case ".tiff":
                case ".exr":
                    tex = EditorGUIUtility.FindTexture("Texture2D Icon") as Texture2D;
                    break;
                case ".mp3":
                case ".wav":
                case ".ogg":
                case ".aiff":
                    tex = EditorGUIUtility.FindTexture("AudioClip Icon") as Texture2D;
                    break;
                case ".mp4":
                case ".mov":
                case ".avi":
                case ".m4v":
                case ".wmv":
                    tex = EditorGUIUtility.FindTexture("VideoClip Icon") as Texture2D;
                    break;
                case ".cs":
                case ".js":
                case ".ts":
                case ".json":
                case ".xml":
                case ".yaml":
                case ".yml":
                case ".txt":
                case ".csv":
                case ".shader":
                    break; // use TextAsset Icon fallback
            }

            if (tex == null)
            {
                tex = EditorGUIUtility.FindTexture("TextAsset Icon") as Texture2D;
            }
            if (tex == null)
            {
                var content = EditorGUIUtility.IconContent("DefaultAsset Icon");
                tex = content?.image as Texture2D;
            }
            return tex;
        }
    }
}