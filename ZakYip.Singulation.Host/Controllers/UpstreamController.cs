using System;
using System.Linq;
using System.Text;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Abstractions;
using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Transport.Abstractions;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 上游通信控制器
    /// </summary>
    /// <remarks>
    /// 管理上游 TCP 连接的配置和状态。
    /// 提供连接配置的读取、更新，以及连接状态查询和重连等功能。
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class UpstreamController : ControllerBase {
        private readonly ILogger<UpstreamController> _logger;
        private readonly IUpstreamOptionsStore _store;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<IByteTransport> _transports;

        public UpstreamController(ILogger<UpstreamController> logger,
            IUpstreamOptionsStore store,
            IServiceProvider serviceProvider,
            IEnumerable<IByteTransport> transports) {
            _logger = logger;
            _store = store;
            _serviceProvider = serviceProvider;
            _transports = transports;
        }

        /// <summary>
        /// 获取上游 TCP 配置
        /// </summary>
        /// <remarks>
        /// 获取所有上游 TCP 连接的配置信息。
        /// 包含连接地址、端口、超时设置等配置参数。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>上游配置对象</returns>
        /// <response code="200">获取配置成功</response>
        [HttpGet("configs")]
        public async Task<ApiResponse<UpstreamOptions>> GetAllAsync(CancellationToken ct) {
            var optionsDto = await _store.GetAsync(ct);
            return ApiResponse<UpstreamOptions>.Success(optionsDto);
        }

        /// <summary>
        /// 更新上游 TCP 配置
        /// </summary>
        /// <remarks>
        /// 保存或更新上游 TCP 连接的配置信息。
        /// 配置更新后会持久化保存。
        /// </remarks>
        /// <param name="dto">上游配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">配置已保存</response>
        [HttpPut("configs")]
        public async Task<ApiResponse<string>> UpsertAsync([FromBody] UpstreamOptions dto, CancellationToken ct) {
            await _store.SaveAsync(dto, ct);
            return ApiResponse<string>.Success("配置已保存");
        }

        /// <summary>
        /// 获取上游 TCP 连接状态
        /// </summary>
        /// <remarks>
        /// 获取所有上游 TCP 连接的实时状态信息。
        /// 返回每个连接的 IP、端口、连接状态、实现类型等信息。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>连接状态列表</returns>
        /// <response code="200">获取连接状态成功</response>
        [HttpGet("connections")]
        public Task<ApiResponse<UpstreamConnectionsDto>> GetConnectionsAsync(CancellationToken ct) {
            try {
                var items = _transports.Select((t, i) => new UpstreamConnectionDto {
                    Ip = t.RemoteIp,
                    Port = t.RemotePort,
                    IsServer = t.IsServer,
                    State = t.Status.ToString(),
                    Impl = t.GetType().Name,
                    Index = i + 1
                }).ToList();

                var data = new UpstreamConnectionsDto {
                    Enabled = items.Count > 0,
                    Items = items
                };

                return Task.FromResult(ApiResponse<UpstreamConnectionsDto>.Success(data));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "GetConnections failed.");
                return Task.FromResult(ApiResponse<UpstreamConnectionsDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// 重连指定的上游连接
        /// </summary>
        /// <remarks>
        /// 重启或重连指定索引的上游 TCP 连接。
        /// 索引从 0 开始，对应 connections 列表中的位置。
        /// </remarks>
        /// <param name="index">连接索引</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">重连请求已执行</response>
        /// <response code="404">连接不存在</response>
        [HttpPost("connections/{index}/reconnect")]
        public async Task<ApiResponse<string>> Reconnect(int index, CancellationToken ct) {
            var t = _transports?.ElementAtOrDefault(index);
            if (t is null)
                return ApiResponse<string>.NotFound($"连接 {index} 不存在");

            await t.RestartAsync(ct);
            return ApiResponse<string>.Success("reconnect", "重启/重连请求已执行");
        }
    }
}