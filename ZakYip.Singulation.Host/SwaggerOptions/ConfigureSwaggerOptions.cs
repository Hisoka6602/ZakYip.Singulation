using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions> {
        // 中文注释：保留构造函数，便于未来按需要使用 PartManager/Endpoint 数据。
        public ConfigureSwaggerOptions(
            ApplicationPartManager partManager,
            IEnumerable<EndpointDataSource> endpointSources) {
            _ = partManager;
            _ = endpointSources;
        }

        public void Configure(SwaggerGenOptions opt) {
            opt.SwaggerDoc("v1", new OpenApiInfo {
                Title = "ZakYip Singulation REST API",
                Version = "v1",
                Description = "面向分选控制系统的 RESTful 接口文档。"
            });
            opt.OperationFilter<CustomOperationFilter>();
            opt.SchemaFilter<HideLongListSchemaFilter>();
            opt.SchemaFilter<EnumSchemaFilter>();
            // d) SchemaId 稳定化（避免重复）
            opt.CustomSchemaIds(type => {
                if (type.IsGenericType) {
                    var def = type.GetGenericTypeDefinition().FullName ?? type.Name;
                    var args = string.Join("_", type.GetGenericArguments()
                        .Select(t => (t.FullName ?? t.Name).Replace('.', '_').Replace('+', '_')));
                    return $"{def}_{args}".Replace('.', '_').Replace('+', '_');
                }
                return (type.FullName ?? type.Name).Replace('.', '_').Replace('+', '_');
            });

            // e) 启用注解
            opt.EnableAnnotations();

            // 包含主程序集的 XML 注释
            var assembly = Assembly.GetExecutingAssembly();
            var xmlName = $"{assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
            if (File.Exists(xmlPath)) {
                opt.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // 包含 Core 项目的 XML 注释（用于 DTO、枚举等）
            var coreXmlPath = Path.Combine(AppContext.BaseDirectory, "ZakYip.Singulation.Core.xml");
            if (File.Exists(coreXmlPath)) {
                opt.IncludeXmlComments(coreXmlPath, includeControllerXmlComments: true);
            }

            // 中文注释：不要覆盖 Tag（不要使用 TagActionsBy），否则文档内的 Chute/Packages 等功能分组会被合并
        }
    }
}
