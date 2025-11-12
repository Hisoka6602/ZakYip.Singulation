using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    /// <summary>
    /// 枚举架构过滤器，用于在 Swagger 文档中以字符串形式显示枚举类型并包含详细描述。
    /// </summary>
    /// <remarks>
    /// 此过滤器会：
    /// 1. 将枚举类型的 Schema 类型设置为字符串
    /// 2. 列出所有可能的枚举值
    /// 3. 从 DescriptionAttribute 提取每个枚举成员的描述
    /// 4. 在 Schema 描述中包含枚举值及其数值和描述信息
    /// </remarks>
    public class EnumSchemaFilter : ISchemaFilter {
        private readonly ILogger<EnumSchemaFilter>? _logger;

        /// <summary>
        /// 初始化 <see cref="EnumSchemaFilter"/> 类的新实例。
        /// </summary>
        /// <param name="logger">可选的日志记录器实例。</param>
        public EnumSchemaFilter(ILogger<EnumSchemaFilter>? logger = null) {
            _logger = logger;
        }

        /// <summary>
        /// 应用枚举架构过滤逻辑到 OpenAPI 架构。
        /// </summary>
        /// <param name="schema">要修改的 OpenAPI 架构对象。</param>
        /// <param name="context">包含类型信息的架构过滤器上下文。</param>
        /// <remarks>
        /// 仅处理枚举类型。对于每个枚举值，会提取其名称、整数值和 Description 特性（如果存在）。
        /// </remarks>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            SafeOperationHelper.SafeExecute(() => {
                var type = context.Type;

                // 只处理枚举
                if (type.IsEnum) {
                    // 枚举在文档里用 string 表示
                    schema.Type = "string";
                    schema.Enum.Clear();

                    var sb = new StringBuilder();
                    sb.AppendLine("可用值:");

                    // 把枚举的名称和描述写入
                    foreach (var name in Enum.GetNames(type)) {
                        schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(name));
                        
                        // 获取枚举成员的 Description 特性
                        var field = type.GetField(name);
                        var descAttr = field?.GetCustomAttribute<DescriptionAttribute>();
                        var value = Enum.Parse(type, name);
                        
                        if (descAttr != null) {
                            sb.AppendLine($"- {name} ({Convert.ToInt32(value)}): {descAttr.Description}");
                        } else {
                            sb.AppendLine($"- {name} ({Convert.ToInt32(value)})");
                        }
                    }

                    // 将描述添加到 schema 的 description 中
                    var descriptionText = sb.ToString();
                    if (!string.IsNullOrEmpty(schema.Description)) {
                        schema.Description += "\n\n" + descriptionText;
                    } else {
                        schema.Description = descriptionText;
                    }
                }
            }, _logger, "EnumSchemaFilter.Apply");
        }
    }
}