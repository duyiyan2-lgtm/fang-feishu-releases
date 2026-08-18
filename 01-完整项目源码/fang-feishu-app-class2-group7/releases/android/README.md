# Android 安装包历史版本

当前归档包含 26 个 APK：一个 2026-07-12 基线包，以及 v1.0.1～v1.0.27 中现存的 25 个版本。最新版本为 **v1.0.27**。

## 还原最新 APK

在仓库根目录打开 PowerShell，执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\android-apk-archive\Restore-Apk.ps1 `
  -ManifestPath .\releases\android\packages\manifests\fang-feishu-android-v1.0.27-chinese-registration-fix.apk.json `
  -OutputDirectory .\outputs
```

成功后得到：

```text
outputs\fang-feishu-android-v1.0.27-chinese-registration-fix.apk
```

脚本会核验文件大小和 SHA-256；不通过时会删除损坏的输出文件。其他版本只需替换 `-ManifestPath` 中的清单文件名。

完整原始校验值见 [SHA256SUMS.txt](packages/SHA256SUMS.txt)，机器可读目录见 [catalog.json](packages/catalog.json)。

## 版本与修复历史

| 版本 | 安装包文件 | 主要内容 |
| --- | --- | --- |
| 2026-07-12 基线 | `fang-feishu-android-fixed-20260712.apk` | 早期修正版基线 |
| v1.0.1 | `fang-feishu-android-v1.0.1-rtc-fix.apk` | RTC 修复 |
| v1.0.2 | `fang-feishu-android-v1.0.2-dynamic-background.apk` | 动态背景 |
| v1.0.3 | `fang-feishu-android-v1.0.3-navigation-ui.apk` | 导航界面优化 |
| v1.0.4 | `fang-feishu-android-v1.0.4-topbar-fix.apk` | 顶部栏修复 |
| v1.0.5 | `fang-feishu-android-v1.0.5-approvals.apk` | 审批功能 |
| v1.0.6 | `fang-feishu-android-v1.0.6-rtc-context-fix.apk` | RTC Context 修复 |
| v1.0.7 | — | 原归档目录未找到该版本 APK |
| v1.0.8 | `fang-feishu-android-v1.0.8-profile-friends-meeting.apk` | 个人资料、好友及会议 |
| v1.0.9 | `fang-feishu-android-v1.0.9-meeting-invite-layout.apk` | 会议邀请布局 |
| v1.0.10 | `fang-feishu-android-v1.0.10-single-session.apk` | 单会话控制 |
| v1.0.11 | `fang-feishu-android-v1.0.11-multi-rtc-meeting-end.apk` | 多端 RTC 与结束会议 |
| v1.0.12 | `fang-feishu-android-v1.0.12-bugfixes.apk` | 综合问题修复 |
| v1.0.13 | `fang-feishu-android-v1.0.13-cloud-tabs.apk` | 云端页签 |
| v1.0.14 | `fang-feishu-android-v1.0.14-0714-bugfixes.apk` | 7 月 14 日 BUG 清单修复 |
| v1.0.15 | `fang-feishu-android-v1.0.15-group-video-meeting.apk` | 群聊视频会议 |
| v1.0.16 | `fang-feishu-android-v1.0.16-pc-mobile-meeting.apk` | PC 与手机视频会议互通 |
| v1.0.17 | `fang-feishu-android-v1.0.17-logout-default-account-fix.apk` | 退出后默认 admin 修复 |
| v1.0.18 | `fang-feishu-android-v1.0.18-realtime-message-fix.apk` | 消息实时刷新修复 |
| v1.0.19 | `fang-feishu-android-v1.0.19-timezone-fix.apk` | 消息时间/时区修复 |
| v1.0.20 | `fang-feishu-android-v1.0.20-document-time-and-meeting-status-fix.apk` | 文档时间及会议状态修复 |
| v1.0.21 | `fang-feishu-android-v1.0.21-video-meeting-crash-fix.apk` | 视频会议闪退修复 |
| v1.0.22 | `fang-feishu-android-v1.0.22-meeting-header-layout-fix.apk` | 会议表头重叠修复 |
| v1.0.23 | — | 修复报告曾引用该版本，但原归档目录未找到 APK |
| v1.0.24 | `fang-feishu-android-v1.0.24-meeting-identity-avatar-fix.apk` | 会议身份与头像映射 |
| v1.0.25 | `fang-feishu-android-v1.0.25-camera-off-avatar-fix.apk` | 关闭摄像头后显示自定义头像 |
| v1.0.26 | `fang-feishu-android-v1.0.26-meeting-topbar-fix.apk` | 视频会议顶部样式修复 |
| v1.0.27 | `fang-feishu-android-v1.0.27-chinese-registration-fix.apk` | 中文用户名注册提示与校验修复 |

## 归档维护

新增 APK 时，使用 Python 3 重新生成共享归档：

```powershell
python .\tools\android-apk-archive\build_apk_archive.py <APK目录> .\releases\android\packages --clean
```

不要手工修改 `chunks`、`manifests`、`catalog.json` 或 `SHA256SUMS.txt`。
