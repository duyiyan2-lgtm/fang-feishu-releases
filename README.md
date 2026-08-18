# 仿飞书项目发布仓库

本仓库存放仿飞书协同办公平台的**最新安装包、部署包、项目资料和作品集**。

归档来源：`仿飞书项目-最新版-20260813`  
上传日期：2026-08-18

## 下载安装包

| 端 | 文件 | 说明 |
| --- | --- | --- |
| Android | [fang-feishu-android-v1.0.27-feishu.zhuyiyuan9.top-20260730-test.apk](03-最新安装包和部署包/Android/fang-feishu-android-v1.0.27-feishu.zhuyiyuan9.top-20260730-test.apk) | Android 测试包 v1.0.27 |
| Web | [FangFeishu-Web-v0.4.0-20260812-hotfix-r2.zip](03-最新安装包和部署包/Web/FangFeishu-Web-v0.4.0-20260812-hotfix-r2.zip) | Web 热修复包 v0.4.0 |
| PC | [FangFeishu-PC-0.5.1-x64.exe](03-最新安装包和部署包/PC/FangFeishu-PC-0.5.1-x64.exe) | PC 完整项目版安装包 |
| PC | [FangFeishu-PC-Portable-0.5.1.exe](03-最新安装包和部署包/PC/FangFeishu-PC-Portable-0.5.1.exe) | PC 便携版 |
| PC | [FangFeishu-PC-Setup-v0.4.0.exe](03-最新安装包和部署包/PC/FangFeishu-PC-Setup-v0.4.0.exe) | PC LiveKit 修复版安装包 |
| 后端 | [fang-feishu-backend-ops-20260718-final.zip](03-最新安装包和部署包/Backend/fang-feishu-backend-ops-20260718-final.zip) | 后端运维部署包 |

SHA-256 校验见 [`SHA256SUMS.txt`](SHA256SUMS.txt)。

## 仓库目录

- `01-完整项目源码/`：完整项目工作区（Web/PC 源码、文档、历史发布分片）
- `02-LiveKit-Web-PC最新修复源码/`：2026-08-12 LiveKit / Web / PC 修复工作区
- `03-最新安装包和部署包/`：Android / Web / PC / Backend 最新交付文件
- `04-项目资料/`：项目汇报资料与 PPT
- `05-HR面试展示图片/`：面试展示图
- `06-HR作品集展示页-可直接发送/`：可直接发送的作品集页面
- `README-项目索引.md`：本地归档原始索引（含源码 Git 状态）

## 源码说明

`01`、`02` 的源码和可入库发布物已上传。以下内容因超过 GitHub 单文件 100MB 限制、或可用命令恢复，未放入本仓库：

- `.git/` 历史包（01 的 pack 约 247MB）
- `node_modules/`（含 172MB 的 `electron.exe`，可在对应目录执行 `npm install`）
- `clients/web-desktop/release/win-unpacked/`（未打包运行目录，含超过 100MB 的 exe）

完整 Git 历史仍在 Gitee：

- 完整项目：https://gitee.com/du-yiyan/fang-feishu-app-class2-group7
- LiveKit 修复工作区：https://gitee.com/grade24-fullstack-class2/fang-feishu-app-class2-group7（分支 `fix/web-livekit-realtime-20260811`）
