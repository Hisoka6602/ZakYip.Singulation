using LiteDB;
using System;
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
        /// <param name="filePath">数据库文件路径。</param>
        public static IServiceCollection AddLiteDbAxisSettings(this IServiceCollection services, string filePath = "singulation.db") {
            services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase($"Filename={filePath};Mode=Shared"));
            services.AddSingleton<IAxisSettingsStore, LiteDbAxisSettingsStore>();
            return services;
        }

        /// <summary>注册轴布局存储（与设置共享同一 LiteDB）。</summary>
        public static IServiceCollection AddLiteDbAxisLayout(this IServiceCollection services) {
            services.AddSingleton<IAxisLayoutStore, LiteDbAxisLayoutStore>();
            return services;
        }
    }
}