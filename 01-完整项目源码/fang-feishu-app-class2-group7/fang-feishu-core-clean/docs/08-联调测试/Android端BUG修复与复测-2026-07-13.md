# Android 端 BUG 修复与复测

日期：2026-07-13  
客户端版本：`1.0.12`（versionCode `13`）

## 已修复

| 测试问题 | 修复内容 | 状态 |
| --- | --- | --- |
| 消息搜索功能不完整 | 同时搜索会话标题、成员、最后一条消息和服务端历史消息；点击历史消息可进入对应会话。 | 已修复 |
| 进入会话后未读数未清除 | 进入会话后调用 `PATCH /im/conversations/{id}/read`，返回列表时自动刷新未读数。 | 已修复 |
| 创建日程只能手输时间 | 改用 Android 原生日期和时间选择器；结束时间早于开始时间时给出明确提示。 | 已修复 |
| 云盘只有上传功能 | 新增文件/回收站切换、移入回收站、恢复文件、永久删除。 | 已修复 |
| 通知不可点击且不能消除未读 | 点击通知显示详情并标记已读；新增“全部已读”。 | 已修复 |

## 线上接口复测

- `GET /documents` 与 `GET /documents/{id}`：成功，文档详情包含正文内容。
- `PATCH /im/conversations/{id}/read`：成功，返回 `code=0`。
- `GET /im/messages/search`：成功，返回 `code=0`。
- `GET /files/trash`：成功，返回 `code=0`。
- `GET /notifications`：成功，返回 `code=0`。

## 视频会议阻塞项

生产环境实测 `POST /meetings/{id}/join` 返回：

```text
code: 2104
Agora AppId is not configured. Set Agora:AppId in server configuration.
```

Android 页面无法绕过该服务端配置。运维需在**实际对外运行的后端实例**设置 `Agora:AppId`、`Agora:AppCertificate`，重启服务后重新调用入会接口，确认响应同时包含非空的 `appId`、`rtcToken` 与合法范围内的 `uid`。

## 测试包

`android-app/release/fang-feishu-android-v1.0.12-bugfixes.apk`

## 剩余验证

已完成编译和 APK 元数据校验；当前未连接真机，仍需测试人员安装 `1.0.12` 后回归消息、日程、云盘、通知流程，并在运维修复 Agora 配置后复测双设备视频通话。
