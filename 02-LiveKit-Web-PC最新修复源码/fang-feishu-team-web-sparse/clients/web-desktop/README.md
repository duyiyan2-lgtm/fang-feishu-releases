# Feishu-like Workspace

> 仿飞书风格的协作办公平台前端项目（P0/P1 功能完整 + 工程交付物齐全）

![Vue 3](https://img.shields.io/badge/Vue-3.4-42b883) ![Vite](https://img.shields.io/badge/Vite-5.2-646cff) ![TailwindCSS](https://img.shields.io/badge/Tailwind-3.4-38bdf8) ![Pinia](https://img.shields.io/badge/Pinia-2.1-yellow)

## ✨ 功能特性

### 🎯 P0 - 核心功能
- ✅ **用户中心**：登录 / 退出 / 当前用户 / JWT 鉴权 / 持久化
- ✅ **管理后台**：用户 / **部门** / 角色权限 / 操作日志
- ✅ **通讯录**：分组列表 / **部门树** / 成员搜索 / 拼音首字母 / 员工名片
- ✅ **即时通讯**：会话列表 / 单聊 / 群聊 / 历史消息 / 撤回 / 未读数
- ✅ **协同文档**：列表 / 创建 / 编辑 / 保存 / 富文本 / **评论** / **版本历史**
- ✅ **云盘**：文件列表 / 网格视图 / 类型筛选 / 存储条
- ✅ **部署**：Docker Compose 一键启动 + Nginx 配置 + 完整文档

### 🎯 P1 - 增强功能
- ✅ **通知中心**：5 种类型 + 实时未读数 + 一键标已读
- ✅ **日历**：月 / 周 / 日三视图 + 事件时间轴 + 拖动定位
- ✅ **审批（OA）**：6 种审批类型 + 自定义表单 + 加签 / 转交 / 驳回
- ✅ **应用中心**：12 个应用 + 安装 / 卸载 + **拖拽配置工作台**
- ✅ **开放平台**：API 文档（12 个接口）+ Webhook + 应用鉴权

### 💎 工程亮点
- 🌗 **暗色模式**：基于 `class` 切换 + 持久化 + 系统偏好
- 📱 **响应式**：从桌面到平板均可访问
- 🔌 **可切换 Mock / 真实 API**：`VITE_USE_MOCK` 环境变量控制
- 📦 **可拆分的打包产物**：vue / icons / editor / utils 各自独立 chunk
- 🎨 **Tailwind 主色定制**：完整 `#3370FF` 飞书蓝设计令牌

## 🚀 快速开始

### 本地开发

```bash
# 1. 安装依赖（推荐国内镜像）
npm install --registry=https://registry.npmmirror.com

# 2. 启动 dev server
npm run dev

# 默认访问 http://localhost:5173
# 如果 5173 被占用，Vite 自动跳到 5180 / 5181 ...
```

**测试账号**：任意手机号 / 邮箱 + 4 位以上密码（如 `13800001111 / 1234`）

### Docker 部署

```bash
# 1. 构建前端产物（容器内会做，无需本地执行）
docker compose up -d --build

# 2. 查看状态
docker compose ps

# 3. 查看日志
docker compose logs -f web

# 4. 访问 http://localhost:8080

# 5. 停止
docker compose down
```

## 📂 目录结构

```
feishu-like-workspace/
├── 📁 src/
│   ├── api/                  # API 封装 + Mock 数据 + 拦截器
│   ├── components/           # 通用组件（Sidebar / TopBar / RichEditor ...）
│   ├── router/               # 路由 + 鉴权守卫
│   ├── stores/               # Pinia（user / theme / messages）
│   ├── utils/                # 工具（pinyin / dayjs / websocket）
│   ├── views/
│   │   ├── Admin/            # 管理后台（users/roles/depts/dict/logs）
│   │   ├── AppCenter/        # 应用中心（market/workbench）
│   │   ├── Approval/         # 审批（list/detail/create）
│   │   ├── Platform/         # 开放平台（api/webhook/apps）
│   │   └── *.vue             # 业务页面
│   ├── App.vue
│   ├── main.js
│   └── style.css
├── 📁 docs/                  # 工程文档（7 篇）
│   ├── 01-architecture.md    # 架构方案
│   ├── 02-requirements.md    # 需求规格
│   ├── 03-database.md        # 数据库设计
│   ├── 04-api.md             # 接口文档
│   ├── 05-testing.md         # 测试用例
│   ├── 06-deployment.md      # 部署指南
│   └── 07-retrospective.md   # 项目复盘
├── 📁 dist/                  # 构建产物（git 忽略）
├── Dockerfile                # 多阶段构建
├── docker-compose.yml        # 服务编排
├── nginx.conf                # Nginx 配置（SPA fallback + gzip）
├── .dockerignore
├── vite.config.js
├── tailwind.config.js
└── package.json
```

## 🛠 关键脚本

```bash
npm run dev        # 启动开发服务器（带 HMR）
npm run build      # 构建生产产物
npm run preview    # 本地预览构建结果（4173 端口）
```

## 🔧 环境变量

`.env.development`（默认配置）
```bash
VITE_API_BASE=/api              # 后端 API 前缀
VITE_WS_URL=ws://host/ws        # WebSocket 地址
VITE_API_PROXY_TARGET=...       # Vite 代理目标
```

`.env.production`（生产模板，按需创建）
```bash
VITE_API_BASE=https://api.example.com
VITE_WS_URL=wss://api.example.com/ws
VITE_USE_MOCK=false             # 关闭 Mock 走真实 API
```

## 📖 文档导航

| 文档 | 说明 |
|---|---|
| [docs/01-architecture.md](docs/01-architecture.md) | 系统架构 + 模块划分 + 技术选型 |
| [docs/02-requirements.md](docs/02-requirements.md) | 完整功能需求 + 验收标准 |
| [docs/03-database.md](docs/03-database.md) | 数据模型 + ER 图 + 建表 SQL |
| [docs/04-api.md](docs/04-api.md) | 25+ 个 REST 接口设计 |
| [docs/05-testing.md](docs/05-testing.md) | 单元 / E2E / 性能测试用例 |
| [docs/06-deployment.md](docs/06-deployment.md) | Docker / Nginx / CI/CD |
| [docs/07-retrospective.md](docs/07-retrospective.md) | 项目复盘 + 经验沉淀 |

## 📄 License

MIT © 2024 Feishu Workspace Team
