// 放在 Editor 文件夹
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class IconHelper
{
    /// <summary>
    /// 返回本机所有可用图标名字（2018+ 亲测有效）
    /// </summary>
    public static string[] GetAllIconNames()
    {
        // 先找 2020.2+ 的枚举
        var iconsType = Type.GetType("UnityEditorInternal.Icons,UnityEditor");
        if (iconsType != null)
            return Enum.GetNames(iconsType);

        // 老版本：反射拿 EditorGUIUtility.iconContentCache
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var cacheField = typeof(EditorGUIUtility).GetField("s_IconList", flags) ??
                         typeof(EditorGUIUtility).GetField("iconContentCache", flags);
        if (cacheField == null) return new string[0];

        var dict = cacheField.GetValue(null) as System.Collections.IDictionary;
        if (dict == null) return new string[0];

        var list = new List<string>();
        foreach (var k in dict.Keys) list.Add(k.ToString());
        return list.ToArray();
    }

    [MenuItem("Tools/Print All Icon Names")]
    static void Dump()
    {
        var icons = GetAllIconNames();
        Debug.Log($"Icon count: {icons.Length}");
        foreach (var n in icons)
            if (n.ToLower().Contains("arrow") || n.ToLower().Contains("down"))
                Debug.Log(n);
    }
}