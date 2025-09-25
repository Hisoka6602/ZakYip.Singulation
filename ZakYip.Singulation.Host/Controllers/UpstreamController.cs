using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace ZakYip.Singulation.Host.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class UpstreamController : ControllerBase {

        /// <summary>
        /// 获取所有上游 TCP 配置
        /// GET /api/upstream/configs
        /// </summary>
        [HttpGet("configs")]
        public async Task<ActionResult<IEnumerable<UpstreamConfigDto>>> GetAllConfigs(CancellationToken ct) {
        }

        /// <summary>
        /// 修改配置
        /// PUT /api/upstream/configs/{id}
        /// </summary>
        [HttpPut("configs")]
        public async Task<ActionResult> UpdateConfig(int id, [FromBody] UpstreamConfigDto dto, CancellationToken ct) {
        }

        /// <summary>
        /// 获取所有上游 TCP 连接状态
        /// GET /api/upstream/connections
        /// </summary>
        [HttpGet("connections")]
        public async Task<ActionResult<IEnumerable<UpstreamConnectionStatusDto>>> GetConnections(CancellationToken ct) {
            var connections = await _connectionManager.GetAllStatusesAsync(ct);
            return Ok(connections);
        }

        /// <summary>
        /// 立即重启/重连指定的上游 TCP
        /// POST /api/upstream/connections/{id}/reconnect
        /// </summary>
        [HttpPost("connections/{id}/reconnect")]
        public async Task<ActionResult> Reconnect(int id, CancellationToken ct) {
            var success = await _connectionManager.ReconnectAsync(id, ct);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}