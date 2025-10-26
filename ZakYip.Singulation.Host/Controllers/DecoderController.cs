using System;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Protocol.Abstractions;
using ZakYip.Singulation.Infrastructure.Configs.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 解码器 API：获取/设置解码选项、在线解码帧（HEX/Base64/原始字节）。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class DecoderController : ControllerBase {
        private readonly ILogger<DecoderController> _log;
        private readonly IUpstreamCodec _codec;
        private readonly IUpstreamCodecOptionsStore? _store;

        public DecoderController(
            ILogger<DecoderController> log,
            IUpstreamCodec codec,
            IUpstreamCodecOptionsStore? store = null) {
            _log = log;
            _codec = codec;
            _store = store;
        }

        // ---------------- 健康探测 ----------------
        /// <summary>
        /// 解码器健康检查
        /// </summary>
        /// <remarks>
        /// 检查解码器服务是否正常运行。
        /// </remarks>
        /// <returns>健康状态</returns>
        /// <response code="200">解码器服务正常</response>
        [HttpGet("health")]
        [SwaggerOperation(
            Summary = "解码器健康检查",
            Description = "检查解码器服务是否正常运行")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [Produces("application/json")]
        public Task<ApiResponse<object>> Health() {
            var data = new { ok = true, codec = _codec.GetType().Name };
            return Task.FromResult(ApiResponse<object>.Success(data));
        }

        /// <summary>
        /// 获取解码器配置选项
        /// </summary>
        /// <remarks>
        /// 读取解码器的配置选项，优先从持久化存储读取。
        /// 如果持久化存储不可用，则返回运行时配置。
        /// </remarks>
        /// <param name="ct">取消令牌</param>
        /// <returns>解码器配置对象</returns>
        /// <response code="200">读取成功</response>
        /// <response code="500">读取失败</response>
        [HttpGet("options")]
        [SwaggerOperation(
            Summary = "获取解码器配置选项",
            Description = "读取解码器的配置选项，优先从持久化存储读取。如果持久化存储不可用，则返回运行时配置。")]
        [ProducesResponseType(typeof(ApiResponse<UpstreamCodecOptions>), 200)]
        [ProducesResponseType(typeof(ApiResponse<UpstreamCodecOptions>), 500)]
        [Produces("application/json")]
        public async Task<ApiResponse<UpstreamCodecOptions>> GetOptions(CancellationToken ct) {
            try {
                if (_store is not null) {
                    var dto = await _store.GetAsync(ct);
                    return ApiResponse<UpstreamCodecOptions>.Success(dto);
                }

                var runtime = new UpstreamCodecOptions();
                return ApiResponse<UpstreamCodecOptions>.Success(runtime); // 可能是 null，由前端自行决定如何呈现
            }
            catch (Exception ex) {
                _log.LogError(ex, "Get decoder options failed.");
                return ApiResponse<UpstreamCodecOptions>.Fail("读取解码器选项失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 保存解码器配置选项
        /// </summary>
        /// <remarks>
        /// 保存解码器的配置选项到持久化存储。
        /// MainCount 和 EjectCount 必须大于等于 0。
        /// 如果持久化存储不可用，配置不会被保存。
        /// </remarks>
        /// <param name="dto">解码器配置对象</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>操作结果</returns>
        /// <response code="200">保存成功</response>
        /// <response code="400">参数验证失败</response>
        [HttpPut("options")]
        [SwaggerOperation(
            Summary = "保存解码器配置选项",
            Description = "保存解码器的配置选项到持久化存储。MainCount 和 EjectCount 必须大于等于 0。如果持久化存储不可用，配置不会被保存。")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ApiResponse<string>>> SaveOptions([FromBody] UpstreamCodecOptions dto,
            CancellationToken ct) {
            if (dto is null)
                return ApiResponse<string>.Success("请求体不能为空。");

            if (dto.MainCount < 0 || dto.EjectCount < 0)
                return ApiResponse<string>.Success("MainCount/EjectCount 必须 >= 0。");

            try {
                var persisted = false;
                if (_store is null) {
                    _log.LogWarning("未注入 IUpstreamCodecOptionsStore，选项不会持久化。");
                }
                else {
                    await _store.UpsertAsync(dto, ct);
                    persisted = true;
                    _log.LogInformation("持久化 UpstreamCodecOptions 成功：Main={Main}, Eject={Eject}", dto.MainCount,
                        dto.EjectCount);
                }

                if (persisted) {
                    //设置编码参数
                }

                return ApiResponse<string>.Success("修改成功");
            }
            catch (Exception ex) {
                _log.LogError(ex, "Save decoder options failed.");
                return ApiResponse<string>.Success("保存解码器选项失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 在线解码帧数据
        /// </summary>
        /// <remarks>
        /// 对提供的帧数据进行在线解码。
        /// 支持三种输入格式：
        /// - Bytes: 原始字节数组（优先）
        /// - Hex: 十六进制字符串（允许空格和破折号分隔）
        /// - Base64: Base64 编码的字符串
        /// 
        /// 解码成功后返回解析出的速度数据，包含主线速度和弹射速度。
        /// </remarks>
        /// <param name="req">解码请求，包含要解码的数据</param>
        /// <returns>解码结果</returns>
        /// <response code="200">解码成功或无法识别</response>
        /// <response code="400">数据格式错误</response>
        [HttpPost("frames")]
        [SwaggerOperation(
            Summary = "在线解码帧数据",
            Description = "对提供的帧数据进行在线解码。支持 Bytes（原始字节数组，优先）、Hex（十六进制字符串）、Base64 三种输入格式。解码成功后返回解析出的速度数据。")]
        [ProducesResponseType(typeof(ApiResponse<DecodeResult>), 200)]
        [ProducesResponseType(typeof(ApiResponse<DecodeResult>), 400)]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<ApiResponse<DecodeResult>> CreateDecodeJob([FromBody] DecodeRequest req) {
            if (!TryMaterializeBytes(req, out var bytes, out var err))
                return ApiResponse<DecodeResult>.Fail(err!);

            try {
                if (_codec.TryDecodeSpeed(bytes, out var speed)) {
                    _log.LogInformation("Decoded SpeedSet: main={Main}, eject={Eject}, seq={Seq}",
                        speed.MainMmps?.Count ?? 0, speed.EjectMmps?.Count ?? 0, speed.Sequence);

                    return ApiResponse<DecodeResult>.Success(new DecodeResult {
                        Ok = true,
                        Kind = "speed",
                        RawLength = bytes.Length,
                        Speed = speed
                    });
                }

                return ApiResponse<DecodeResult>.Success(new DecodeResult {
                    Ok = false,
                    Kind = "unknown",
                    RawLength = bytes.Length,
                    Speed = null
                }, "无法识别的帧格式。");
            }
            catch (Exception ex) {
                _log.LogError(ex, "Decode failed.");
                return ApiResponse<DecodeResult>.Fail("解码失败：" + ex.Message);
            }
        }

        // ---------------- 内部：把请求体转成字节 ----------------
        private static bool TryMaterializeBytes(DecodeRequest req, out ReadOnlySpan<byte> bytes, out string? err) {
            // 1) 原始 bytes 优先
            if (req.Bytes is { Length: > 0 }) {
                bytes = req.Bytes;
                err = null;
                return true;
            }

            // 2) HEX（允许空格/破折号）
            if (!string.IsNullOrWhiteSpace(req.Hex)) {
                var hex = req.Hex!.Replace(" ", "").Replace("-", "");
                if (hex.Length % 2 != 0) {
                    bytes = default;
                    err = "HEX 长度必须为偶数。";
                    return false;
                }

                try {
                    var len = hex.Length / 2;
                    var buf = new byte[len];
                    for (int i = 0; i < len; i++)
                        buf[i] = byte.Parse(hex.AsSpan(i * 2, 2), NumberStyles.AllowHexSpecifier);

                    bytes = buf;
                    err = null;
                    return true;
                }
                catch (Exception ex) {
                    bytes = default;
                    err = "HEX 解析失败：" + ex.Message;
                    return false;
                }
            }

            // 3) Base64
            if (!string.IsNullOrWhiteSpace(req.Base64)) {
                try {
                    var buf = Convert.FromBase64String(req.Base64!);
                    bytes = buf;
                    err = null;
                    return true;
                }
                catch (Exception ex) {
                    bytes = default;
                    err = "Base64 解析失败：" + ex.Message;
                    return false;
                }
            }

            bytes = default;
            err = "缺少数据载荷（请提供 bytes | hex | base64 之一）。";
            return false;
        }
    }
}