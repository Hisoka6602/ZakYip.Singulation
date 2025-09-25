using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    internal static class SwaggerGroupDiscovery {

        /// <summary>
        /// 发现所有 Swagger 分组（支持 MVC 控制器与 Minimal API）。
        /// </summary>
        /// <param name="partManager">MVC 的 ApplicationPartManager（只包含已注册的控制器）</param>
        /// <param name="endpointSources">可选：EndpointDataSource（用于发现 Minimal API 的 WithGroupName）</param>
        public static IReadOnlyCollection<string> DiscoverGroups(
            ApplicationPartManager partManager,
            IEnumerable<EndpointDataSource>? endpointSources = null) {
            // -------- MVC 控制器端 --------
            var feature = new ControllerFeature();
            partManager.PopulateFeature(feature);

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ci in feature.Controllers) {
                var t = ci.AsType();

                // 控制器级 GroupName
                var ctrlGroup = t.GetCustomAttribute<ApiExplorerSettingsAttribute>()?.GroupName;
                if (!string.IsNullOrWhiteSpace(ctrlGroup)) set.Add(ctrlGroup!);

                // 方法级 GroupName（覆盖控制器）
                foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public)) {
                    var hasHttp = m.GetCustomAttributes(inherit: true)
                                   .Any(a => a is Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute);
                    if (!hasHttp) continue;

                    var actGroup = m.GetCustomAttribute<ApiExplorerSettingsAttribute>()?.GroupName;
                    if (!string.IsNullOrWhiteSpace(actGroup)) set.Add(actGroup!);
                }
            }

            // -------- Minimal API 端（可选）--------
            // .NET 7/8 的 MapGroup().WithGroupName("xxx") 会向 Endpoint.Metadata 添加 ApiExplorerSettingsAttribute(GroupName=xxx)
            if (endpointSources != null) {
                foreach (var src in endpointSources) {
                    foreach (var ep in src.Endpoints) {
                        var group = ep.Metadata?
                            .OfType<ApiExplorerSettingsAttribute>()
                            .FirstOrDefault()
                            ?.GroupName;

                        if (!string.IsNullOrWhiteSpace(group)) set.Add(group!);
                    }
                }
            }

            if (set.Count == 0) set.Add("default");
            return set.ToArray();
        }
    }
}