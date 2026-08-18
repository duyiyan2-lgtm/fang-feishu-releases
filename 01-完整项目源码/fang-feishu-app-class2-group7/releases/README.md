# 仿飞书项目统一交付区

本目录集中保存 Android、PC、Web、后端部署包和修复记录。更新时间：2026-07-21。

准备合并到团队主仓库时，请先阅读 [合并交接说明](MERGE-GUIDE.md)。

| 模块 | 当前交付版本 | 内容 | 入口 |
| --- | --- | --- | --- |
| Android | v1.0.27 | 26 个可校验、可还原的历史 APK | [Android 版本归档](android/README.md) |
| PC | v0.4.0 | Windows x64 NSIS 安装包 | [PC 安装包](pc/README.md) |
| Web | v0.4.0 | Vite 生产构建产物 | [Web 部署包](web/README.md) |
| Backend | 2026-07-18 final | 最新运维部署压缩包 | [后端部署包](backend/README.md) |
| 修复记录 | 2026-07-14～2026-07-16 | Android、视频会议、跨端互通修复报告 | [修复记录索引](fix-records/README.md) |

## 为什么安装包使用分片归档

Android 历史 APK 共约 1.41 GiB，单个 APK 约 50～57 MiB；PC 安装包约 81 MiB。它们超过 Gitee 免费仓库的单文件限制，不能直接作为一个大文件提交。

本仓库使用以下无损方案：

- Android：按 APK 内部 ZIP 条目切分并跨版本去重，原始 1.41 GiB 缩减为约 229 MiB 共享数据块。
- PC：将安装程序切成 16 MiB 分片。
- 每个版本均保存清单、文件大小和 SHA-256。
- 仓库内提供 PowerShell 一键还原脚本；还原后会自动校验，字节内容与原安装包完全一致。

分片不是可直接安装的文件，请先按对应模块 README 还原。
