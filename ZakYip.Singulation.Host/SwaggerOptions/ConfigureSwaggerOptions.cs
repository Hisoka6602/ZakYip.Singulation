using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace ZakYip.Singulation.Host.SwaggerOptions {

    internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions> {
        private readonly ApplicationPartManager _partManager;
        private readonly IEnumerable<EndpointDataSource> _endpointSources;

        // 中文注释：DI 注入 MVC 的 PartManager + EndpointDataSource（用于 Minimal API）
        public ConfigureSwaggerOptions(
            ApplicationPartManager partManager,
            IEnumerable<EndpointDataSource> endpointSources) {
            _partManager = partManager;
            _endpointSources = endpointSources;
        }

        public void Configure(SwaggerGenOptions opt) {
            // a) 自动发现分组（统一调用工具类）
            /*var groups = SwaggerGroupDiscovery.DiscoverGroups(_partManager, _endpointSources);

            // b) 为每个分组注册文档
            foreach (var g in groups.OrderBy(x => x)) {
                opt.SwaggerDoc(g, new OpenApiInfo { Title = $"泽业Scs—Api {g}", Version = g });
            }

            // c) 严格把 GroupName 匹配的接口纳入对应文档（含 EndpointMetadata 兜底）
            opt.DocInclusionPredicate((docName, apiDesc) => {
                var groupName = apiDesc.GroupName
                    ?? apiDesc.ActionDescriptor?.EndpointMetadata?
                        .OfType<ApiExplorerSettingsAttribute>()
                        .FirstOrDefault()?.GroupName;

                return string.Equals(groupName, docName, StringComparison.OrdinalIgnoreCase);
            });*/
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

            // f) Include XML（主程序 + 应用层 + 插件 XML 与 DLL 同名）
            /*var basePath = AppContext.BaseDirectory;

            var mainXml = Path.Combine(basePath, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(mainXml)) opt.IncludeXmlComments(mainXml, true);

            var appXml = Path.Combine(basePath, "ZakYip.Scs.Application.xml");
            if (File.Exists(appXml)) opt.IncludeXmlComments(appXml, true);

            var pluginsRoot = Path.Combine(basePath, "Plugins");
            if (Directory.Exists(pluginsRoot)) {
                var xmlFiles = Directory.GetFiles(pluginsRoot, "*.xml", SearchOption.AllDirectories);
                foreach (var xml in xmlFiles) {
                    try {
                        var dll = Path.Combine(Path.GetDirectoryName(xml)!, Path.GetFileNameWithoutExtension(xml) + ".dll");
                        if (File.Exists(dll)) opt.IncludeXmlComments(xml, true);
                    }
                    catch (Exception ex) {
                        NLog.LogManager.GetCurrentClassLogger().Error($"加载 XML 失败: {xml}, 错误: {ex.Message}");
                    }
                }
            }*/

            // 中文注释：不要覆盖 Tag（不要使用 TagActionsBy），否则文档内的 Chute/Packages 等功能分组会被合并
        }
    }
}