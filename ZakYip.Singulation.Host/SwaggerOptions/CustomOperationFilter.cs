using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    public class CustomOperationFilter : IOperationFilter {

        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            // 获取路由信息
            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor) {
                var routeTemplate = controllerActionDescriptor.AttributeRouteInfo?.Template;
                if (!string.IsNullOrEmpty(routeTemplate)) {
                    // 将路由路径添加到描述中
                    operation.Description += $"\n\n路由路径: /{routeTemplate}";
                }
            }
        }
    }
}