# RemoteConsole 概述

RemoteConsole 是一个面向 Unity 项目的远程调试工具合集，提供统一的“远程日志采集”“远程游戏界面预览（LookIn）”“远程文件浏览、校验与下载”能力，帮助团队在真机环境下快速定位问题、还原现场并完成数据校验。

## 背景与痛点

- 真机问题难复现：设备端崩溃/卡顿/界面异常在编辑器中重现困难，定位成本高。
- 日志与状态割裂：仅有日志无法还原场景层级、激活状态、组件关系，交叉排查耗时。
- 资源校验繁琐：文件完整性与一致性需要手工拷贝/校验，MD5 对比不统一、效率低。
- 联调链路冗长：远程抓取信息依赖多工具与步骤，团队成员重复配置环境，沟通成本高。
- 跨平台差异：不同平台路径/权限/运行环境差异导致排障复杂，缺少统一视图。

## 功能一览

- 远程日志
  - 设备端实时把 Unity 日志推送到编辑器端显示
  - 支持日志堆栈查看，点击日志可跳转到对应代码位置
- LookIn：
  - 远程获取游戏界面层次关系、节点组件相关数据
  - 在 Editor 场景中生成与远端一致的可视化的层级视图
  - 支持在 Editor 对象节点显隐、位置、大小缩放、旋转等操作反向同步到远端
- 文件浏览：
  - 浏览设备端持久化目录
  - 支持目录同步（按需拉取子目录与文件）
  - 文件 MD5 获取
  - 文件下载
  
## 功能使用示例
  ![功能录屏](../../doc/screenrecord.mov)

## 集成与快速开始（设备端）

使用 `RCCapability` 单例进行连接与日志转发，参考 `Assets/Script/Sample.cs`。

```csharp
using RConsole.Runtime;
using UnityEngine;

public class Example : MonoBehaviour
{
    void Awake()
    {
        RCCapability.Instance.Initialize(Application.persistentDataPath);
    }

    void Update()
    {
        RCCapability.Instance.Update();
    }

    public void ConnectToEditor(string ip)
    {
        RCCapability.Instance.Connect(ip);
    }

    public void Disconnect()
    {
        RCCapability.Instance.Disconnect();
    }

    public void StartForwarding()
    {
        RCCapability.Instance.CaptureLog();
    }

    public void StopForwarding()
    {
        RCCapability.Instance.EscapeLog();
    }

    void OnDestroy()
    {
        RCCapability.Instance.EscapeLog();
        RCCapability.Instance.Disconnect();
    }
}
```

**常用状态**

- `RCCapability.Instance.IsConnected`：是否已连接到编辑器。
- `RCCapability.Instance.IsCapturingLogs`：是否正在转发 Unity 日志。

## 编辑器端操作指南

- 打开窗口：菜单 Window/Remote Console，或在代码中 `RConsole.Editor.RConsoleMainWindow.ShowWindow()`。
- 启动/关闭服务：窗口左上角“启动服务/关闭服务”按钮，默认端口 13337、路径 `/remote-console`。
- 设备筛选：点击“连接设备”下拉，选择某一设备后仅显示该设备日志；再次点击可取消筛选。
- 远程日志：窗口“日志”页签显示实时日志，支持堆栈与跳转。
- LookIn：
  - 点击“获取 LookIn 数据”按钮或代码 `RConsoleCtrl.Instance.FetchLookin()`。
  - 编辑器会在当前场景生成 `LookIn(设备名)` 根节点，并按远端层级生成子节点。
  - 支持在 Editor 中对节点进行 Active、位置、旋转、缩放、RectTransform Size 等操作，并实时反向同步到远端。
- 文件浏览：
  - 在窗口的“文件”页签中浏览设备端持久化目录，首次默认拉取根目录 `/`。
  - 选中目录后可执行“同步”，按需拉取子项并动态追加至树。
  - 选中文件后可“获取信息”，显示 MD5；右侧详情面板展示名称、路径、类型、大小、最后修改时间、MD5。
  - 支持拖拽调整左右面板宽度，展开状态在同步后保持。

## 网络与平台要求

- 设备与 Mac 必须在同一局域网，且网络不启用客户端隔离。
- Mac 防火墙/安全软件需允许端口 `13337` 入站访问。
- iOS 设备需要本地网络权限（`Info.plist` 的 `NSLocalNetworkUsageDescription` 说明）。
- 若使用 `ws://`，iOS 可能受 ATS 限制；开发阶段可在 `Info.plist` 添加例外，生产建议使用 `wss://`。

## 常见问题（FAQ）

- 连接卡住或失败：
  - 确认设备端没有使用 `127.0.0.1/localhost`，而是使用 Mac 的局域网 IP。
  - 检查端口 `13337` 是否被防火墙阻止，编辑器端是否已启动服务。
  - iOS 需开启本地网络权限；如使用 `ws://`，请根据需要添加 ATS 例外或改用 `wss://`。

---

如需更详细的接入示例或特定平台的配置说明，请反馈你的目标与场景，我可以补充相应文档与示例。
