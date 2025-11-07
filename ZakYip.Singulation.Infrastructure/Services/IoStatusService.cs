using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Core.Contracts;
using csLTDMC;

namespace ZakYip.Singulation.Infrastructure.Services {

    /// <summary>
    /// IO 状态查询服务，提供读取和写入雷赛控制器 IO 端口状态的功能。
    /// </summary>
    public sealed class IoStatusService {
        private readonly ILogger<IoStatusService> _logger;
        private readonly IControllerOptionsStore _ctrlOptsStore;
        private readonly ZakYip.Singulation.Drivers.Abstractions.IBusAdapter _busAdapter;
        private ushort _cardNo;

        public IoStatusService(
            ILogger<IoStatusService> logger,
            IControllerOptionsStore ctrlOptsStore,
            ZakYip.Singulation.Drivers.Abstractions.IBusAdapter busAdapter) {
            _logger = logger;
            _ctrlOptsStore = ctrlOptsStore;
            _busAdapter = busAdapter;
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
        /// 查询所有 IO 状态（使用并行读取优化性能）。
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

            // 使用 ArrayPool 减少内存分配
            var inputBitNumbers = ArrayPool<int>.Shared.Rent(inputCount);
            var outputBitNumbers = ArrayPool<int>.Shared.Rent(outputCount);
            
            try {
                // 填充位号数组
                for (int i = 0; i < inputCount; i++) {
                    inputBitNumbers[i] = inputStart + i;
                }
                for (int i = 0; i < outputCount; i++) {
                    outputBitNumbers[i] = outputStart + i;
                }

                // 并行读取输入和输出 IO，提高性能
                var inputTask = Task.Run(() => {
                    var results = new IoStatusDto[inputCount];
                    // 使用并行循环读取输入IO，限制并发度避免资源耗尽
                    Parallel.For(0, inputCount, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => {
                        results[i] = ReadInputBit(inputBitNumbers[i]);
                    });
                    return results.ToList();
                }, ct);

                var outputTask = Task.Run(() => {
                    var results = new IoStatusDto[outputCount];
                    // 使用并行循环读取输出IO，限制并发度避免资源耗尽
                    Parallel.For(0, outputCount, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => {
                        results[i] = ReadOutputBit(outputBitNumbers[i]);
                    });
                    return results.ToList();
                }, ct);

                // 等待两个任务并行完成
                await Task.WhenAll(inputTask, outputTask);

                var inputIos = inputTask.Result;
                var outputIos = outputTask.Result;

                var allIos = inputIos.Concat(outputIos);
                var validCount = allIos.Count(io => io.IsValid);
                var errorCount = allIos.Count(io => !io.IsValid);

                var response = new IoStatusResponseDto {
                    InputIos = inputIos,
                    OutputIos = outputIos,
                    ValidCount = validCount,
                    ErrorCount = errorCount
                };

                return response;
            }
            finally {
                // 归还数组到池中
                ArrayPool<int>.Shared.Return(inputBitNumbers);
                ArrayPool<int>.Shared.Return(outputBitNumbers);
            }
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

        /// <summary>
        /// 写入输出 IO 端口电平状态。
        /// </summary>
        /// <param name="bitNo">输出 IO 端口编号</param>
        /// <param name="state">要设置的电平状态（High 或 Low）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>写入结果</returns>
        public async Task<(bool Success, string Message)> WriteOutputBitAsync(
            int bitNo,
            IoState state,
            CancellationToken ct = default) {

            await InitializeAsync(ct);

            // 检查总线是否已初始化，禁止在初始化/复位期间写入 IO
            if (!_busAdapter.IsInitialized) {
                var errorMsg = "总线未初始化或正在复位中，禁止写入 IO 端口";
                _logger.LogWarning(errorMsg);
                return (false, errorMsg);
            }

            try {
                // 将 IoState 枚举转换为 API 需要的值
                // IoState.High = 1, IoState.Low = 0 （枚举值已在定义时确保为 0 或 1）
                ushort onOff;
                switch (state)
                {
                    case IoState.Low:
                        onOff = 0;
                        break;
                    case IoState.High:
                        onOff = 1;
                        break;
                    default:
                        var errorMsg = $"无效的 IO 状态值: {state}";
                        _logger.LogError(errorMsg);
                        return (false, errorMsg);
                }

                _logger.LogInformation(
                    "准备写入输出 IO 位 {BitNo}，状态：{State} ({OnOff})",
                    bitNo, state, onOff);

                // 调用雷赛 API 写入输出位
                // 参数：卡号、位号、状态（0=低电平，1=高电平）
                // 返回值：0=成功，<0=错误
                short result = LTDMC.dmc_write_outbit(_cardNo, (ushort)bitNo, onOff);

                if (result < 0) {
                    var errorMsg = $"写入输出 IO 位 {bitNo} 失败，错误码：{result}";
                    _logger.LogError(errorMsg);
                    return (false, errorMsg);
                }

                _logger.LogInformation(
                    "成功写入输出 IO 位 {BitNo}，状态：{State}",
                    bitNo, state);

                return (true, "写入成功");
            }
            catch (Exception ex) {
                var errorMsg = $"写入输出 IO 位 {bitNo} 时发生异常：{ex.Message}";
                _logger.LogError(ex, errorMsg);
                return (false, errorMsg);
            }
        }
    }
}
