# 构建说明

## 自包含部署（Self-Contained Deployment）

从此版本开始，ZakYip.Singulation 支持**自包含部署**模式，这意味着：

✅ **无需在目标机器上安装 .NET 运行时**  
✅ **所有必需的运行时文件都包含在发布输出中**  
✅ **应用程序可以在没有 .NET 的"干净"机器上运行**

### 部署模式说明

项目支持两种部署模式：

1. **框架依赖部署**（Framework-Dependent Deployment）
   - 默认的开发模式
   - 输出文件小（约 10MB）
   - 需要目标机器安装 .NET 8.0 运行时
   - 使用场景：开发、测试环境

2. **自包含部署**（Self-Contained Deployment）
   - 通过指定 `-r {runtime}` 参数启用
   - 输出文件大（约 100MB，包含运行时）
   - 无需目标机器安装 .NET 运行时
   - 使用场景：生产环境、客户部署

## 构建命令

### 1. 开发构建（Debug）

用于日常开发和调试（框架依赖模式）：

```bash
dotnet build ZakYip.Singulation.Host -c Debug
```

输出位置：`ZakYip.Singulation.Host/bin/Debug/net8.0/`

**特点**：
- 不包含 .NET 运行时（需要机器已安装 .NET 8.0）
- 输出文件小（约 10MB）
- 构建速度快

### 2. 发布构建（Release）- 自包含

#### Windows x64（推荐用于生产）

```bash
dotnet build ZakYip.Singulation.Host -c Release -r win-x64
```

输出位置：`ZakYip.Singulation.Host/bin/Release/net8.0/win-x64/`

#### Linux x64

```bash
dotnet build ZakYip.Singulation.Host -c Release -r linux-x64
```

输出位置：`ZakYip.Singulation.Host/bin/Release/net8.0/linux-x64/`

**特点**：
- 包含完整的 .NET 运行时
- 输出文件大（约 100MB）
- 目标机器无需安装 .NET

#### 其他平台

支持的运行时标识符（Runtime Identifiers）：
- `win-x64` - Windows 64位
- `win-x86` - Windows 32位
- `win-arm64` - Windows ARM64
- `linux-x64` - Linux 64位
- `linux-arm` - Linux ARM
- `linux-arm64` - Linux ARM64
- `osx-x64` - macOS Intel
- `osx-arm64` - macOS Apple Silicon

示例：

```bash
dotnet build ZakYip.Singulation.Host -c Release -r osx-arm64
```

### 3. 发布（Publish）

如果需要准备生产部署包，使用 `publish` 命令：

```bash
# Windows - 自包含
dotnet publish ZakYip.Singulation.Host -c Release -r win-x64

# Linux - 自包含
dotnet publish ZakYip.Singulation.Host -c Release -r linux-x64

# 框架依赖（需要目标机器安装 .NET）
dotnet publish ZakYip.Singulation.Host -c Release
```

输出位置：`ZakYip.Singulation.Host/bin/Release/net8.0/{runtime}/publish/`

## 构建输出说明

### 自包含部署的输出内容

构建后的输出目录包含：

1. **应用程序文件**
   - `ZakYip.Singulation.Host.exe`（Windows）或 `ZakYip.Singulation.Host`（Linux）- 主可执行文件
   - `ZakYip.Singulation.Host.dll` - 应用程序库
   - `ZakYip.Singulation.*.dll` - 项目引用的库

2. **.NET 运行时文件**（约 100MB）
   - `coreclr.dll` / `libcoreclr.so` - 核心运行时
   - `clrjit.dll` / `libclrjit.so` - JIT 编译器
   - `hostfxr.dll` / `libhostfxr.so` - 框架解析器
   - `hostpolicy.dll` / `libhostpolicy.so` - 主机策略
   - `System.*.dll` - 系统库
   - `Microsoft.*.dll` - Microsoft 库

3. **其他文件**
   - `LTDMC.dll` - 雷赛运动控制驱动
   - `appsettings.json` - 配置文件
   - `nlog.config` - 日志配置

### 文件大小

- **Debug 构建**：约 110MB
- **Release 构建**：约 100MB（经过优化）
- **文件数量**：约 370+ 个文件

## 运行应用程序

### Windows

直接双击或在命令行运行：

```cmd
ZakYip.Singulation.Host.exe
```

### Linux

添加执行权限并运行：

```bash
chmod +x ZakYip.Singulation.Host
./ZakYip.Singulation.Host
```

## 配置选项

在 `ZakYip.Singulation.Host.csproj` 中有以下配置选项：

### 当前配置

```xml
<SelfContained>true</SelfContained>                    <!-- 自包含部署 -->
<RuntimeIdentifier>win-x64</RuntimeIdentifier>         <!-- 默认目标平台 -->
<PublishSingleFile>false</PublishSingleFile>           <!-- 不打包为单文件 -->
<PublishTrimmed>false</PublishTrimmed>                 <!-- 不裁剪运行时 -->
<PublishReadyToRun>false</PublishReadyToRun>           <!-- 不使用 R2R 编译 -->
```

### 高级选项（可选）

如果需要进一步优化，可以考虑启用以下选项：

#### 1. 单文件发布（PublishSingleFile）

将所有文件打包为单个可执行文件：

```xml
<PublishSingleFile>true</PublishSingleFile>
```

**优点**：
- 部署更简单（只有一个文件）
- 更易于分发

**缺点**：
- 首次启动时需要解压（略慢）
- 文件大小更大
- 某些场景下可能不兼容

#### 2. 运行时裁剪（PublishTrimmed）

⚠️ **谨慎使用** - 可能导致运行时错误

```xml
<PublishTrimmed>true</PublishTrimmed>
```

**优点**：
- 显著减小发布大小（可减少 30-50%）

**缺点**：
- 可能裁剪掉反射或动态加载需要的类型
- 需要彻底测试以确保功能正常
- 不推荐用于使用 SignalR、依赖注入等功能的应用

#### 3. ReadyToRun 编译（PublishReadyToRun）

预编译以提高启动性能：

```xml
<PublishReadyToRun>true</PublishReadyToRun>
```

**优点**：
- 更快的启动时间（减少 JIT 编译）

**缺点**：
- 增加发布大小（约 10-20%）
- 编译时间更长

## 常见问题

### Q: 为什么输出文件这么大？

A: 自包含部署包含完整的 .NET 运行时（约 100MB），这样应用程序可以在没有安装 .NET 的机器上运行。

### Q: 如何减小输出大小？

A: 可以考虑：
1. 使用 `PublishTrimmed` 裁剪未使用的运行时代码（需要彻底测试）
2. 使用 `PublishSingleFile` 压缩文件
3. 改为框架依赖部署（需要目标机器安装 .NET 运行时）

### Q: 如何切换回框架依赖部署？

A: 如果目标机器已安装 .NET 8.0 运行时，可以改为框架依赖部署以减小文件大小：

在 `ZakYip.Singulation.Host.csproj` 中修改：

```xml
<SelfContained>false</SelfContained>
<!-- 移除或注释掉 RuntimeIdentifier -->
```

然后构建：

```bash
dotnet build ZakYip.Singulation.Host -c Release
```

输出大小将减少到约 10MB，但需要在目标机器上安装 .NET 8.0 运行时。

### Q: 跨平台编译

A: 在 Windows 上可以构建 Linux 版本，反之亦然。只需指定目标运行时标识符：

```bash
# 在 Windows 上构建 Linux 版本
dotnet build -c Release -r linux-x64

# 在 Linux 上构建 Windows 版本  
dotnet build -c Release -r win-x64
```

## 部署检查清单

部署到生产环境前，请确认：

- [ ] 使用 Release 配置构建
- [ ] 选择正确的运行时标识符（win-x64, linux-x64 等）
- [ ] 测试应用程序在目标平台上能正常启动
- [ ] 验证所有功能正常工作（尤其是硬件驱动、SignalR、数据库）
- [ ] 检查配置文件（appsettings.json）是否正确
- [ ] 确认所有必需的文件都已包含（LTDMC.dll 等）

## 更多信息

详细的 .NET 发布选项文档：
- [自包含部署](https://learn.microsoft.com/zh-cn/dotnet/core/deploying/deploy-with-cli#self-contained-deployment)
- [运行时标识符目录](https://learn.microsoft.com/zh-cn/dotnet/core/rid-catalog)
- [单文件应用程序](https://learn.microsoft.com/zh-cn/dotnet/core/deploying/single-file)
- [裁剪自包含部署](https://learn.microsoft.com/zh-cn/dotnet/core/deploying/trimming/trim-self-contained)
