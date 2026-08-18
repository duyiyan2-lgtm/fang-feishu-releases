# 仿飞书工作台 · Electron PC 客户端

基于现有 Vue 3 + Vite Web 端封装的桌面应用（Windows / macOS / Linux）。

## 快速开始

```powershell
cd clients\web-desktop
npm install

# Web 开发（浏览器）
npm run dev

# PC 客户端开发（窗口 + 热更新）
npm run electron:dev

# 先构建再本地预览桌面端
npm run electron:preview

# 打包 Windows 安装包 + 便携版
npm run electron:build:win
```

产物目录：`clients/web-desktop/release/`

| 文件 | 说明 |
|------|------|
| `FangFeishu-PC-0.5.0-x64.exe` | NSIS 安装程序 |
| `FangFeishu-PC-Portable-0.5.0.exe` | 免安装便携版 |

## Web 流畅度相关改动

- 主布局 `keep-alive` 缓存高频模块（消息/日历/文档等），切换不白屏、少重复请求
- 列表 `content-visibility` + 滚动容器 `contain`，长列表更顺滑
- 路由懒加载 + Vite 分包（Vue / Agora / SignalR 分离）
- 动画仅用 `opacity` / `transform`，遵循 `prefers-reduced-motion`
- 侧边栏可折叠，减少主内容区挤压

## 桌面端能力

| 能力 | 说明 |
|------|------|
| 启动动画 | `electron/splash.html` 启动屏，主窗口就绪后淡出 |
| 系统托盘 | 关闭窗口进托盘；双击托盘恢复；托盘菜单可退出 |
| 无边框窗口 | 自定义标题栏（最小化 / 最大化 / 关闭） |
| 单实例 | 二次启动聚焦已有窗口 |
| 应用图标 | `build/icon.ico` / `build/icon.png` |
| 外部链接 | 系统浏览器打开 |
| IPC | `window.electronAPI.*` |

```js
// 渲染进程
await window.electronAPI.getVersion()
window.electronAPI.windowControl('minimize') // maximize | close
```

## 常见问题

**打包卡住下载 Electron**

```powershell
$env:ELECTRON_MIRROR = "https://npmmirror.com/mirrors/electron/"
npm run electron:build:win
```

**跳过代码签名**

脚本已默认 `CSC_IDENTITY_AUTO_DISCOVERY=false`。

**开发模式要 DevTools**

`electron:dev` 默认打开；生产打包不打开。
