@echo off
chcp 65001 >nul
echo === Feishu Web 测试环境 ===
echo 启动 http://localhost:3000
echo.
where npx >nul 2>nul
if %errorlevel%==0 (
  start "" http://localhost:3000
  npx serve -p 3000
) else (
  where python >nul 2>nul
  if %errorlevel%==0 (
    start "" http://localhost:3000
    python -m http.server 3000
  ) else (
    echo 需要 Node.js 或 Python！
    pause
  )
)
