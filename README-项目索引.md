# 仿飞书项目最新版归档

迁移日期：2026-08-13

## 目录说明

- `01-完整项目源码/fang-feishu-app-class2-group7`：完整项目仓库，包含 Android、Web、PC、后端、文档、工具和历史发布资料。
- `02-LiveKit-Web-PC最新修复源码/fang-feishu-team-web-sparse`：2026-08-12 最新 LiveKit/Web/PC 修复工作区，保留完整 Git 信息与构建依赖。
- `03-最新安装包和部署包`：按 Android、Web、PC、Backend 分类的最新版交付文件。
- `04-项目资料/fang_feishu_report`：项目汇报生成资料。
- `04-项目资料/项目汇报PPT`：5 份项目汇报成品 PPT，迁移后均已通过 SHA-256 校验。

## Git 状态

### 完整项目源码

- 分支：`master`
- 提交：`1e9ab4cd90955f3f479b21fe26844764f95caa4f`
- 远程仓库：`https://gitee.com/du-yiyan/fang-feishu-app-class2-group7.git`
- 迁移时存在 35 项尚未提交的修改/新增文件，已完整原样保留。

### LiveKit 最新修复源码

- 分支：`fix/web-livekit-realtime-20260811`
- 提交：`a7588c83aab91d6642ebf9606c49c23fcc2a0c18`
- 远程仓库：`https://gitee.com/grade24-fullstack-class2/fang-feishu-app-class2-group7.git`
- 迁移时工作区干净。

## 最新交付文件

- Android：`fang-feishu-android-v1.0.27-feishu.zhuyiyuan9.top-20260730-test.apk`
- Web：`FangFeishu-Web-v0.4.0-20260812-hotfix-r2.zip`
- PC 完整项目版：`FangFeishu-PC-0.5.1-x64.exe`
- PC 便携版：`FangFeishu-PC-Portable-0.5.1.exe`
- PC LiveKit 修复版：`FangFeishu-PC-Setup-v0.4.0.exe`
- 后端：`fang-feishu-backend-ops-20260718-final.zip`

各文件 SHA-256 见 `SHA256SUMS.txt`。

## 校验结果

- 完整项目：27,516 个文件，约 1.72 GB；第二遍复制检查为 0 个复制、0 个差异、0 个失败。
- LiveKit 修复源码：26,688 个文件，约 951.30 MB；第二遍复制检查为 0 个复制、0 个差异、0 个失败。
- 两套源码的分支、提交号、远程地址与 Git 工作区状态均与迁移前一致。

## 使用提醒

日常继续开发时，优先确认要在“完整项目源码”还是“LiveKit 最新修复源码”上工作。完整项目仍有未提交修改，建议先执行 `git status` 检查并提交到新分支；不要直接丢弃这些修改。
