using System.Reflection;
using System.ComponentModel;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    /// <summary>
    /// 架构过滤器，用于简化长类型名称并为枚举类型添加详细描述。
    /// </summary>
    /// <remarks>
    /// 此过滤器执行以下操作：
    /// 1. 对于 ZakYip 命名空间下的类型，生成友好的短名称作为 Schema 标题
    /// 2. 对于枚举类型，在描述中列出所有可能的值及其整数值和 Description 特性
    /// 这有助于在 Swagger UI 中提供更清晰、更易读的 API 文档。
    /// </remarks>
    public class HideLongListSchemaFilter : ISchemaFilter {
        private readonly ILogger<HideLongListSchemaFilter>? _logger;

        /// <summary>
        /// 初始化 <see cref="HideLongListSchemaFilter"/> 类的新实例。
        /// </summary>
        /// <param name="logger">可选的日志记录器实例。</param>
        public HideLongListSchemaFilter(ILogger<HideLongListSchemaFilter>? logger = null) {
            _logger = logger;
        }

        /// <summary>
        /// 应用架构过滤逻辑到 OpenAPI 架构。
        /// </summary>
        /// <param name="schema">要修改的 OpenAPI 架构对象。</param>
        /// <param name="context">包含类型信息的架构过滤器上下文。</param>
        /// <remarks>
        /// 此方法处理两种情况：
        /// 1. ZakYip 命名空间的类型：设置友好的 Schema 标题
        /// 2. 枚举类型：在描述中添加所有枚举值及其说明
        /// </remarks>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
            SafeOperationHelper.SafeExecute(() => {
                var type = context.Type;

                // 检查类型名称是否包含 "ZakYip"，以确定是否需要处理
                if (type.FullName != null && type.FullName.Contains("ZakYip")) {
                    // 获取简短类名
                    var shortName = GetFriendlyTypeName(type);

                    // 设置 schema.Title
                    schema.Title = shortName;
                }

                if (context.Type.IsEnum) {
                    // 获取枚举值和名称
                    var enumValues = Enum.GetValues(context.Type).Cast<object>().ToArray();
                    var enumNames = enumValues.Select(v => v.ToString()).ToArray();
                    var enumValuesAsInt = enumValues.Select(v => (int)v).ToArray();

                    // 获取枚举值的 Description 属性
                    var enumDescriptions = enumNames.Select(name => {
                        if (name != null) {
                            var field = context.Type.GetField(name);
                            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
                            return attribute?.Description ?? name;
                        }

                        return null;
                    }).ToArray();

                    // 设置 schema 的描述
                    schema.Description = $"Enum values: {string.Join(", ", enumNames.Zip(enumValuesAsInt, (name, value) => $"{value}={name}[{enumDescriptions[Array.IndexOf(enumNames, name)]}]"))}";
                }
            }, _logger, "HideLongListSchemaFilter.Apply");
        }

        /// <summary>
        /// 获取类型的友好名称，处理泛型类型的嵌套参数。
        /// </summary>
        /// <param name="type">要获取友好名称的类型。</param>
        /// <returns>简化后的类型名称字符串。</returns>
        /// <remarks>
        /// 此方法递归处理泛型类型参数，生成格式如 "TypeName.Arg1AndArg2" 的名称。
        /// </remarks>
        private string GetFriendlyTypeName(Type type) {
            // 获取简短类名（去掉泛型标记）
            var typeName = type.Name;
            if (typeName.Contains('`')) {
                typeName = typeName.Substring(0, typeName.IndexOf('`'));
            }

            // 如果是泛型类型，处理泛型参数
            if (type.IsGenericType) {
                var genericArguments = type.GetGenericArguments();
                var genericArgumentNames = genericArguments.Select(GetFriendlyTypeName).ToArray(); // 递归处理泛型参数
                var genericArgumentString = string.Join("And", genericArgumentNames);
                typeName = $"{typeName}.{genericArgumentString}";
            }

            return typeName;
        }
    }
}