# 视频会议后端接口说明

本模块使用 Android 原生端 + Agora Video SDK 的接入方式。后端负责会议房间记录、成员权限、邀请通知和 Agora RTC Token 下发。

## 服务器配置

生产环境不要把 Agora 密钥写进代码，建议使用环境变量：

```bash
Agora__AppId=你的 Agora AppId
Agora__AppCertificate=你的 Agora AppCertificate
Agora__TokenExpireSeconds=3600
```

如果 Agora 项目关闭了 App Certificate，可以只配置 `Agora__AppId`，接口会返回 `rtcToken: null` 和 `tokenRequired: false`。

## 数据库迁移

已生成迁移：

```text
backend/src/FangFeishu.Api/Data/Migrations/20260709080639_AddMeetings.cs
```

服务器可二选一执行：

```bash
dotnet ef database update --project backend/src/FangFeishu.Api/FangFeishu.Api.csproj --startup-project backend/src/FangFeishu.Api/FangFeishu.Api.csproj
```

或执行 SQL：

```text
backend/scripts/sql/20260709080639_AddMeetings.sql
```

## 接口列表

所有接口都需要登录后的 Bearer Token。

### 创建会议

`POST /api/v1/meetings`

请求：

```json
{
  "title": "项目同步会议",
  "roomName": "项目同步会议",
  "roomId": "ff_202607091610",
  "memberUserIds": []
}
```

说明：`title`、`roomName` 任传一个即可；`roomId` 可不传，后端会自动生成。

### 加入会议并获取 Agora 参数

`POST /api/v1/meetings/{id}/join`

返回的 `appId`、`channelName`、`uid`、`rtcToken` 给 Android Agora SDK 入会使用。

### 查询会议列表

`GET /api/v1/meetings`

可选查询：`?status=Active`、`?status=Ended` 或 `?status=All`。状态值不区分大小写；不传状态或传 `All` 时返回当前用户可访问的全部会议。

### 查询会议详情

`GET /api/v1/meetings/{id}`

会议成员对象包含 `userId`、`userName`、`username`、`avatarUrl`、`role`、参会时间和 `rtcIdentities`。Android、PC 和 Web 客户端应使用 `rtcIdentities.uid` 映射 RTC 用户，并在成员关闭摄像头时使用 `avatarUrl` 显示真实头像。`rtcIdentities` 同时包含 `Legacy` 历史 UID 和 Android/Desktop/Web/MiniProgram 四端 UID，用于新旧服务滚动部署期间保持用户名、头像映射稳定。

### 邀请成员

`POST /api/v1/meetings/{id}/invite`

请求：

```json
{
  "memberUserIds": ["用户ID"]
}
```

只有会议创建人或系统管理员可以邀请成员；会议结束后不能继续邀请。新增成员成功后，后端向每名新增成员推送 `MeetingInvited`，事件结构参见 [REALTIME-EVENTS.md](REALTIME-EVENTS.md)。

### 离开会议

`POST /api/v1/meetings/{id}/leave`

记录当前用户离会时间，不会自动结束整个会议。

### 结束会议

`PATCH /api/v1/meetings/{id}/end`

只有会议创建人或系统管理员可以结束会议。接口支持幂等调用：会议已结束时直接返回当前会议状态，不会重复推送 `MeetingEnded`。
