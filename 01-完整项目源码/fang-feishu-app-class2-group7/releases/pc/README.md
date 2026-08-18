# PC 客户端

| 项目 | 值 |
| --- | --- |
| 版本 | v0.4.0 |
| 平台 | Windows x64 |
| 安装器 | NSIS |
| 还原文件 | `FangFeishu-PC-Setup-v0.4.0.exe` |
| 文件大小 | 85,309,884 字节 |
| SHA-256 | `DA0675A972E3CB5CE70CE97B9176E86B60DC5E5026CA6F67BDEBBC72153BE375` |

## 还原安装包

在仓库根目录打开 PowerShell，执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\file-archive\Restore-File.ps1 `
  -ManifestPath .\releases\pc\v0.4.0\package\manifest.json `
  -OutputDirectory .\outputs
```

脚本会依次校验六个分片，并校验最终安装程序的大小和 SHA-256。

该安装包当前未配置代码签名证书，Windows 可能显示 SmartScreen 提示。安装前请核对上方 SHA-256。

PC 与 Web 共用的源码位于 [`clients/web-desktop`](../../clients/web-desktop/README.md)。
