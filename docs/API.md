# Singulation REST API 指南

本文档覆盖默认部署下的主机接口：所有路径均以 `https://localhost:5001/` 为基准。实际生产环境请替换为部署时的域名或 IP，并保证 HTTPS 证书已正确安装。

## 统一返回约定

所有接口均返回 `ApiResponse<T>` 包装体：

```json
{
  "result": true,
  "msg": "操作成功",
  "data": {}
}
```

- `result`：布尔值，指示请求是否成功。
- `msg`：人类可读的提示信息。
- `data`：实际业务数据，可为空。

## 控制器（Controller）资源

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/axes/controller` | 查询总线控制器的当前状态（轴数量、错误码、初始化状态）。 |
| `POST` | `/api/axes/controller/reset` | 创建一次控制器复位请求，体内提供 `type` (`hard`/`soft`)。 |
| `GET` | `/api/axes/controller/errors` | 获取当前错误码。 |
| `DELETE` | `/api/axes/controller/errors` | 清除错误状态。 |
| `GET` | `/api/axes/controller/options` | 拉取控制器驱动模板。 |
| `PUT` | `/api/axes/controller/options` | 覆盖控制器驱动模板。 |

### 示例：获取控制器状态

```http
GET https://localhost:5001/api/axes/controller
Accept: application/json
```

成功响应：

```json
{
  "result": true,
  "msg": "获取控制器状态成功",
  "data": {
    "axisCount": 4,
    "errorCode": 0,
    "initialized": true
  }
}
```

## 轴拓扑资源

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/axes/topology` | 读取当前的轴网格布局定义。 |
| `PUT` | `/api/axes/topology` | 覆盖布局，体内需包含行列与布局。 |
| `DELETE` | `/api/axes/topology` | 删除现有布局，恢复为空。 |

## 轴集合资源

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/axes/axes` | 列举所有已注册轴的快照。 |
| `GET` | `/api/axes/axes/{id}` | 查询特定轴的状态。 |

## 解码服务

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/decoder/health` | 解码服务健康检查。 |
| `GET` | `/api/decoder/options` | 获取持久化的解码配置。 |
| `PUT` | `/api/decoder/options` | 覆盖解码配置。 |
| `POST` | `/api/decoder/frames` | 提交一帧数据进行解码，支持 raw/hex/base64。 |

`POST /api/decoder/frames` 请求示例：

```http
POST https://localhost:5001/api/decoder/frames
Content-Type: application/json

{
  "hex": "AA 55 01 02"
}
```

## 安全管线命令

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `POST` | `/api/safety/commands` | 向安全管线提交一条命令，体内包含 `command` (`Start`/`Stop`/`Reset`) 与可选 `reason`。 |

示例：

```http
POST https://localhost:5001/api/safety/commands
Content-Type: application/json

{
  "command": "Stop",
  "reason": "手动停止"
}
```

## 运行会话管理

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `DELETE` | `/api/system/session` | 删除当前运行会话，通知宿主进程优雅退出（外部部署器负责拉起）。 |

## Upstream 解码配置与状态

| 方法 | 路径 | 描述 |
| --- | --- | --- |
| `GET` | `/api/upstream/configuration` | 读取上游 TCP 配置。 |
| `PUT` | `/api/upstream/configuration` | 更新上游 TCP 配置。 |
| `GET` | `/api/upstream/status` | 查看当前上游连接状态。 |

> ⚠️ Upstream 配置接口仍保持与 LiteDB 同步，需要先通过 `PUT` 写入后再调用 `POST /api/system/session` 触发宿主重载。

## Swagger/OpenAPI

应用默认启用 Swagger：在浏览器访问 `https://localhost:5001/swagger` 可查看交互式文档。所有 RESTful 接口都会展示在 `v1` 文档中，并附带 XML 注释描述与示例。

## 调试建议

1. 本地开发时建议启用 HTTPS 开发证书（`dotnet dev-certs https --trust`）。
2. API 均支持取消令牌，可在客户端传递 `CancellationToken` 控制超时。
3. 当通过 `/api/system/session` 触发退出后，请确保外部 Watchdog（例如 `install.bat` 部署脚本）处于运行状态，以便自动拉起服务。
