# 仿飞书后端运维部署说明（Operations Deployment Guide，2026-07-16）

## 本次内容

- 修复 `GET/POST /api/v1/meetings` 空 body 500：`UserName` 与 `Username` 在 Web JSON 配置下发生序列化名称冲突；修复后继续兼容 `userName` 与 `username` 两个前端字段。
- 补齐通知、好友和视频会议的 SignalR 实时事件。
- 修正视频会议邀请事件名为前端约定的 `MeetingInvited`，并补齐 `meetingId`、邀请人和完整会议信息。
- `MeetingEnded` 增加明确的 `meetingId`，重复结束会议不会重复推送。
- 会议邀请限制为创建人/管理员，已结束会议禁止继续邀请；会议状态筛选兼容大小写和 `All`。
- 会议成员响应增加 `avatarUrl`，用于手机端关闭摄像头后显示正确头像。
- 会议成员的 `rtcIdentities` 增加 `Legacy` 历史 UID，并保留 Android/Desktop/Web/MiniProgram 四端 UID，修复新旧服务滚动部署时参会人只显示数字、头像不一致的问题。
- 明确 HTTP JSON 使用 camelCase，并兼容大小写。
- 修复群聊和注册接口契约说明。
- 后端 Release 测试 33/33 通过。
- 本次没有数据库表结构变化，不需要执行 SQL 或 EF Migration。

## 部署前

1. 备份服务器当前代码和 `.env.prod`。
2. 不要使用包内的 `.env.prod.example` 覆盖服务器已有的 `.env.prod`。
3. 确认 `.env.prod` 中 PostgreSQL、JWT、MinIO、Agora 和 CORS 配置完整。

## Docker Compose 部署

在解压后的项目根目录执行：

```bash
docker compose \
  --env-file deploy/docker/.env.prod \
  -f deploy/docker/docker-compose.prod.yml \
  up -d --build api
```

首次部署且服务器没有 `.env.prod` 时：

```bash
cp deploy/docker/.env.prod.example deploy/docker/.env.prod
```

随后必须修改其中的密码、域名、JWT Secret 和 Agora 配置，再启动服务。

## Nginx

参考 `deploy/nginx/fang-feishu-api.conf`。必须保留 `/hubs/im` 下列设置，否则 SignalR WebSocket 无法正常连接：

```nginx
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";
proxy_read_timeout 3600s;
proxy_send_timeout 3600s;
```

修改配置后执行：

```bash
nginx -t
systemctl reload nginx
```

## 部署验证

```bash
docker compose \
  --env-file deploy/docker/.env.prod \
  -f deploy/docker/docker-compose.prod.yml \
  ps

docker logs --tail 200 fang-feishu-api

curl -i http://127.0.0.1:5080/health
```

预期 `/health` 返回 HTTP 200。随后请由前端验证：

1. 注册和登录。
2. 创建多人群聊并发送消息。
3. 好友申请、接受和拒绝实时刷新。
4. 视频会议邀请和结束实时刷新。
5. 通知中心无需退出页面即可收到新通知。
6. 使用 admin 与 user_a 两台 Android 手机加入同一会议，双方都应显示对方用户名；关闭镜头后头像应与个人资料一致，不应显示“参会人 + 数字 UID”。

前端订阅的会议邀请事件必须写成 `MeetingInvited`（区分大小写）。旧名称 `MeetingInvite` 已停用。

部署后请额外执行：

```bash
docker logs fang-feishu-api --tail 200
```

日志中不应再出现 `property name ... collides with another property`。随后使用登录 Token 验证 `GET /api/v1/meetings` 返回 200、`POST /api/v1/meetings` 返回 201。

SignalR 事件载荷见 `backend/docs/REALTIME-EVENTS.md`，HTTP 请求示例见 `backend/docs/FRONTEND-INTEGRATION.md`。
