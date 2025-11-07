using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Infrastructure.Services {
    /// <summary>
    /// 智能故障诊断服务，提供自动故障诊断和解决建议
    /// </summary>
    public sealed class FaultDiagnosisService {
        private readonly ILogger<FaultDiagnosisService> _logger;
        private readonly IAxisController _axisController;
        
        // 内置故障知识库
        private readonly List<FaultKnowledgeEntry> _knowledgeBase;

        public FaultDiagnosisService(
            ILogger<FaultDiagnosisService> logger,
            IAxisController axisController) {
            _logger = logger;
            _axisController = axisController;
            _knowledgeBase = InitializeKnowledgeBase();
        }

        /// <summary>
        /// 诊断指定轴的故障
        /// </summary>
        public async Task<FaultDiagnosisDto?> DiagnoseAxisAsync(string axisId, CancellationToken ct = default) {
            try {
                var drive = _axisController.Drives
                    .FirstOrDefault(d => d.Axis.ToString() == axisId);

                if (drive == null) {
                    return new FaultDiagnosisDto {
                        AxisId = axisId,
                        FaultType = "AXIS_NOT_FOUND",
                        Severity = FaultSeverity.Error,
                        Description = $"未找到轴 {axisId}",
                        DiagnosedAt = DateTime.Now
                    };
                }

                return DiagnoseAxisDrive(drive);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "诊断轴 {AxisId} 失败", axisId);
                return null;
            }
        }

        /// <summary>
        /// 诊断所有轴并返回有问题的轴
        /// </summary>
        public async Task<List<FaultDiagnosisDto>> DiagnoseAllAxesAsync(CancellationToken ct = default) {
            var results = new List<FaultDiagnosisDto>();

            try {
                var drives = _axisController.Drives.ToList();

                foreach (var drive in drives) {
                    var diagnosis = DiagnoseAxisDrive(drive);
                    if (diagnosis != null && diagnosis.Severity >= FaultSeverity.Warning) {
                        results.Add(diagnosis);
                    }
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "诊断所有轴失败");
            }

            return results;
        }

        /// <summary>
        /// 根据错误码查询知识库
        /// </summary>
        public FaultDiagnosisDto? QueryKnowledgeBase(int errorCode, string? axisId = null) {
            var entry = _knowledgeBase.FirstOrDefault(e => e.ErrorPattern == errorCode.ToString());
            
            if (entry == null) {
                return null;
            }

            return CreateDiagnosisFromKnowledge(entry, axisId, errorCode);
        }

        private FaultDiagnosisDto? DiagnoseAxisDrive(IAxisDrive drive) {
            var axisId = drive.Axis.ToString();

            // 1. 检查驱动状态
            if (drive.Status == DriverStatus.Disconnected || drive.Status == DriverStatus.Faulted) {
                return new FaultDiagnosisDto {
                    AxisId = axisId,
                    FaultType = "AXIS_DISCONNECTED",
                    Severity = FaultSeverity.Critical,
                    Description = $"轴 {axisId} 已断开连接",
                    PossibleCauses = new List<string> {
                        "网络连接中断",
                        "驱动器电源故障",
                        "总线通信故障",
                        "驱动器硬件故障"
                    },
                    Suggestions = new List<string> {
                        "检查网络连接是否正常",
                        "检查驱动器电源指示灯",
                        "重启驱动器并重新连接",
                        "检查总线电缆连接",
                        "查看驱动器错误指示灯"
                    },
                    DiagnosedAt = DateTime.Now,
                    ErrorCode = drive.LastErrorCode
                };
            }

            // 2. 检查错误码
            if (drive.LastErrorCode != 0) {
                var errorCode = drive.LastErrorCode;
                var errorMsg = drive.LastErrorMessage;

                // 先查询知识库
                var knowledgeResult = QueryKnowledgeBase(errorCode, axisId);
                if (knowledgeResult != null) {
                    // 添加驱动器的原始错误消息
                    return knowledgeResult with { ErrorMessage = errorMsg };
                }

                // 通用错误诊断
                return new FaultDiagnosisDto {
                    AxisId = axisId,
                    FaultType = "AXIS_ERROR",
                    Severity = FaultSeverity.Error,
                    Description = $"轴 {axisId} 发生错误: {errorMsg ?? "未知错误"}",
                    PossibleCauses = new List<string> {
                        "驱动器报警",
                        "参数配置错误",
                        "运动超限",
                        "负载过大"
                    },
                    Suggestions = new List<string> {
                        $"检查错误码: {errorCode}",
                        "查阅驱动器手册",
                        "检查运动参数配置",
                        "清除驱动器报警后重试"
                    },
                    DiagnosedAt = DateTime.Now,
                    ErrorCode = errorCode,
                    ErrorMessage = errorMsg
                };
            }

            // 3. 检查使能状态
            if (!drive.IsEnabled) {
                return new FaultDiagnosisDto {
                    AxisId = axisId,
                    FaultType = "AXIS_NOT_ENABLED",
                    Severity = FaultSeverity.Warning,
                    Description = $"轴 {axisId} 未使能",
                    PossibleCauses = new List<string> {
                        "轴未启动",
                        "安全信号未激活",
                        "使能命令未发送"
                    },
                    Suggestions = new List<string> {
                        "检查安全开关状态",
                        "发送使能命令",
                        "检查急停按钮是否释放"
                    },
                    DiagnosedAt = DateTime.Now
                };
            }

            // 4. 检查速度反馈异常
            if (drive.LastTargetMmps.HasValue && drive.LastFeedbackMmps.HasValue) {
                var target = (double)drive.LastTargetMmps.Value;
                var feedback = (double)drive.LastFeedbackMmps.Value;
                
                if (Math.Abs(target) > 0.1) {  // 目标速度不为 0
                    var deviation = Math.Abs(target - feedback) / Math.Abs(target);
                    
                    if (deviation > 0.3) {  // 偏差超过 30%
                        return new FaultDiagnosisDto {
                            AxisId = axisId,
                            FaultType = "SPEED_DEVIATION",
                            Severity = FaultSeverity.Warning,
                            Description = $"轴 {axisId} 速度偏差过大: 目标 {target:F2} mm/s, 实际 {feedback:F2} mm/s",
                            PossibleCauses = new List<string> {
                                "负载过大导致速度跟随不佳",
                                "加速度设置过低",
                                "机械摩擦增大",
                                "驱动器参数调整不当"
                            },
                            Suggestions = new List<string> {
                                "检查机械负载是否正常",
                                "适当增加加速度参数",
                                "检查传动机构是否有卡滞",
                                "调整驱动器 PID 参数"
                            },
                            DiagnosedAt = DateTime.Now
                        };
                    }
                }
            }

            // 正常状态，返回 null
            return null;
        }

        private FaultDiagnosisDto CreateDiagnosisFromKnowledge(
            FaultKnowledgeEntry entry, 
            string? axisId, 
            int? errorCode) {
            
            var causes = string.IsNullOrEmpty(entry.PossibleCausesJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(entry.PossibleCausesJson) ?? new List<string>();

            var suggestions = string.IsNullOrEmpty(entry.SuggestionsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(entry.SuggestionsJson) ?? new List<string>();

            return new FaultDiagnosisDto {
                AxisId = axisId,
                FaultType = entry.FaultType,
                Severity = (FaultSeverity)entry.Severity,
                Description = entry.Description,
                PossibleCauses = causes,
                Suggestions = suggestions,
                DiagnosedAt = DateTime.Now,
                ErrorCode = errorCode
            };
        }

        /// <summary>
        /// 初始化内置故障知识库
        /// </summary>
        private List<FaultKnowledgeEntry> InitializeKnowledgeBase() {
            var kb = new List<FaultKnowledgeEntry>();

            // 常见错误码知识库（基于雷赛驱动器）
            kb.Add(new FaultKnowledgeEntry {
                ErrorPattern = "-1",
                FaultType = "PARAMETER_ERROR",
                Severity = 2,
                Description = "参数错误或逻辑异常",
                PossibleCausesJson = JsonSerializer.Serialize(new[] {
                    "命令参数超出有效范围",
                    "速度或加速度参数配置不合理",
                    "PPR 值未正确配置"
                }),
                SuggestionsJson = JsonSerializer.Serialize(new[] {
                    "检查速度和加速度参数是否在允许范围内",
                    "验证 PPR 值配置是否正确",
                    "查看系统日志获取详细错误信息"
                })
            });

            kb.Add(new FaultKnowledgeEntry {
                ErrorPattern = "-2",
                FaultType = "COMMUNICATION_ERROR",
                Severity = 3,
                Description = "通信故障或设备不响应",
                PossibleCausesJson = JsonSerializer.Serialize(new[] {
                    "总线通信中断",
                    "驱动器掉线",
                    "网络超时",
                    "总线电缆松动或损坏"
                }),
                SuggestionsJson = JsonSerializer.Serialize(new[] {
                    "检查总线连接状态",
                    "重启驱动器",
                    "检查网络连接",
                    "更换总线电缆"
                })
            });

            kb.Add(new FaultKnowledgeEntry {
                ErrorPattern = "16",
                FaultType = "OVER_VOLTAGE",
                Severity = 2,
                Description = "过压保护",
                PossibleCausesJson = JsonSerializer.Serialize(new[] {
                    "输入电压过高",
                    "制动能量回馈过大",
                    "电源波动"
                }),
                SuggestionsJson = JsonSerializer.Serialize(new[] {
                    "检查输入电源电压",
                    "增加制动电阻",
                    "降低减速度参数",
                    "安装电压稳定器"
                })
            });

            kb.Add(new FaultKnowledgeEntry {
                ErrorPattern = "17",
                FaultType = "UNDER_VOLTAGE",
                Severity = 2,
                Description = "欠压保护",
                PossibleCausesJson = JsonSerializer.Serialize(new[] {
                    "输入电压过低",
                    "电源容量不足",
                    "电源线路压降过大"
                }),
                SuggestionsJson = JsonSerializer.Serialize(new[] {
                    "检查输入电源电压",
                    "更换更大容量的电源",
                    "缩短电源线长度或增大线径"
                })
            });

            kb.Add(new FaultKnowledgeEntry {
                ErrorPattern = "18",
                FaultType = "OVER_CURRENT",
                Severity = 3,
                Description = "过流保护",
                PossibleCausesJson = JsonSerializer.Serialize(new[] {
                    "负载过大",
                    "机械卡死",
                    "加速度设置过高",
                    "驱动器故障"
                }),
                SuggestionsJson = JsonSerializer.Serialize(new[] {
                    "检查机械负载是否正常",
                    "降低加速度参数",
                    "检查传动机构是否卡滞",
                    "检查驱动器是否损坏"
                })
            });

            kb.Add(new FaultKnowledgeEntry {
                ErrorPattern = "21",
                FaultType = "ENCODER_ERROR",
                Severity = 2,
                Description = "编码器故障",
                PossibleCausesJson = JsonSerializer.Serialize(new[] {
                    "编码器连接线松动",
                    "编码器损坏",
                    "编码器信号干扰"
                }),
                SuggestionsJson = JsonSerializer.Serialize(new[] {
                    "检查编码器连接线",
                    "更换编码器",
                    "增加屏蔽措施",
                    "远离干扰源"
                })
            });

            kb.Add(new FaultKnowledgeEntry {
                ErrorPattern = "25",
                FaultType = "POSITION_LIMIT",
                Severity = 1,
                Description = "位置限位保护",
                PossibleCausesJson = JsonSerializer.Serialize(new[] {
                    "触发硬件限位开关",
                    "软件限位参数设置",
                    "运动超出允许范围"
                }),
                SuggestionsJson = JsonSerializer.Serialize(new[] {
                    "检查限位开关状态",
                    "回零复位",
                    "调整软件限位参数",
                    "检查运动指令是否合理"
                })
            });

            _logger.LogInformation("故障知识库初始化完成，共 {Count} 条记录", kb.Count);
            return kb;
        }
    }
}
