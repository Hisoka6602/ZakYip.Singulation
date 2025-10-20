# ZakYip.AfterSales.Courier

售后快递模块 - 钉钉企业API集成

## 功能概述

本模块提供与钉钉企业API的集成功能，主要用于获取组织成员信息。支持无部门场景下获取成员列表，并提供自动分页功能。

## 核心功能

### 1. 成员列表获取（qyapi_get_member）

`DingTalkService.GetMemberListAsync()` 方法实现了钉钉 API 的 `qyapi_get_member` 功能，支持：

- **无部门获取**：即使企业没有部门结构，也能获取所有成员
- **指定部门获取**：可以指定部门ID获取特定部门的成员
- **分页支持**：支持自定义偏移量和每页大小

### 2. 自动分页获取所有成员

`DingTalkService.GetAllMembersAsync()` 方法自动处理分页逻辑，一次性获取所有成员：

```csharp
var members = await dingTalkService.GetAllMembersAsync(accessToken);
```

### 3. 完整测试套件

`DingTalkTester` 类提供了全面的测试功能：

- 测试无部门获取成员
- 测试指定部门获取成员
- 测试自动分页获取所有成员
- 运行所有测试的便捷方法

## 使用示例

### 基本使用

```csharp
using ZakYip.AfterSales.Courier.Services;
using ZakYip.AfterSales.Courier.Models;

// 创建服务实例
var httpClient = new HttpClient();
var dingTalkService = new DingTalkService(httpClient);

// 获取成员列表（无部门）
var request = new GetMemberRequestDto
{
    AccessToken = "your_access_token",
    DeptId = null,  // 不指定部门
    Offset = 0,
    Size = 100
};

var response = await dingTalkService.GetMemberListAsync(request);

if (response.ErrCode == 0)
{
    foreach (var member in response.UserList)
    {
        Console.WriteLine($"{member.Name} - {member.Position}");
    }
}
```

### 获取所有成员（自动分页）

```csharp
// 获取所有成员（自动处理分页）
var allMembers = await dingTalkService.GetAllMembersAsync("your_access_token");

Console.WriteLine($"共获取 {allMembers.Count} 个成员");
```

### 获取指定部门成员

```csharp
var request = new GetMemberRequestDto
{
    AccessToken = "your_access_token",
    DeptId = 1,  // 指定部门ID
    Offset = 0,
    Size = 100
};

var response = await dingTalkService.GetMemberListAsync(request);
```

### 使用测试器

```csharp
using ZakYip.AfterSales.Courier;

var tester = new DingTalkTester();

// 运行所有测试
await tester.RunAllTestsAsync("your_access_token");

// 或者运行单个测试
var result = await tester.TestGetMemberWithoutDeptAsync("your_access_token");
if (result.Success)
{
    Console.WriteLine($"测试成功: {result.Message}");
}
```

## 项目结构

```
ZakYip.AfterSales.Courier/
├── Models/
│   ├── DingTalkRequestDto.cs      # 钉钉请求基础 DTO
│   ├── GetMemberRequestDto.cs     # 获取成员列表请求 DTO
│   ├── DingTalkMemberDto.cs       # 钉钉成员信息 DTO
│   └── GetMemberResponseDto.cs    # 获取成员列表响应 DTO
├── Services/
│   └── DingTalkService.cs         # 钉钉 API 服务
├── DingTalkTester.cs              # 钉钉功能测试器
└── TestResult.cs                  # 测试结果 DTO
```

## 数据模型

### DingTalkMemberDto

成员信息包含以下字段：

- `UserId` - 员工唯一标识ID
- `UnionId` - 员工在当前企业内的唯一标识
- `Name` - 员工姓名
- `Mobile` - 手机号码
- `Email` - 员工邮箱
- `StateCode` - 员工状态（1-试用期，2-正式，3-实习期，5-待离职，-1-无状态）
- `Position` - 职位信息
- `DeptIdList` - 所属部门ID列表

## 注意事项

1. **访问令牌**：需要有效的钉钉企业API访问令牌（AccessToken）
2. **API限制**：钉钉API有调用频率限制，请注意控制调用频率
3. **权限要求**：需要企业管理员授权相应的API权限
4. **安全性**：请妥善保管AccessToken，不要泄露给未授权用户

## 技术栈

- .NET 8.0
- System.Text.Json - JSON序列化
- HttpClient - HTTP客户端

## 代码规范

本项目遵循以下代码规范：

- 每个DTO类都有独立的文件
- 每个类都有独立的文件
- 所有公共接口都有XML注释
- 使用异步编程模式（async/await）
