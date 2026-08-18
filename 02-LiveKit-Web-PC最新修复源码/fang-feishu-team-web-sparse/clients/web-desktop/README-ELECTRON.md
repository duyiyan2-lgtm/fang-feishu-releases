# Electron 桌面端使用说明

> 把现有的 Vue 3 + Vite 项目封装成 PC 桌面应用。
> **Web 代码完全没动**，只新增了 `electron/` 目录和修改了 `package.json`。

## 🚀 快速上手

### 1. 安装 Electron 依赖

```bash
cd "C:/Users/汪炜康/feishu-like-workspace"
npm install
```

会自动安装：
- `electron` - Electron 主进程
- `electron-builder` - 打包工具
- `concurrently` - 同时跑多条命令
- `wait-on` - 等待端口 ready
- `cross-env` - 跨平台环境变量

### 2. 开发模式（桌面窗口里调试）

```bash
npm run electron:dev
```

会自动：
1. 启动 Vite dev server（监听 5173 端口）
2. 等 Vite ready 后，启动 Electron 桌面窗口
3. 窗口会自动加载 `http://localhost:5173`
4. DevTools 自动开启（detached 模式）

> 💡 如果 5173 端口被其他项目占用，Electron 会自动扫描 5180-5185 端口找 Vite。

### 3. 预览打包后的应用

```bash
npm run electron:preview
```

1. `vite build` 生成 `dist/`
2. Electron 加载 `dist/index.html`
3. 没有 DevTools（生产模式）

### 4. 打包成桌面安装程序

#### 当前平台打包

```bash
npm run electron:build
```

#### 指定平台

```bash
# Windows: 生成 .exe 安装程序（NSIS）
npm run electron:build:win

# macOS: 生成 .dmg（需要 macOS 机器）
npm run electron:build:mac

# Linux: 生成 .AppImage
npm run electron:build:linux
```

打包完成后，产物在 `release/` 目录：

```
release/
├── Feishu Workspace-Setup-0.4.0.exe      ← Windows 安装包
├── Feishu Workspace-0.4.0-x64.dmg       ← macOS 安装包
└── Feishu Workspace-0.4.0.AppImage      ← Linux 安装包
```

## 📁 新增/修改文件

```
项目根/
├── electron/                  ← 新增目录
│   ├── main.js                 ← Electron 主进程（窗口、菜单、IPC）
│   └── preload.js              ← 安全 IPC 桥（暴露 window.electronAPI）
├── electron-builder.yml       ← 打包配置（可放 package.json 里）
├── package.json                ← 修改：加 main / scripts / devDependencies / build
├── .gitignore                  ← 修改：加 release/ out/
└── src/                        ← 完全没动！
```

## 🎮 主进程功能（main.js）

| 功能 | 说明 |
|---|---|
| 智能找端口 | 自动扫描 5173 → 5180-5185 找 Vite |
| 文件菜单 | 新建窗口、退出 |
| 编辑菜单 | 撤销、重做、复制粘贴 |
| 视图菜单 | 重载、DevTools、缩放、全屏 |
| 帮助菜单 | 关于对话框（显示版本、平台）|
| 外部链接 | `window.open` 自动转系统浏览器 |
| IPC | `window.electronAPI.getVersion()` 等 |

## 🔌 渲染进程访问原生能力

`window.electronAPI` 已暴露：

```js
// 在任何 Vue 组件里
const version = await window.electronAPI.getVersion()
console.log('Electron version:', version)

await window.electronAPI.openExternal('https://github.com')
```

> ⚠️ 当前 Vue 代码不需要改任何东西。如需调用系统 API，直接用 `window.electronAPI.xxx`。

## 📦 打包配置说明

| 配置项 | 值 | 含义 |
|---|---|---|
| `appId` | `com.feishu.workspace` | 应用唯一标识 |
| `productName` | `Feishu Workspace` | 显示名称 |
| `asar` | `true` | 源码压缩 |
| `directories.output` | `release` | 打包输出目录 |
| `win.target` | `nsis` | Windows 用 NSIS 安装包 |
| `mac.target` | `dmg` | macOS 用 DMG |
| `linux.target` | `AppImage` | Linux 用 AppImage |

## 🐛 常见问题

### Q: 启动后白屏？
- 确认 `npm run dev` 已运行（开发模式）
- 或 `npm run build` 已生成 `dist/`（生产模式）

### Q: 打包时下载 Electron 卡住？
- 设置镜像：`set ELECTRON_MIRROR=https://npmmirror.com/mirrors/electron/`
- 或使用 vpn

### Q: Windows 打包失败说"代码签名"？
- 开发阶段可以加 `CSC_IDENTITY_AUTO_DISCOVERY=false` 跳过签名

### Q: 主进程修改后没生效？
- Electron 主进程不会热更新，需要重启 `npm run electron:dev`

### Q: 想自定义图标？
- 把 `.ico`（Windows）/ `.icns`（mac）/ `.png`（Linux）放进 `build/` 目录
- electron-builder 自动识别