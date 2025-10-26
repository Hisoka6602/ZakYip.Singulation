using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Host.Dto;
using ZakYip.Singulation.Core.Contracts;
using csLTDMC;

namespace ZakYip.Singulation.Host.Services {

    /// <summary>
    /// IO 状态查询服务，提供读取雷赛控制器所有 IO 端口状态的功能。
    /// </summary>
    public sealed class IoStatusService {
        private readonly ILogger<IoStatusService> _logger;
        private readonly IControllerOptionsStore _ctrlOptsStore;
        private ushort _cardNo;

        public IoStatusService(
            ILogger<IoStatusService> logger,
            IControllerOptionsStore ctrlOptsStore) {
            _logger = logger;
            _ctrlOptsStore = ctrlOptsStore;
        }

        /// <summary>
        /// 初始化服务，获取控制器卡号。
        /// </summary>
        private async Task InitializeAsync(CancellationToken ct) {
            try {
                var options = await _ctrlOptsStore.GetAsync(ct);
                _cardNo = (ushort)options.Template.Card;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "初始化 IoStatusService 失败");
                _cardNo = 0; // 默认卡号
            }
        }

        /// <summary>
        /// 查询所有 IO 状态。
        /// </summary>
        /// <param name="inputStart">输入 IO 起始位号，默认 0</param>
        /// <param name="inputCount">输入 IO 数量，默认 32</param>
        /// <param name="outputStart">输出 IO 起始位号，默认 0</param>
        /// <param name="outputCount">输出 IO 数量，默认 32</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>IO 状态响应对象</returns>
        public async Task<IoStatusResponseDto> GetAllIoStatusAsync(
            int inputStart = 0,
            int inputCount = 32,
            int outputStart = 0,
            int outputCount = 32,
            CancellationToken ct = default) {

            await InitializeAsync(ct);

            var response = new IoStatusResponseDto();
            int validCount = 0;
            int errorCount = 0;

            // 读取输入 IO
            for (int i = 0; i < inputCount; i++) {
                int bitNo = inputStart + i;
                var ioStatus = ReadInputBit(bitNo);
                response.InputIos.Add(ioStatus);
                
                if (ioStatus.IsValid) {
                    validCount++;
                } else {
                    errorCount++;
                }
            }

            // 读取输出 IO
            for (int i = 0; i < outputCount; i++) {
                int bitNo = outputStart + i;
                var ioStatus = ReadOutputBit(bitNo);
                response.OutputIos.Add(ioStatus);
                
                if (ioStatus.IsValid) {
                    validCount++;
                } else {
                    errorCount++;
                }
            }

            response.ValidCount = validCount;
            response.ErrorCount = errorCount;

            _logger.LogInformation(
                "查询 IO 状态完成：输入 IO {InputCount} 个，输出 IO {OutputCount} 个，成功 {ValidCount} 个，失败 {ErrorCount} 个",
                inputCount, outputCount, validCount, errorCount);

            return response;
        }

        /// <summary>
        /// 读取单个输入 IO 状态。
        /// </summary>
        private IoStatusDto ReadInputBit(int bitNo) {
            try {
                // 调用雷赛 API 读取输入位
                // 返回值：0=低电平，1=高电平，<0=错误
                short result = LTDMC.dmc_read_inbit(_cardNo, (ushort)bitNo);

                if (result < 0) {
                    return new IoStatusDto {
                        BitNumber = bitNo,
                        Type = IoType.Input,
                        State = IoState.Low,
                        IsValid = false,
                        ErrorMessage = $"读取失败，错误码：{result}"
                    };
                }

                return new IoStatusDto {
                    BitNumber = bitNo,
                    Type = IoType.Input,
                    State = result == 1 ? IoState.High : IoState.Low,
                    IsValid = true,
                    ErrorMessage = null
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "读取输入位 {BitNo} 时发生异常", bitNo);
                return new IoStatusDto {
                    BitNumber = bitNo,
                    Type = IoType.Input,
                    State = IoState.Low,
                    IsValid = false,
                    ErrorMessage = $"异常：{ex.Message}"
                };
            }
        }

        /// <summary>
        /// 读取单个输出 IO 状态。
        /// </summary>
        private IoStatusDto ReadOutputBit(int bitNo) {
            try {
                // 调用雷赛 API 读取输出位
                // 返回值：0=低电平，1=高电平，<0=错误
                short result = LTDMC.dmc_read_outbit(_cardNo, (ushort)bitNo);

                if (result < 0) {
                    return new IoStatusDto {
                        BitNumber = bitNo,
                        Type = IoType.Output,
                        State = IoState.Low,
                        IsValid = false,
                        ErrorMessage = $"读取失败，错误码：{result}"
                    };
                }

                return new IoStatusDto {
                    BitNumber = bitNo,
                    Type = IoType.Output,
                    State = result == 1 ? IoState.High : IoState.Low,
                    IsValid = true,
                    ErrorMessage = null
                };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "读取输出位 {BitNo} 时发生异常", bitNo);
                return new IoStatusDto {
                    BitNumber = bitNo,
                    Type = IoType.Output,
                    State = IoState.Low,
                    IsValid = false,
                    ErrorMessage = $"异常：{ex.Message}"
                };
            }
        }
    }
}
