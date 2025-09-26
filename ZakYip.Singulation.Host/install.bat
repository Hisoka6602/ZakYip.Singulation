@echo off
:: ========== 配置区 ==========
set serviceName=ZakYip.Singulation.Host
set serviceDescription=泽业单件分离服务
::set dependService=mysql
:: =============================

:: 检查管理员权限
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo 错误：此脚本必须以管理员身份运行！
    pause
    exit /b 1
)

set "serviceFilePath=%~dp0ZakYip.Singulation.Host.exe"

if not exist "%serviceFilePath%" (
    echo 错误：服务文件未找到！%serviceFilePath%
    pause
    exit /b 1
)

echo 正在安装服务: %serviceName%
echo 文件路径: %serviceFilePath%
echo 依赖服务: %dependService%
echo ====================================================

:: 创建服务
sc create "%serviceName%" binPath= "%serviceFilePath%" displayname= "%serviceDescription%" 
if %errorlevel% equ 0 (
    echo [成功] 服务创建成功。
) else (
    echo [失败] 服务创建失败，请检查：
    echo   - 服务是否已存在（可用 sc delete 卸载）
    echo   - 依赖服务名是否正确（当前: %dependService%）
    pause
    exit /b 1
)

sc config "%serviceName%" start= auto
sc description "%serviceName%" "%serviceDescription%"
sc start "%serviceName%"

echo 服务安装并启动完成。
pause