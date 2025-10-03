@echo off
setlocal enabledelayedexpansion

:: =========================
:: 配置区（按需修改）
:: =========================
set "serviceName=ZakYip.Singulation"
set "serviceDescription=泽业单件分离服务（单件分离 Host）"
:: 如需要依赖某个服务（示例：MySQL），取消下一行注释并改名：
:: set "dependService=MySQL80"

:: 计算 EXE 路径（默认放在与本 bat 同目录）
set "exeName=ZakYip.Singulation.Host.exe"
set "exePath=%~dp0%exeName%"

:: =========================
:: 管理员权限检查
:: =========================
net session >nul 2>&1
if %errorlevel% neq 0 (
  echo [错误] 请以“管理员身份”运行此脚本。
  pause
  exit /b 1
)

:: =========================
:: 基本校验
:: =========================
if not exist "%exePath%" (
  echo [错误] 未找到可执行文件：%exePath%
  echo 请将 %exeName% 放在本脚本同目录，或修改脚本中的 exePath。
  pause
  exit /b 1
)

:: =========================
:: 若同名服务已存在：先停止并删除
:: =========================
sc query "%serviceName%" >nul 2>&1
if %errorlevel%==0 (
  echo [信息] 检测到已存在服务：%serviceName%，尝试停止并删除...
  sc stop "%serviceName%" >nul 2>&1
  timeout /t 2 >nul
  sc delete "%serviceName%" >nul 2>&1
  timeout /t 1 >nul
)

:: =========================
:: 组装 create 参数
:: =========================
set "createCmd=sc create "%serviceName%" binPath= "\"%exePath%\"" start= auto"
if defined dependService (
  set "createCmd=%createCmd% depend= %dependService%"
)

echo [信息] 正在创建服务：%serviceName%
%createCmd%
if %errorlevel% neq 0 (
  echo [失败] 服务创建失败，请检查权限或路径（%exePath%）。
  pause
  exit /b 1
)

:: 设置描述
sc description "%serviceName%" "%serviceDescription%" >nul

:: =========================
:: 失败自动恢复策略
::  - 60 秒内的失败计数窗口
::  - 失败后 5 秒重启
:: =========================
sc failure "%serviceName%" reset= 60 actions= restart/5000 >nul
:: 可选：始终把失败视为需要恢复（不同系统版本可能无此命令）
:: sc failureflag "%serviceName%" 1 >nul 2>&1

:: =========================
:: 启动服务
:: =========================
echo [信息] 启动服务...
sc start "%serviceName%"
if %errorlevel% neq 0 (
  echo [警告] 服务启动命令返回非零。请用 "sc query %serviceName%" 查看状态或检查事件日志。
) else (
  echo [成功] 服务已启动。
)

echo.
echo [完成] 安装脚本执行结束：
echo   - 服务名：%serviceName%
echo   - 路径：%exePath%
echo   - 开机自启：是
echo   - 失败自动重启：是（5 秒）
echo.
pause
endlocal
