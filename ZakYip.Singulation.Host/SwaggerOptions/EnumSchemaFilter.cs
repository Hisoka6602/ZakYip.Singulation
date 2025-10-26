using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    public class EnumSchemaFilter : ISchemaFilter {

        public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
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
                if (!string.IsNullOrEmpty(schema.Description)) {
                    schema.Description += "\n\n" + sb.ToString();
                } else {
                    schema.Description = sb.ToString();
                }
            }
        }
    }
}