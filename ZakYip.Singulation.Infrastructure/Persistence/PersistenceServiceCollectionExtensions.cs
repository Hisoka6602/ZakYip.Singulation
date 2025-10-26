using LiteDB;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

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
                // 如果是相对路径，则相对于应用程序基目录解析
                var resolvedPath = Path.IsPathRooted(filePath)
                    ? filePath
                    : Path.Combine(AppContext.BaseDirectory, filePath);
                
                // 确保数据库文件所在目录存在
                var directory = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                return new LiteDatabase($"Filename={resolvedPath};Mode=Shared");
            });
            services.AddSingleton<IControllerOptionsStore, LiteDbControllerOptionsStore>();
            return services;
        }

        /// <summary>注册轴布局存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbAxisLayout(this IServiceCollection services) {
            services.AddSingleton<IAxisLayoutStore, LiteDbAxisLayoutStore>();
            return services;
        }

        /// <summary>注册雷赛安全 IO 配置存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbLeadshineSafetyIo(this IServiceCollection services) {
            services.AddSingleton<ILeadshineSafetyIoOptionsStore, LiteDbLeadshineSafetyIoOptionsStore>();
            return services;
        }

        /// <summary>注册 IO 状态监控配置存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbIoStatusMonitor(this IServiceCollection services) {
            services.AddSingleton<IIoStatusMonitorOptionsStore, LiteDbIoStatusMonitorOptionsStore>();
            return services;
        }
    }
}