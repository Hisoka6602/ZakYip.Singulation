using System;
using Microsoft.Extensions.Logging;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Drivers.Common {
    /// <summary>
    /// 轴状态验证器：提供统一的状态转换验证和日志记录功能。
    /// 用于解决状态转换逻辑分散在多个类中导致的状态不一致和调试困难问题。
    /// </summary>
    public static class AxisStateValidator {
        /// <summary>
        /// 验证轴驱动状态转换的有效性。
        /// </summary>
        /// <param name="axis">轴标识</param>
        /// <param name="currentStatus">当前状态</param>
        /// <param name="targetStatus">目标状态</param>
        /// <param name="operation">操作名称</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>如果转换有效返回 true，否则返回 false</returns>
        public static bool ValidateDriverStatusTransition(
            AxisId axis,
            DriverStatus currentStatus,
            DriverStatus targetStatus,
            string operation,
            ILogger? logger = null) {
            
            // 允许的状态转换规则
            var isValid = (currentStatus, targetStatus) switch {
                // 从 Disconnected 可以转换到任何状态（初始化或重连）
                (DriverStatus.Disconnected, _) => true,
                
                // 从 Initializing 可以转换到 Connected 或 Degraded（初始化成功或失败）
                (DriverStatus.Initializing, DriverStatus.Connected) => true,
                (DriverStatus.Initializing, DriverStatus.Degraded) => true,
                (DriverStatus.Initializing, DriverStatus.Faulted) => true,
                
                // 从 Connected 可以转换到 Degraded、Recovering 或 Disconnected
                (DriverStatus.Connected, DriverStatus.Degraded) => true,
                (DriverStatus.Connected, DriverStatus.Recovering) => true,
                (DriverStatus.Connected, DriverStatus.Disconnected) => true,
                (DriverStatus.Connected, DriverStatus.Disabled) => true,
                (DriverStatus.Connected, DriverStatus.Disposed) => true,
                
                // 从 Degraded 可以转换到 Recovering、Faulted 或 Disconnected
                (DriverStatus.Degraded, DriverStatus.Recovering) => true,
                (DriverStatus.Degraded, DriverStatus.Faulted) => true,
                (DriverStatus.Degraded, DriverStatus.Disconnected) => true,
                (DriverStatus.Degraded, DriverStatus.Connected) => true,
                (DriverStatus.Degraded, DriverStatus.Disposed) => true,
                
                // 从 Recovering 可以转换到 Connected、Degraded 或 Faulted
                (DriverStatus.Recovering, DriverStatus.Connected) => true,
                (DriverStatus.Recovering, DriverStatus.Degraded) => true,
                (DriverStatus.Recovering, DriverStatus.Faulted) => true,
                (DriverStatus.Recovering, DriverStatus.Disconnected) => true,
                
                // 从 Disabled 可以转换到 Connected 或 Disconnected
                (DriverStatus.Disabled, DriverStatus.Connected) => true,
                (DriverStatus.Disabled, DriverStatus.Disconnected) => true,
                (DriverStatus.Disabled, DriverStatus.Disposed) => true,
                
                // 从 Faulted 可以转换到 Recovering 或 Disconnected
                (DriverStatus.Faulted, DriverStatus.Recovering) => true,
                (DriverStatus.Faulted, DriverStatus.Disconnected) => true,
                (DriverStatus.Faulted, DriverStatus.Disposed) => true,
                
                // 任何状态都可以转换到 Disposed（清理）
                (_, DriverStatus.Disposed) => true,
                
                // 同状态转换（通常是幂等操作）
                _ when currentStatus == targetStatus => true,
                
                // 其他转换不允许
                _ => false
            };
            
            if (!isValid) {
                logger?.LogWarning(
                    "[状态转换验证失败] 轴={Axis}, 操作={Operation}, 当前状态={Current}, 目标状态={Target}, 原因=不允许的状态转换",
                    axis, operation, currentStatus, targetStatus);
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 验证机柜隔离状态转换的有效性。
        /// </summary>
        /// <param name="currentState">当前状态</param>
        /// <param name="targetState">目标状态</param>
        /// <param name="operation">操作名称</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>如果转换有效返回 true，否则返回 false</returns>
        public static bool ValidateCabinetStateTransition(
            CabinetIsolationState currentState,
            CabinetIsolationState targetState,
            string operation,
            ILogger? logger = null) {
            
            // 允许的状态转换规则
            var isValid = (currentState, targetState) switch {
                // 从 Normal 可以转换到 Degraded 或 Isolated
                (CabinetIsolationState.Normal, CabinetIsolationState.Degraded) => true,
                (CabinetIsolationState.Normal, CabinetIsolationState.Isolated) => true,
                
                // 从 Degraded 可以转换到 Normal 或 Isolated
                (CabinetIsolationState.Degraded, CabinetIsolationState.Normal) => true,
                (CabinetIsolationState.Degraded, CabinetIsolationState.Isolated) => true,
                
                // 从 Isolated 只能通过 Reset 转换到 Normal
                (CabinetIsolationState.Isolated, CabinetIsolationState.Normal) => true,
                
                // 同状态转换（幂等操作）
                _ when currentState == targetState => true,
                
                // 其他转换不允许
                _ => false
            };
            
            if (!isValid) {
                logger?.LogWarning(
                    "[状态转换验证失败] 操作={Operation}, 当前状态={Current}, 目标状态={Target}, 原因=不允许的状态转换",
                    operation, currentState, targetState);
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 记录轴驱动状态转换日志。
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="axis">轴标识</param>
        /// <param name="fromStatus">原状态</param>
        /// <param name="toStatus">新状态</param>
        /// <param name="operation">触发操作</param>
        /// <param name="reason">转换原因（可选）</param>
        public static void LogDriverStatusTransition(
            ILogger logger,
            AxisId axis,
            DriverStatus fromStatus,
            DriverStatus toStatus,
            string operation,
            string? reason = null) {
            
            if (fromStatus == toStatus) {
                // 同状态转换，使用 Debug 级别
                logger.LogDebug(
                    "[轴状态保持] 轴={Axis}, 状态={Status}, 操作={Operation}",
                    axis, toStatus, operation);
                return;
            }
            
            var reasonText = string.IsNullOrWhiteSpace(reason) ? "" : $", 原因={reason}";
            
            // 根据目标状态选择日志级别
            var logLevel = toStatus switch {
                DriverStatus.Faulted => LogLevel.Error,
                DriverStatus.Degraded => LogLevel.Warning,
                DriverStatus.Disconnected when fromStatus != DriverStatus.Disabled => LogLevel.Warning,
                _ => LogLevel.Information
            };
            
            logger.Log(logLevel,
                "[轴状态转换] 轴={Axis}, 操作={Operation}, 从 {FromStatus} 转换到 {ToStatus}{Reason}",
                axis, operation, fromStatus, toStatus, reasonText);
        }
        
        /// <summary>
        /// 记录机柜隔离状态转换日志。
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="fromState">原状态</param>
        /// <param name="toState">新状态</param>
        /// <param name="operation">触发操作</param>
        /// <param name="reason">转换原因（可选）</param>
        public static void LogCabinetStateTransition(
            ILogger logger,
            CabinetIsolationState fromState,
            CabinetIsolationState toState,
            string operation,
            string? reason = null) {
            
            if (fromState == toState) {
                // 同状态转换，使用 Debug 级别
                logger.LogDebug(
                    "[机柜状态保持] 状态={State}, 操作={Operation}",
                    toState, operation);
                return;
            }
            
            var reasonText = string.IsNullOrWhiteSpace(reason) ? "" : $", 原因={reason}";
            
            // 根据目标状态选择日志级别
            var logLevel = toState switch {
                CabinetIsolationState.Isolated => LogLevel.Error,
                CabinetIsolationState.Degraded => LogLevel.Warning,
                CabinetIsolationState.Normal => LogLevel.Information,
                _ => LogLevel.Information
            };
            
            logger.Log(logLevel,
                "[机柜状态转换] 操作={Operation}, 从 {FromState} 转换到 {ToState}{Reason}",
                operation, fromState, toState, reasonText);
        }
        
        /// <summary>
        /// 验证操作前提条件：检查驱动状态是否允许执行指定操作。
        /// </summary>
        /// <param name="axis">轴标识</param>
        /// <param name="currentStatus">当前状态</param>
        /// <param name="operation">操作名称</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>如果允许操作返回 true，否则返回 false</returns>
        public static bool ValidateOperationPrecondition(
            AxisId axis,
            DriverStatus currentStatus,
            string operation,
            ILogger? logger = null) {
            
            // 定义每个操作允许的状态
            var isValid = operation.ToLowerInvariant() switch {
                "enable" or "enableasync" => currentStatus is DriverStatus.Disconnected or DriverStatus.Disabled or DriverStatus.Connected,
                "disable" or "disableasync" => currentStatus is DriverStatus.Connected or DriverStatus.Degraded or DriverStatus.Faulted,
                "writespeed" or "writespeedasync" => currentStatus is DriverStatus.Connected or DriverStatus.Degraded,
                "stop" or "stopasync" => currentStatus is not DriverStatus.Disposed,
                "setacceldecel" or "setacceldecelasync" => currentStatus is DriverStatus.Connected or DriverStatus.Degraded,
                _ => true // 其他操作不限制
            };
            
            if (!isValid) {
                logger?.LogWarning(
                    "[操作前提条件验证失败] 轴={Axis}, 操作={Operation}, 当前状态={Status}, 原因=当前状态不允许执行此操作",
                    axis, operation, currentStatus);
            }
            
            return isValid;
        }
    }
}
