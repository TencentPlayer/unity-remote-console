# RemoteConsole 使用指南

RemoteConsole 帮助你在 Unity 编辑器中集中查看来自设备（iOS/Android/其他平台）运行时的日志与错误信息，并支持按设备进行筛选。

## 功能

- 设备端实时把 Unity 日志推送到编辑器端显示。
- 编辑器支持按“客户端设备”进行单选筛选；再次点击已选设备即可恢复显示全部。
- 连接时自动上报设备信息（设备名、平台、应用信息、会话 ID）。

## 集成方式（推荐）

使用 `RCLogManager` 单例进行连接与日志转发，参考项目中的示例 `Assets/Script/Sample.cs`。

```csharp
using RConsole.Runtime;
using UnityEngine;

public class Example : MonoBehaviour
{
    // 连接到编辑器所在电脑（请使用 Mac 的局域网 IP，不要用 127.0.0.1/localhost）
    public void ConnectToEditor(string ip)
    {
        RCLogManager.Instance.Connect(ip); // 默认端口 13337，路径 /remote-console
    }

    public void Disconnect()
    {
        RCLogManager.Instance.Disconnect();
    }

    // 开始/停止转发 Unity 日志到编辑器
    public void StartForwarding()
    {
        RCLogManager.Instance.ForwardingUnityLog();
    }

    public void StopForwarding()
    {
        RCLogManager.Instance.StopForwardingUnityLog();
    }
}
```

### 常用状态

- `RCLogManager.Instance.IsConnected`：是否已连接到编辑器。
- `RCLogManager.Instance.IsCapturingLogs`：是否正在转发 Unity 日志。

## 编辑器端

- 在 Unity 中打开 RemoteConsole 主窗口（菜单入口已集成）。
- 点击“连接设备”按钮，即可查看当前连接的设备并进行筛选。

## 注意事项

- 设备与 Mac 必须在同一局域网，且网络不启用客户端隔离。
- Mac 防火墙/安全软件需允许端口 `13337` 入站访问。
- iOS 设备需要本地网络权限（`Info.plist` 的 `NSLocalNetworkUsageDescription` 说明）。
- 若使用明文 `ws://`，iOS 可能受 ATS 限制；开发阶段可在 `Info.plist` 添加例外，生产建议使用 `wss://`。

## 示例引用

- 完整交互按钮示例见 `Assets/Script/Sample.cs`（包含连接、断开、开始/停止转发、按钮状态联动）。

---

如需更详细的接入示例或 iOS 配置样例，请提出你的目标平台与部署方式，我可以补充相应说明。

## 注意事项

- 设备与 Mac 必须在同一局域网，且网络不启用客户端隔离。
- Mac 防火墙/安全软件需允许端口 `13337` 入站访问。
- iOS 设备需要本地网络权限（`Info.plist` 的 `NSLocalNetworkUsageDescription` 说明）。
- 若使用 `ws://`，iOS 可能受 ATS 限制；开发阶段可在 `Info.plist` 添加例外，生产建议使用 `wss://`。

## 常见问题

- 连接卡住或失败：
  - 确认设备端没有使用 `127.0.0.1/localhost`，而是使用 Mac 的局域网 IP。
  - 检查端口 `13337` 是否被防火墙阻止，编辑器端是否已启动服务。
  - iOS 需开启本地网络权限；如使用 `ws://`，请根据需要添加 ATS 例外或改用 `wss://`。

---

如需更详细的接入示例或 iOS 配置键值样例，请提出你的目标平台与部署方式，我可以补充相应说明。