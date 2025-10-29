using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    public class CustomOperationFilter : IOperationFilter {
        private readonly ILogger<CustomOperationFilter>? _logger;

        public CustomOperationFilter(ILogger<CustomOperationFilter>? logger = null) {
            _logger = logger;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            SafeOperationHelper.SafeExecute(() => {
                // 获取路由信息
                if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor) {
                    var routeTemplate = controllerActionDescriptor.AttributeRouteInfo?.Template;
                    if (!string.IsNullOrEmpty(routeTemplate)) {
                        // 将路由路径添加到描述中
                        operation.Description += $"\n\n路由路径: /{routeTemplate}";
                    }
                }
            }, _logger, "CustomOperationFilter.Apply");
        }
    }
}