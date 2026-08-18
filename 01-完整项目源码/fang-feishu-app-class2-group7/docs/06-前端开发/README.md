# 06 · 前端开发

## 🎯 阶段目标

按模块/页面/组件**逐个交付** Vue 3 + Tailwind CSS 实现，每个产出物必须配套**完整、详实、带配图**的开发文档。

## 📝 必含章节（每篇前端开发文档）

| 章节 | 说明 | 必含配图 |
| :--: | :-- | :--: |
| 一、功能概述 | 实现哪个需求（关联需求文档） | ❌ |
| 二、技术方案 | 组件拆分、状态管理、路由设计 | ✅ |
| 三、目录结构 | 文件树、命名说明 | ✅ |
| 四、核心实现 | 关键代码 + 注释 | ✅ |
| 五、组件复用 | 复用了哪些组件 / 工具 | ❌ |
| 六、运行截图 | 主要功能截图（多状态） | ✅ |
| 七、性能优化 | 懒加载 / 防抖 / 缓存 / 骨架屏 | ✅ |
| 八、踩坑与复盘 | 问题 + 解决方案 | ❌ |

## 📂 命名规范

```
06-前端开发-<模块名>-<组件/页面/模块名>.md
```

示例：
- `06-前端开发-IM-消息列表组件.md`
- `06-前端开发-登录-SSO接入.md`
- `06-前端开发-PC客户端-Electron打包.md`

## 🖼 配图要求

- 组件结构图 / 状态流转图
- 代码截图（关键逻辑，红框标注）
- **运行截图 ≥ 3 张**（多状态：加载/成功/空/错误）
- 移动端需含 iOS + Android 真机截图
- **每篇 ≥ 8 张配图**（前端文档配图要求最高）

## 📦 产出物清单

- [ ] PC 网页端（`frontend/pc-web/`）
- [ ] PC 客户端（`frontend/pc-client/`，Electron / Tauri）
- [ ] 移动 H5（`frontend/mobile-h5/`）
- [ ] 移动 App（`frontend/mobile-app/`，Uni-app / Taro 编译 iOS + Android）
- [ ] 自研组件库（`frontend/packages/ui/`）
- [ ] 公共工具 / Hooks（`frontend/packages/utils/`）

## ✍️ 文档模板

```markdown
# <模块名> - <组件/页面/功能>

## 一、功能概述
- 关联需求：[01-需求分析-xxx.md](../01-需求分析/xxx.md)
- 关联原型：[02-原型设计-xxx.md](../02-原型设计/xxx.md)
- 关联接口：[05-接口设计-xxx.md](../05-接口设计/xxx.md)

## 二、技术方案
（插入组件树 / 状态流转图）

## 三、目录结构
```
frontend/pc-web/src/views/xxx/
├── index.vue
├── components/
│   ├── XxxList.vue
│   └── XxxForm.vue
├── composables/
│   └── useXxx.ts
└── stores/
    └── xxx.ts
```

## 四、核心实现
（关键代码 + 红框截图）

## 五、组件复用
- [XxxButton](../components/XxxButton.md) - 通用按钮
- [useRequest](../composables/useRequest.md) - 请求 hook

## 六、运行截图
（多状态截图：默认/加载/空/错误）

## 七、性能优化
- 列表虚拟滚动：使用 vue-virtual-scroller
- 图片懒加载：IntersectionObserver
- 防抖搜索：300ms

## 八、踩坑与复盘
| 问题 | 原因 | 解决方案 |
```
