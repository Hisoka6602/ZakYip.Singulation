@echo off
setlocal enabledelayedexpansion

:: =========================
:: 配置区（按需修改）
:: =========================
set "serviceName=ZakYip.Singulation"
set "serviceDisplayName=ZakYip.Singulation"  :: 可与 serviceName 不同；仅用于服务管理器显示
set "serviceDescription=泽业单件分离服务（单件分离 Host）"
:: 如需要依赖某个服务（示例：MySQL），取消下一行注释并改名（多个用/分隔，如 MySQL80/W32Time）：
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
:: 创建服务
:: =========================
set "createCmd=sc create "%serviceName%" binPath= "\"%exePath%\"" start= auto"
:: 设置显示名（可选）
set "createCmd=%createCmd% DisplayName= "%serviceDisplayName%""
:: 依赖（可选）
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

:: （可选）设置延迟自启（如需）
:: sc config "%serviceName%" start= delayed-auto >nul

:: =========================
:: 失败自动恢复策略
::  - 60 秒内的失败计数窗口
::  - 连续三次失败均在 5 秒后重启
::  - 将“非崩溃/优雅退出”也视为失败（关键）
:: =========================
sc failure "%serviceName%" reset= 60 actions= restart/5000/restart/5000/restart/5000 >nul
sc failureflag "%serviceName%" 1 >nul 2>&1

:: （可选）查询验证当前恢复配置
sc qfailure "%serviceName%"

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
echo   - 显示名：%serviceDisplayName%
echo   - 路径：%exePath%
echo   - 开机自启：是
echo   - 失败自动重启：是（5 秒 × 3 次；含非崩溃/优雅退出）
echo.
echo [提示] 若需维护时不重启，请暂时禁用服务或临时修改恢复策略。
echo.
pause
endlocal
