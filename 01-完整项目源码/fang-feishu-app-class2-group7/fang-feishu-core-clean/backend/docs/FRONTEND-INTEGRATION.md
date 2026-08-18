# 前端接口联调说明

## JSON 字段命名

HTTP JSON 契约统一使用 `camelCase`。服务端属性匹配不区分大小写，因此旧客户端传入 PascalCase 仍可兼容，但新代码应统一发送 camelCase。

### 注册

```http
POST /api/v1/auth/register
Content-Type: application/json
```

```json
{
  "username": "new_user",
  "password": "secret123",
  "realName": "New User",
  "email": "new.user@example.com",
  "phone": null,
  "clientType": "Web"
}
```

校验规则：

- `username` 长度为 3–64，只允许字母、数字、下划线和连字符。
- `password` 至少 6 位。
- `realName` 长度为 1–64。

### 创建群聊

```http
POST /api/v1/im/conversations
Authorization: Bearer {JWT}
Content-Type: application/json
```

```json
{
  "type": "Group",
  "title": "项目群聊",
  "memberUserIds": [
    "00000000-0000-0000-0000-000000000001"
  ]
}
```

注意：

- 请求体必须是上面的扁平结构，不要额外包裹 `{ "request": { ... } }`。
- `memberUserIds` 的每个值都必须是合法 UUID。
- 当前登录用户无需放入 `memberUserIds`，服务端会自动加入。
- `title` 已由接口支持，不会因为字段本身导致 JSON 转换错误。

## SignalR

SignalR Hub 不会显示在 Swagger。连接方式和全部事件名参见 [REALTIME-EVENTS.md](REALTIME-EVENTS.md)。

## 400 错误排查

出现 400 时请同时保留以下信息，避免只根据字段名猜测：

1. 完整 URL 和 HTTP 方法。
2. 实际发送的原始 JSON 请求体。
3. 响应状态码和完整响应体。
4. Swagger 或浏览器 Network 中的 `traceId`。

如果响应中出现 `The JSON value could not be converted to System.Guid`，应优先检查 UUID，而不是修改 DTO 字段大小写。
