# SignalR 实时事件契约

## 连接

- Hub 地址：`/hubs/im`
- 鉴权：在连接查询参数中传入 `access_token={JWT}`。
- 连接成功后，服务端会自动把当前连接加入 `user:{userId}` 用户组。
- 聊天页面调用 `JoinConversation(conversationId)` 后加入对应的会话组。

SignalR 是长连接协议，不属于普通 HTTP Controller，因此这些方法和事件不会显示在 Swagger 中。

## 服务端推送事件

### 通知

#### `ReceiveNotification`

任何业务模块新增通知并成功保存后，自动发送给通知所属用户。

```json
{
  "id": "notification-uuid",
  "title": "Meeting invitation",
  "content": "项目同步会议",
  "type": "Meeting",
  "resourceType": "Meeting",
  "resourceId": "meeting-uuid",
  "isRead": false,
  "createdAt": "2026-07-15T01:30:00Z"
}
```

#### `NotificationRead`

当前用户在任一客户端将单条通知标记为已读时发送，用于同步其他设备。

```json
{
  "id": "notification-uuid",
  "isRead": true
}
```

#### `NotificationsReadAll`

当前用户将全部通知标记为已读时发送。

```json
{
  "unreadCount": 0
}
```

### 好友

- `FriendRequestReceived`：收到新的好友申请。
- `FriendRequestAccepted`：好友申请已被接受，申请人和处理人都会收到。
- `FriendRequestRejected`：好友申请已被拒绝，申请人和处理人都会收到。
- `FriendRemoved`：好友关系被删除，双方都会收到。

前三个事件的载荷与好友申请 REST 返回结构一致：

```json
{
  "id": "friendship-uuid",
  "status": "Pending",
  "direction": "Incoming",
  "greeting": "你好",
  "createdAt": "2026-07-15T01:30:00Z",
  "user": {
    "id": "user-uuid",
    "username": "zhangsan",
    "realName": "张三"
  }
}
```

`FriendRemoved` 载荷：

```json
{
  "userId": "removed-friend-user-uuid"
}
```

### 视频会议

- `MeetingInvited`：创建会议时的受邀成员或后续新增成员收到。
- `MeetingEnded`：会议结束时所有会议成员收到。

`MeetingInvited` 载荷：

```json
{
  "meetingId": "meeting-uuid",
  "inviterId": "inviter-user-uuid",
  "inviterName": "Admin User",
  "title": "项目群聊的视频会议",
  "roomId": "ff_20260716143000_abcd",
  "channelName": "ff_20260716143000_abcd",
  "status": "Active",
  "meeting": {}
}
```

`MeetingEnded` 载荷：

```json
{
  "meetingId": "meeting-uuid",
  "title": "项目群聊的视频会议",
  "status": "Ended",
  "endedAt": "2026-07-16T06:35:00Z",
  "meeting": {}
}
```

其中 `meeting` 与会议详情接口 `GET /api/v1/meetings/{id}` 的 `data` 一致，包含房间号、频道名、成员和时间信息。字段详情参见 [meetings-api.md](meetings-api.md)。事件名区分大小写，前端必须订阅 `MeetingInvited`，不能写成旧名称 `MeetingInvite`。

### 即时消息

现有事件继续保留：

- `ReceiveMessage`
- `MessageRecalled`
- `MessageReactionUpdated`
- `ConversationUpdated`
- `ConversationRemoved`
- `ConversationDissolved`
- `ConversationAnnouncementUpdated`

## 客户端恢复规则

实时推送采用尽力送达。客户端断线重连后，应重新调用对应 REST 接口拉取最新数据：

- 通知：`GET /api/v1/notifications`
- 好友申请：`GET /api/v1/contacts/requests`
- 会议：`GET /api/v1/meetings`
- 会话和消息：`GET /api/v1/im/conversations`

不要仅依赖本地收到的事件作为最终数据来源。
