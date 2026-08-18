# Meetings GET/POST 500 故障定位与修复报告

日期：2026-07-16  
环境：`alxy.fun` 同构 Docker API + PostgreSQL 16 隔离复现  
影响接口：`GET /api/v1/meetings`、`POST /api/v1/meetings`

## 一、结论

本次空响应 500 的根因不是 SignalR，也不是 `MeetingInvited` / `MeetingEnded` 发布逻辑。

会议成员响应中同时存在以下两个 CLR 匿名属性：

```text
UserName
Username
```

后端使用 Web JSON 配置（camelCase，并启用大小写不敏感属性处理）。序列化会议响应时，这两个属性产生名称冲突，`System.Text.Json` 抛出 `InvalidOperationException`，响应尚未写出就被中断，因此客户端看到 HTTP 500 且 body 为空。

POST 的数据库写入已经成功，异常发生在序列化返回值阶段。所以第一次 POST 虽然显示失败，会议记录仍可能已写入；之后 GET 查询到该记录时又触发同一序列化异常。

## 二、为何没有执行 `git revert HEAD`

当前仓库 HEAD 为：

```text
e403c187e2f79d45f3532781d4b89cc94f887de9
feat: 新增微信小程序端支持
日期：2026-07-02
```

整个 `backend/` 目录目前不在 Git 跟踪范围内，`MeetingsController.cs` 在 HEAD 中不存在，也没有可用的文件提交历史。因此：

- `git revert HEAD` 不会恢复会议控制器；
- 它会尝试撤销无关的小程序提交；
- 在当前大量未跟踪文件的工作树中执行存在误回滚风险。

基于上述证据，本次没有执行 `git revert HEAD`。

## 三、复现证据

使用当前 Dockerfile 构建 Release 镜像，连接独立 PostgreSQL 16，依次执行：

1. `GET /health`：200。
2. admin 登录：成功。
3. 首次 `GET /api/v1/meetings`（数据库为空）：200。
4. `POST /api/v1/meetings`：数据库 INSERT 成功，但响应 500、body 为空。
5. 再次 `GET /api/v1/meetings`：500、body 为空。

容器日志关键异常：

```text
System.InvalidOperationException:
The JSON property name ... username collides with another property.
```

堆栈位置为 `SystemTextJsonOutputFormatter.WriteResponseBodyAsync`，说明错误发生在 HTTP JSON 输出阶段，而不是数据库查询或 SignalR 推送阶段。

## 四、修复方式

会议成员响应改为使用带明确键名的字典，继续保持前端现有契约：

```json
{
  "userId": "...",
  "userName": "Admin User",
  "username": "admin",
  "avatarUrl": null
}
```

这样既不改变 Android/PC/Web 已使用的字段，又不会触发 CLR 属性元数据冲突。

SignalR 逻辑保持不变：

- 创建会议或追加邀请后发送 `MeetingInvited`；
- 结束会议后发送 `MeetingEnded`；
- 实时发布失败采用尽力投递，不回滚已经成功的数据库事务。

## 五、验证结果

### 自动化测试

```text
dotnet test FangFeishu.Backend.sln -c Release
通过：33
失败：0
```

新增的回归断言会使用生产同款 Web JSON 选项，真实序列化 POST 和 GET 返回值，并确认 `userName`、`username`、`avatarUrl` 可同时输出。

### Docker + PostgreSQL HTTP e2e

```text
GET 已有会议：200
POST 创建会议：201
GET 创建后列表：200
响应包含 userName + username：是
API 错误日志：无
```

### SignalR e2e

真实建立 user_a SignalR 长连接后：

```text
连接状态：Connected
admin 创建会议：201
user_a 收到 MeetingInvited：是
admin 结束会议：200
user_a 收到 MeetingEnded：是
两个事件 meetingId：正确
```

## 六、部署与服务器日志

部署新包后，在 `alxy.fun` 服务器执行：

```bash
docker logs fang-feishu-api --tail 200
```

本机 Docker 日志不等于生产服务器日志；必须在实际运行 `fang-feishu-api` 的服务器上执行。部署后重点确认日志中不存在：

```text
property name ... collides with another property
Unhandled exception
```

随后依次复测登录、GET 会议列表、POST 创建会议、会议邀请和结束事件。

## 七、数据库说明

- 本次修复不涉及数据库结构变更；
- 不需要执行 SQL 或 EF Migration；
- 故障期间返回 500 的 POST 可能已经写入会议记录，部署后应检查并清理测试产生的重复会议。
