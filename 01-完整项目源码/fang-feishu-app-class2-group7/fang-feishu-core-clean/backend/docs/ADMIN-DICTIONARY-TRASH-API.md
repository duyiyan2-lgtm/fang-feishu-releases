# 角色权限、数据字典与文档回收站接口说明

所有接口均使用 `/api/v1` 前缀和 camelCase JSON。角色与数据字典写操作需要管理员权限。

## 一、角色权限

### 创建或更新角色

```json
{
  "roleName": "项目管理员",
  "roleCode": "project_admin",
  "description": "负责项目配置",
  "permissions": ["project.read", "project.write", "meeting.manage"]
}
```

- `POST /api/v1/roles`
- `PUT /api/v1/roles/{id}`
- `GET /api/v1/roles`
- `GET /api/v1/roles/{id}`
- `DELETE /api/v1/roles/{id}`

独立更新权限：

```http
PUT /api/v1/roles/{id}/permissions
```

```json
{
  "permissions": ["project.read", "meeting.manage"]
}
```

## 二、数据字典

分类接口：

- `GET /api/v1/dict/categories`
- `GET /api/v1/dict/categories/{code}`
- `POST /api/v1/dict/categories`
- `PUT /api/v1/dict/categories/{code}`
- `DELETE /api/v1/dict/categories/{code}`

分类请求示例：

```json
{
  "code": "approval_status",
  "name": "审批状态",
  "description": "审批流程状态",
  "isEnabled": true
}
```

字典项接口：

- `GET /api/v1/dict/categories/{code}/items`
- `POST /api/v1/dict/categories/{code}/items`
- `PUT /api/v1/dict/categories/{code}/items/{itemId}`
- `DELETE /api/v1/dict/categories/{code}/items/{itemId}`

字典项请求示例：

```json
{
  "code": "approved",
  "label": "已通过",
  "value": "Approved",
  "description": null,
  "sortOrder": 1,
  "isEnabled": true
}
```

管理员读取停用数据时可以追加 `?includeDisabled=true`。

## 三、文档回收站

- `DELETE /api/v1/documents/{id}`：移入回收站。
- `GET /api/v1/documents/trash`：列出当前用户已删除文档；管理员可查看全部。
- `POST /api/v1/documents/{id}/restore`：恢复文档。
- `DELETE /api/v1/documents/{id}/permanent`：彻底删除。
- `GET /api/v1/documents?includeDeleted=true`：同时查看正常和已删除文档。

普通用户只能恢复或彻底删除自己拥有的文档，管理员可以处理全部文档。

## 四、会话创建约定

前端创建单聊时应传目标联系人 ID，不要传当前用户自己的 ID：

```json
{
  "type": "Private",
  "title": null,
  "memberUserIds": ["目标联系人的用户 ID"]
}
```

后端兼容 `Private` 和 `Single`，统一存储为 `Single`。重复创建同一单聊时返回已有会话。

