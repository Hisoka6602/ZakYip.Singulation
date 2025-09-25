using System;
using System.Linq;
using System.Text;
using TouchSocket.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Abstractions;
using System.ComponentModel.DataAnnotations;
using ZakYip.Singulation.Transport.Abstractions;
using ZakYip.Singulation.Core.Contracts.Dto.Transport;

namespace ZakYip.Singulation.Host.Controllers {

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
        /// 获取所有上游 TCP 配置
        /// GET /api/upstream/configs
        /// </summary>
        [HttpGet("configs")]
        public async Task<ApiResponse<UpstreamOptionsDto>> GetAllAsync(CancellationToken ct) {
            var optionsDto = await _store.GetAsync(ct);
            return ApiResponse<UpstreamOptionsDto>.Success(optionsDto);
        }

        /// <summary>
        /// 修改配置
        /// PUT /api/upstream/configs/{id}
        /// </summary>
        [HttpPut("configs")]
        public async Task<ApiResponse<string>> UpsertAsync([FromBody] UpstreamOptionsDto dto, CancellationToken ct) {
            await _store.SaveAsync(dto, ct);
            return ApiResponse<string>.Success("配置已保存");
        }

        /// <summary>
        /// 获取所有上游 TCP 连接状态
        /// GET /api/upstream/connections
        /// </summary>
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