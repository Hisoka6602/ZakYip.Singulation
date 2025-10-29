using System.Reflection;
using System.ComponentModel;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    public class HideLongListSchemaFilter : ISchemaFilter {
        private readonly ILogger<HideLongListSchemaFilter>? _logger;

        public HideLongListSchemaFilter(ILogger<HideLongListSchemaFilter>? logger = null) {
            _logger = logger;
        }

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