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

namespace ZakYip.Singulation.Host.Controllers {

    /// <summary>
    /// 解码器 API：获取/设置解码选项、在线解码帧（HEX/Base64/原始字节）。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class DecoderController : ControllerBase {
        private readonly ILogger<DecoderController> _log;
        private readonly IUpstreamCodec _codec;
        private readonly IUpstreamCodecOptionsStore? _store; // 可选：如果你已有 UpstreamOptions 的 LiteDB Store，请通过 DI 注入

        public DecoderController(
            ILogger<DecoderController> log,
            IUpstreamCodec codec,
            IUpstreamCodecOptionsStore? store = null) {
            _log = log;
            _codec = codec;
            _store = store;
        }

        // ---------------- 健康探测 ----------------
        [HttpGet("health")]
        public async Task<ApiResponse<object>> Health() {
            var data = new { ok = true, codec = _codec.GetType().Name };
            return ApiResponse<object>.Success(data);
        }

        // ---------------- 读取选项（优先持久化，没有则尝试运行时读取） ----------------
        [HttpGet("options")]
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

        // ---------------- 保存选项（持久化；若实现支持则热更新） ----------------
        [HttpPut("options")]
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

        // ---------------- 在线解码 ----------------
        [HttpPost("decode")]
        public ActionResult<ApiResponse<DecodeResult>> Decode([FromBody] DecodeRequest req) {
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

                // 若你还有 TryDecodeHeartbeat / TryDecodeXXX，可在此追加分支

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