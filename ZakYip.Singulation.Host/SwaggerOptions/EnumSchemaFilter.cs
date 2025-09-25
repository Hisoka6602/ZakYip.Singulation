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

                // 把枚举的名称写入
                foreach (var name in Enum.GetNames(type)) {
                    schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(name));
                }
            }
        }
    }
}