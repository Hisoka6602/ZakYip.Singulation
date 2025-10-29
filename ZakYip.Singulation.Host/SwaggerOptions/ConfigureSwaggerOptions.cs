using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions> {
        private readonly ILogger<ConfigureSwaggerOptions> _logger;

        // 中文注释：保留构造函数，便于未来按需要使用 PartManager/Endpoint 数据。
        public ConfigureSwaggerOptions(
            ApplicationPartManager partManager,
            IEnumerable<EndpointDataSource> endpointSources,
            ILogger<ConfigureSwaggerOptions> logger) {
            _ = partManager;
            _ = endpointSources;
            _logger = logger;
        }

        public void Configure(SwaggerGenOptions opt) {
            opt.SwaggerDoc("v1", new OpenApiInfo {
                Title = "ZakYip Singulation REST API",
                Version = "v1",
                Description = "面向分选控制系统的 RESTful 接口文档。"
            });

            // 安全执行：添加操作过滤器
            SafeOperationHelper.SafeExecute(
                () => opt.OperationFilter<CustomOperationFilter>(),
                _logger,
                "添加 CustomOperationFilter");

            // 安全执行：添加 Schema 过滤器
            SafeOperationHelper.SafeExecute(
                () => opt.SchemaFilter<HideLongListSchemaFilter>(),
                _logger,
                "添加 HideLongListSchemaFilter");

            SafeOperationHelper.SafeExecute(
                () => opt.SchemaFilter<EnumSchemaFilter>(),
                _logger,
                "添加 EnumSchemaFilter");

            // d) SchemaId 稳定化（避免重复）
            SafeOperationHelper.SafeExecute(() => {
                opt.CustomSchemaIds(type => {
                    if (type.IsGenericType) {
                        var def = type.GetGenericTypeDefinition().FullName ?? type.Name;
                        var args = string.Join("_", type.GetGenericArguments()
                            .Select(t => (t.FullName ?? t.Name).Replace('.', '_').Replace('+', '_')));
                        return $"{def}_{args}".Replace('.', '_').Replace('+', '_');
                    }
                    return (type.FullName ?? type.Name).Replace('.', '_').Replace('+', '_');
                });
            }, _logger, "配置 CustomSchemaIds");

            // e) 启用注解
            SafeOperationHelper.SafeExecute(
                () => opt.EnableAnnotations(),
                _logger,
                "启用 Swagger 注解");

            // 包含主程序集的 XML 注释（使用安全隔离器）
            SafeOperationHelper.SafeExecute(() => {
                var assembly = Assembly.GetExecutingAssembly();
                var xmlName = $"{assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
                if (File.Exists(xmlPath)) {
                    opt.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                    _logger.LogInformation("已加载 Swagger XML 文档: {XmlPath}", xmlPath);
                }
                else {
                    _logger.LogWarning("未找到 Swagger XML 文档: {XmlPath}", xmlPath);
                }
            }, _logger, "加载主程序集 XML 注释");

            // 包含 Core 项目的 XML 注释（用于 DTO、枚举等）（使用安全隔离器）
            SafeOperationHelper.SafeExecute(() => {
                var coreXmlPath = Path.Combine(AppContext.BaseDirectory, "ZakYip.Singulation.Core.xml");
                if (File.Exists(coreXmlPath)) {
                    opt.IncludeXmlComments(coreXmlPath, includeControllerXmlComments: true);
                    _logger.LogInformation("已加载 Core XML 文档: {XmlPath}", coreXmlPath);
                }
                else {
                    _logger.LogWarning("未找到 Core XML 文档: {XmlPath}", coreXmlPath);
                }
            }, _logger, "加载 Core 项目 XML 注释");

            // 中文注释：不要覆盖 Tag（不要使用 TagActionsBy），否则文档内的 Chute/Packages 等功能分组会被合并
        }
    }
}
