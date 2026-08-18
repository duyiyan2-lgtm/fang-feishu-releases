# Web 端生产构建

当前版本：v0.4.0。

部署内容位于 [`v0.4.0/dist`](v0.4.0/dist)，已于 2026-07-21 使用 Vite 5.4.21 完成生产构建。

## 部署方式

将 `dist` 目录中的全部文件复制到 Nginx/静态站点根目录。单页应用需要将未知前端路由回退到 `index.html`，API 与 SignalR 地址按部署环境配置。

## 重新构建

```powershell
cd .\clients\web-desktop
npm ci
npm run build
```

构建时出现的单块体积超过 500 kB 提示为性能建议，不影响本次构建成功；后续可继续通过路由懒加载拆包优化。
