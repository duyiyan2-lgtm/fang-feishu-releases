# 合并交接说明

## 分支信息

- 来源仓库：`du-yiyan/fang-feishu-app-class2-group7`
- 来源分支：`feat/unified-delivery-20260721`
- 建议目标：团队主仓库的 `master`
- 整理日期：2026-07-21

本次内容全部位于独立分支，没有直接修改个人仓库或团队仓库的 `master`。截至整理时，已获取团队主仓库最新 `upstream/master`（`f4f3de5`），使用 Git merge-tree 检查无内容冲突。

## 推荐合并方式

需要完整交付时，在 Gitee 创建 Pull Request：

```text
du-yiyan:feat/unified-delivery-20260721
  -> grade24-fullstack-class2:master
```

合并前由维护者再次更新目标分支并执行冲突检查即可。不要让其他成员向本交付分支提交日常开发代码。

## 提交拆分

| 顺序 | Commit | 内容 | 体积特点 |
| --- | --- | --- | --- |
| 1 | `ea57618` | 已清理并同步修复后的核心 Android/Backend 项目 | 源码 |
| 2 | `fa04fd1` | PC/Web 共用源码与 Web v0.4.0 构建 | 小文件 |
| 3 | `9fbbd7c` | 最新后端部署包与修复记录 | 小文件 |
| 4 | `cb0d452` | 26 个 Android 历史 APK 的去重归档 | 约 232 MiB |
| 5 | `7282527` | PC v0.4.0 安装包分片归档 | 约 81 MiB |
| 6 | `946e0e1` | 统一交付入口和 Git 忽略例外 | 小文件 |
| 7 | 当前提交 | 本合并交接说明 | 小文件 |

## 按需合并

如果团队主仓库暂时不希望承担安装包体积，可只合并源码、Web 构建、后端包和修复记录，暂不合并 `cb0d452`、`7282527`；这两个大文件提交可以继续保留在交付分支中供下载和备份。

如果团队确认要将所有安装包纳入主仓库，建议直接合并完整分支，不要逐个复制 `chunks` 文件。安装包清单与共享数据块必须成套保留。

选择 cherry-pick 时按上表顺序执行，并在最后检查：

```bash
git status
git diff --check HEAD~1 HEAD
```

Android 与 PC 安装包还原命令分别见：

- [`android/README.md`](android/README.md)
- [`pc/README.md`](pc/README.md)
