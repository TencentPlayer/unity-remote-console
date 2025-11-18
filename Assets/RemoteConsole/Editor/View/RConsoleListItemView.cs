using System;
using RConsole.Common;
using UnityEditor;
using UnityEngine;

namespace RConsole.Editor
{
    // 渲染列表中的单条日志项（图标、时间、首行消息）
    internal class RConsoleListItemView
    {
        private readonly GUIContent _iconLog;
        private readonly GUIContent _iconWarn;
        private readonly GUIContent _iconErr;
        private readonly GUIStyle _styleTime;
        private readonly GUIStyle _styleIp;
        private readonly GUIStyle _styleLog;
        private readonly GUIStyle _styleWarn;
        private readonly GUIStyle _styleError;

        public RConsoleListItemView(
            GUIContent iconLog,
            GUIContent iconWarn,
            GUIContent iconErr,
            GUIStyle styleTime,
            GUIStyle styleIp,
            GUIStyle styleLog,
            GUIStyle styleWarn,
            GUIStyle styleError)
        {
            _iconLog = iconLog;
            _iconWarn = iconWarn;
            _iconErr = iconErr;
            _styleTime = styleTime;
            _styleIp = styleIp;
            _styleLog = styleLog;
            _styleWarn = styleWarn;
            _styleError = styleError;
        }

        public void DrawRow(Rect rowRect, LogRecordModel it, bool isSelected)
        {
            if (isSelected)
            {
                // 选中项背景高亮
                EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.15f));
            }

            float x = rowRect.x + 4f;
            float y = rowRect.y + 2f;

            // 图标
            var icon = it.level == LogType.Log ? _iconLog : it.level == LogType.Warning ? _iconWarn : _iconErr;
            Texture iconTex = icon?.image;
            if (iconTex != null)
            {
                GUI.DrawTexture(new Rect(x, y, 14, 14), iconTex);
            }
            x += 14F; // 图标占位宽度

            // 时间
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(it.timestamp).ToLocalTime();
            var timeStr = dt.ToString("[HH:mm:ss.fff]");
            GUI.Label(new Rect(x, rowRect.y, 80, rowRect.height), timeStr, _styleTime);
            x += 80F;

            // IP
            var ip = it.clientInfoModel == null ? "0.0.0.0" : it.clientInfoModel.address;
            GUI.Label(new Rect(x, rowRect.y, 100, rowRect.height), $"[{ip}]", _styleIp);
            x += 100F;

            // 首行消息
            string msg = it.message ?? string.Empty;
            int nl = msg.IndexOf('\n');
            if (nl >= 0) msg = msg.Substring(0, nl);

            var style = it.level == LogType.Log ? _styleLog : it.level == LogType.Warning ? _styleWarn : _styleError;
            GUI.Label(new Rect(x, rowRect.y, rowRect.width - (x - rowRect.x) - 4f, rowRect.height), msg, style);
        }
    }
}