# 仿飞书 Android 原生客户端

本目录是基于 **Kotlin + Jetpack Compose** 的 Android 客户端，默认请求已部署后端：

```text
https://alxy.fun/api/v1/
```

## 已接入功能

| 页面 | 已接入后端接口 |
| --- | --- |
| 登录与注册 | `POST /auth/login`、`POST /auth/register`、`GET /auth/me`、`POST /auth/logout` |
| 消息与群聊 | 会话列表、历史消息、发送消息、联系人、多成员群聊创建 |
| 云文档 | 文档列表、创建文档 |
| 日历 | 日程列表、创建日程 |
| 审批 | 审批列表、发起审批 |
| 云盘 | 文件列表、系统文件选择器上传 |
| 任务 | 任务列表、创建、完成状态流转 |
| 知识库 | 知识空间列表、创建 |
| 视频会议 | 会议列表、创建、加入、离开、Agora RTC 视频宫格 |
| 通知 | 通知列表 |

## 样式与会议实现

- 默认主题：3x3 `ComposeMeshGradient`，淡青色到淡蓝色低频动态渐变。
- 设置页可切换为深黑色飞书风格，选择会持久化到本地 DataStore。
- `Haze` 仅用于顶部栏、底部栏、控制栏、卡片等 Compose 浮层。
- Agora 视频通过 `AndroidView` 内的 `SurfaceView` 渲染。`SurfaceView` 不作为 Haze 的模糊源，因此控制栏保留半透明色调与边缘层次，但不依赖“真实视频模糊”。

## RTC 前提

手机端调用：

```text
POST /api/v1/meetings/{id}/join
```

后端返回 `appId`、`channelName`、`uid`、`rtcToken` 后，Android 使用 Agora SDK 加入频道。服务器必须配置有效的：

```text
Agora:AppId
Agora:AppCertificate
```

若接口返回错误码 `2104`，说明服务器尚未配置 Agora AppId，客户端无法进入真实音视频房间。

## 构建

要求：JDK 17、Android SDK API 36、`arm64-v8a` Android 真机。

```powershell
cd android-app
.\gradlew.bat assembleDebug
```

Debug APK 输出：

```text
app\build\outputs\apk\debug\app-debug.apk
```

首次运行使用后端已验证的账号登录即可。不要将生产账号密码、JWT 或 Agora AppCertificate 写入客户端代码。
