using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    /// <summary>
    /// 自定义 Swagger 操作过滤器，用于在 API 文档中添加额外的路由信息。
    /// </summary>
    /// <remarks>
    /// 此过滤器会将控制器的路由模板路径添加到操作描述中，
    /// 帮助 API 使用者更好地理解端点的完整路由信息。
    /// </remarks>
    public class CustomOperationFilter : IOperationFilter {
        private readonly ILogger<CustomOperationFilter>? _logger;

        /// <summary>
        /// 初始化 <see cref="CustomOperationFilter"/> 类的新实例。
        /// </summary>
        /// <param name="logger">可选的日志记录器实例，用于记录操作过滤过程中的信息。</param>
        public CustomOperationFilter(ILogger<CustomOperationFilter>? logger = null) {
            _logger = logger;
        }

        /// <summary>
        /// 应用过滤器逻辑到 Swagger 操作定义。
        /// </summary>
        /// <param name="operation">要修改的 OpenAPI 操作对象。</param>
        /// <param name="context">包含操作上下文信息的过滤器上下文。</param>
        /// <remarks>
        /// 此方法会从控制器操作描述符中提取路由模板，
        /// 并将其追加到操作的描述字段中。
        /// </remarks>
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