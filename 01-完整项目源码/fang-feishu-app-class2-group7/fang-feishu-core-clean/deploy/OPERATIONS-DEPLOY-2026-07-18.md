# 仿飞书后端运维部署说明（2026-07-18）

## 一、本次修复内容

- 修复 `POST /api/v1/im/conversations` 对 `Private`/`Single` 类型处理不一致的问题。
- 私聊只接受一个其他用户，重复发起同一私聊时返回已有会话，避免重复会话。
- PostgreSQL 瞬时故障增加 3 次自动重试。
- 全局未处理异常统一返回 JSON，包含业务码 `5000` 和 `traceId`，不再返回空响应体。
- SignalR 通知推送失败不再把已成功提交的数据库操作误报为 HTTP 500。
- SignalR 增加 15 秒 KeepAlive、45 秒客户端超时和 15 秒握手超时。
- 角色接口增加 `permissions: string[]` 持久化及独立权限更新接口。
- 增加数据字典分类和字典项 CRUD 接口。
- 文档删除改为软删除，增加回收站、恢复及彻底删除接口。
- `/health` 现在检查数据库连接；新增 `/health/live` 进程存活检查。

## 二、数据库变更

应用启动时会由 `DbSeeder` 幂等执行以下变更：

- `roles` 增加 `PermissionsJson`。
- `documents` 增加 `IsDeleted`、`DeletedAt`、`DeletedBy`。
- 新增 `dictionary_categories` 和 `dictionary_items` 表及索引。

部署前必须备份 PostgreSQL 数据库。上述变更使用 `ADD COLUMN IF NOT EXISTS` 和 `CREATE TABLE IF NOT EXISTS`，可重复执行。

## 三、部署命令

1. 备份服务器当前代码、数据库及 `deploy/docker/.env.prod`。
2. 解压本次完整后端包，不要单独复制某一个 DLL。
3. 不要用包内 `.env.prod.example` 覆盖服务器已有配置。
4. 在解压目录执行：

```bash
docker compose \
  --env-file deploy/docker/.env.prod \
  -f deploy/docker/docker-compose.prod.yml \
  up -d --build api
```

5. 检查启动和数据库建表日志：

```bash
docker logs fang-feishu-api --tail 500
```

## 四、健康检查

```bash
curl -i http://127.0.0.1:5080/health/live
curl -i http://127.0.0.1:5080/health
```

- `/health/live`：进程存活即返回 200。
- `/health`：应用和 PostgreSQL 均可用时返回 200；数据库不可用时返回结构化 503。

## 五、前端复测清单

1. 从 `GET /api/v1/contacts` 选择另一个真实联系人。
2. 使用 `POST /api/v1/im/conversations` 和 `type=Private` 创建私聊，预期首次 201、重复请求 200 且返回同一个会话 ID。
3. 只传当前用户自己时应返回 400、业务码 `1501`，这是正确校验。
4. 创建和修改角色，刷新或更换浏览器后 `permissions` 仍应保留。
5. 完成数据字典分类、字典项的新增、修改、停用和删除测试。
6. 删除文档后应进入 `/api/v1/documents/trash`，可恢复；彻底删除后不可恢复。
7. 连续请求联系人、部门、文档和文件接口，若发生错误，响应必须包含 `traceId`。
8. 保持 SignalR 在线，验证消息和通知无需退出页面即可刷新。

## 六、Nginx 要求

`/hubs/im` 必须保留 WebSocket Upgrade：

```nginx
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";
proxy_read_timeout 3600s;
proxy_send_timeout 3600s;
```

配置修改后执行：

```bash
nginx -t
systemctl reload nginx
```

## 七、回滚提醒

- 新增字段和字典表不会破坏旧程序，但旧程序不认识文档软删除字段。
- 如果已经有文档进入回收站，不建议直接回滚到旧程序，否则旧程序可能重新展示已删除文档。
- 出现问题时优先保留本版数据库结构，只回滚应用镜像，并立即联系后端处理。

## 八、验证结果

- Release 编译通过。
- 后端自动化测试 41/41 通过。
- 包内包含完整源码、测试、Docker Compose、Nginx 配置和本部署说明。

