using LiteDB;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Infrastructure.Persistence.Vendors.Leadshine;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>持久化注册扩展。</summary>
    public static class PersistenceServiceCollectionExtensions {

        /// <summary>
        /// 使用 LiteDB 作为轴设置存储，直接存储 AxisSettingsDto。
        /// </summary>
        /// <param name="services">DI 容器。</param>
        /// <param name="filePath">数据库文件路径。默认存储在 data 目录下。</param>
        public static IServiceCollection AddLiteDbAxisSettings(this IServiceCollection services, string filePath = "data/singulation.db") {
            services.AddSingleton<ILiteDatabase>(_ => {
                // 防御性处理 filePath，确保不是 null 或空字符串
                var safeFilePath = string.IsNullOrWhiteSpace(filePath) ? "data/singulation.db" : filePath;
                // 如果是相对路径，则相对于应用程序基目录解析
                var resolvedPath = Path.IsPathRooted(safeFilePath)
                    ? safeFilePath
                    : Path.Combine(AppContext.BaseDirectory, safeFilePath);
                
                // 确保数据库文件所在目录存在
                var directory = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                
                // 优化的连接字符串配置：
                // - Mode=Shared: 允许多个进程/线程共享数据库
                // - Connection=Direct: 使用直接连接以提升性能
                // - Upgrade=true: 自动升级数据库结构
                // - Collation: 使用二进制排序以提升性能
                var connectionString = $"Filename={resolvedPath};Mode=Shared;Connection=Direct;Upgrade=true;Collation=en-US/None";
                return new LiteDatabase(connectionString);
            });
            services.AddSingleton<IControllerOptionsStore, LiteDbControllerOptionsStore>();
            return services;
        }

        /// <summary>注册轴布局存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbAxisLayout(this IServiceCollection services) {
            services.AddSingleton<IAxisLayoutStore, LiteDbAxisLayoutStore>();
            return services;
        }

        /// <summary>注册雷赛控制面板 IO 配置存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbLeadshineCabinetIo(this IServiceCollection services) {
            services.AddSingleton<ILeadshineCabinetIoOptionsStore, LiteDbLeadshineCabinetIoOptionsStore>();
            return services;
        }

        /// <summary>注册 IO 状态监控配置存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbIoStatusMonitor(this IServiceCollection services) {
            services.AddSingleton<IIoStatusMonitorOptionsStore, LiteDbIoStatusMonitorOptionsStore>();
            return services;
        }

        /// <summary>注册 IO 联动配置存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbIoLinkage(this IServiceCollection services) {
            services.AddSingleton<IIoLinkageOptionsStore, LiteDbIoLinkageOptionsStore>();
            return services;
        }

        /// <summary>注册速度联动配置存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbSpeedLinkage(this IServiceCollection services) {
            services.AddSingleton<ISpeedLinkageOptionsStore, LiteDbSpeedLinkageOptionsStore>();
            return services;
        }
    }
}