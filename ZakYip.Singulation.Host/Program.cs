using NLog.Web;
using System.Runtime;
using System.Text.Unicode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ZakYip.Singulation.Host.Safety;
using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Host.Runtime;
using ZakYip.Singulation.Host.Workers;
using Microsoft.AspNetCore.Diagnostics;
using ZakYip.Singulation.Host.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Drivers.Common;
using Microsoft.AspNetCore.Http.Features;
using ZakYip.Singulation.Host.Extensions;
using ZakYip.Singulation.Drivers.Registry;
using ZakYip.Singulation.Drivers.Leadshine;
using ZakYip.Singulation.Host.SignalR.Hubs;
using ZakYip.Singulation.Core.Contracts.Dto;
using ZakYip.Singulation.Host.SwaggerOptions;
using ZakYip.Singulation.Drivers.Abstractions;
using Microsoft.AspNetCore.ResponseCompression;
using ZakYip.Singulation.Protocol.Abstractions;
using ZakYip.Singulation.Infrastructure.Safety;
using ZakYip.Singulation.Infrastructure.Transport;
using ZakYip.Singulation.Protocol.Vendors.Huarary;
using ZakYip.Singulation.Core.Abstractions.Safety;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Infrastructure.Persistence;

ThreadPool.SetMinThreads(128, 128);
System.Runtime.GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

var host = Host.CreateDefaultBuilder(args)
    // ---------- 配置文件 ----------
    .ConfigureAppConfiguration((context, config) => {
        // 加载 appsettings.json（可按需再叠加环境配置）
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        // 注：如果需要环境特定文件，可添加：
        // var env = context.HostingEnvironment.EnvironmentName;
        // config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
    })
    // ---------- DI 服务注册 ----------
    .ConfigureServices((context, services) => {
        var configuration = context.Configuration;

        // ---------- HttpContextAccessor ----------
        services.AddHttpContextAccessor();

        // ---------- Controllers + Newtonsoft.Json ----------
        services
            .AddControllers(options => {
                // options.Filters.Add<LogRequestResponseAttribute>(); // 请求响应日志（如后续需要）
            })
            .AddJsonOptions(options => {
                // System.Text.Json 基础设置（备用）
                options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            })
            .AddNewtonsoftJson(options => {
                // 统一 JSON 行为（驼峰、忽略循环、日期格式、枚举字符串）
                var s = options.SerializerSettings;
                s.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                s.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
                s.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local;
                s.DateFormatString = "yyyy-MM-dd HH:mm:ss.fff";
                s.Formatting = (Newtonsoft.Json.Formatting)Newtonsoft.Json.Formatting.Indented;
                s.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            })
            // 将当前程序集里的控制器暴露给 MVC（避免跨项目找不到 Controller）
            .AddApplicationPart(typeof(Program).Assembly)
            .AddDataAnnotationsLocalization();

        // ---------- 模型验证失败统一响应 ----------
        services.Configure<ApiBehaviorOptions>(opt => {
            opt.InvalidModelStateResponseFactory = context => {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"[{x.Key}]:{x.Value?.Errors.FirstOrDefault()?.ErrorMessage}")
                    .ToList();

                return new JsonResult(new {
                    Result = false,
                    Msg = $"Body:{string.Join("|", errors)}"
                });
            };
        });

        // ---------- CORS ----------
        services.AddCors(options => {
            options.AddPolicy("CorsPolicy", builder => {
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowed(_ => true);
            });
        });

        // ---------- 表单上传（如有大文件场景） ----------
        services.Configure<FormOptions>(opt => {
            opt.MultipartBodyLengthLimit = long.MaxValue;
        });

        // ---------- Response Compression（零入侵性能增强） ----------
        services.AddResponseCompression(opt => {
            opt.EnableForHttps = true;
            opt.Providers.Add<BrotliCompressionProvider>();
            opt.Providers.Add<GzipCompressionProvider>();
        });

        // ---------- Swagger ----------
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // 使用 IConfigureOptions 延迟配置 Swagger（避免在此处 BuildServiceProvider）
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        // ---------- Hosted Services ----------

        // ---------- UDP 服务发现 ----------
        services.Configure<UdpDiscoveryOptions>(configuration.GetSection("UdpDiscovery"));
        services.AddHostedService<UdpDiscoveryService>();

        services.AddHostedService<SingulationWorker>();
        // ---------- 初始化轴 ----------
        services.AddHostedService<AxisBootstrapper>();

        // services.AddHostedService<RuntimeStatusReporter>();

        // ---------- SignalR ----------
        services.AddSingulationSignalR();
        // ---------- 配置存储 ----------
        services.AddLiteDbAxisSettings().AddLiteDbAxisLayout().AddUpstreamFromLiteDb().AddLiteDbLeadshineSafetyIo().AddLiteDbIoStatusMonitor();
        // ---------- 设备相关注入 ----------
        services.AddSingleton<IDriveRegistry>(sp => {
            var r = new DefaultDriveRegistry();
            r.Register("leadshine", (axisId, port, opts) => new LeadshineLtdmcAxisDrive(opts));
            // 未来在这里再注册其它品牌：
            // r.Register("Inovance", (axisId, port, opts) => new InovanceAxisDrive(opts));
            return r;
        });

        // Program.cs / DI 注册处
        services.AddSingleton<IBusAdapter>(sp => {
            var store = sp.GetRequiredService<IControllerOptionsStore>();

            // 1) 同步获取配置（LiteDB 本身是本地存取，同步拿即可）
            var dto = store.GetAsync().GetAwaiter().GetResult();

            //根据 Vendor 选择 BusAdapter（为未来扩展做分派）
            var vendor = dto.Vendor?.Trim().ToLowerInvariant();
            switch (vendor) {
                case "leadshine":
                case "ltdmc":
                    return new LeadshineLtdmcBusAdapter(
                        cardNo: (ushort)dto.Template.Card,
                        portNo: (ushort)dto.Template.Port,  // ← 修正强转
                        controllerIp: dto.ControllerIp
                    );

                // 未来接别的厂商：在这里新增 case
                // case "inovance": return new InovanceBusAdapter(...);

                default:
                    throw new NotSupportedException(
                        $"Unsupported controller vendor: '{dto.Vendor}'. " +
                        "Please update ControllerOptions.Vendor in LiteDB.");
            }
        });

        services.AddSingleton<IAxisEventAggregator, AxisEventAggregator>();
        services.AddSingleton<IAxisController, AxisController>();

        // ---------- IO 状态服务 ----------
        services.AddSingleton<IoStatusService>();
        services.AddHostedService<IoStatusWorker>();

        // ---------- 安全 ----------
        services.Configure<FrameGuardOptions>(configuration.GetSection("FrameGuard"));
        services.AddSingleton<ISafetyIsolator, SafetyIsolator>();

        // 注册 LoopbackSafetyIoModule
        services.AddSingleton<LoopbackSafetyIoModule>();

        // 注册 LeadshineSafetyIoModule（可能不会被使用，取决于配置）
        services.AddSingleton<LeadshineSafetyIoModule>(sp => {
            var logger = sp.GetRequiredService<ILogger<LeadshineSafetyIoModule>>();
            var safetyStore = sp.GetRequiredService<ILeadshineSafetyIoOptionsStore>();
            var options = safetyStore.GetAsync().GetAwaiter().GetResult();
            var busStore = sp.GetRequiredService<IControllerOptionsStore>();
            var busDto = busStore.GetAsync().GetAwaiter().GetResult();
            var cardNo = (ushort)busDto.Template.Card;
            return new LeadshineSafetyIoModule(logger, cardNo, options);
        });

        // 根据数据库配置选择安全 IO 模块实现
        services.AddSingleton<ISafetyIoModule>(sp => {
            var safetyStore = sp.GetRequiredService<ILeadshineSafetyIoOptionsStore>();
            var options = safetyStore.GetAsync().GetAwaiter().GetResult();

            if (options.Enabled) {
                // 使用硬件安全 IO 模块（雷赛控制器物理按键）
                return sp.GetRequiredService<LeadshineSafetyIoModule>();
            }
            else {
                // 使用回环测试模块（仅用于开发测试）
                return sp.GetRequiredService<LoopbackSafetyIoModule>();
            }
        });

        // 注册指示灯服务
        services.AddSingleton<IndicatorLightService>(sp => {
            var logger = sp.GetRequiredService<ILogger<IndicatorLightService>>();
            var safetyStore = sp.GetRequiredService<ILeadshineSafetyIoOptionsStore>();
            var options = safetyStore.GetAsync().GetAwaiter().GetResult();
            var busStore = sp.GetRequiredService<IControllerOptionsStore>();
            var busDto = busStore.GetAsync().GetAwaiter().GetResult();
            var cardNo = (ushort)busDto.Template.Card;
            return new IndicatorLightService(logger, cardNo, options);
        });

        services.AddSingleton<FrameGuard>();
        services.AddSingleton<IFrameGuard>(sp => sp.GetRequiredService<FrameGuard>());
        services.AddSingleton<SafetyPipeline>();
        services.AddSingleton<ISafetyPipeline>(sp => sp.GetRequiredService<SafetyPipeline>());
        services.AddHostedService(sp => sp.GetRequiredService<SafetyPipeline>());
        // ---------- 上游数据连接Tcp相关注入 ----------
        services.AddUpstreamTcpFromLiteDb();
        // ---------- 解码器相关注入 ----------
        services.AddSingleton<IUpstreamCodec>(provider => {
            var store = provider.GetRequiredService<IUpstreamCodecOptionsStore>();

            // 获取配置（若不存在则返回默认）
            var options = store.GetAsync().GetAwaiter().GetResult();

            store.UpsertAsync(options).GetAwaiter().GetResult();

            return new HuararyCodec(
                mainCount: options.MainCount,
                ejectCount: options.EjectCount
            );
        });
        // ---------- 速度多发 ----------
        services.AddSingleton<IUpstreamFrameHub, UpstreamFrameHub>();
        // ---------- 事件泵 ----------
        services.AddHostedService<TransportEventPump>();
        // ---------- 速度执行器 ----------
        services.AddHostedService<SpeedFrameWorker>();
        // ---------- 心跳执行器 ----------
        services.AddHostedService<HeartbeatWorker>();
        // ---------- 日志泵 ----------
        services.AddSingleton<LogEventBus>();
        services.AddHostedService<LogEventPump>();
        // ---------- 日志清理 ----------
        services.AddHostedService<LogsCleanupService>();
    })
    // ---------- 日志 ----------
    .ConfigureLogging(logging => {
        // 压低控制台输出，避免 IO 压力；生产建议用 NLog/Serilog 收敛日志
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Warning);
    })
    .ConfigureWebHostDefaults(webBuilder => {
        webBuilder.ConfigureKestrel((context, options) => {
            // ---------- Kestrel ----------
            var url = "http://localhost:5005";
#if !DEBUG
            url = context.Configuration.GetValue<string>("KestrelUrl", "http://localhost:5005");
#endif
            webBuilder.UseUrls(url);

            // 请求体上限（按需调整）
            options.Limits.MaxRequestBodySize = 30L * 1024 * 1024 * 1024; // 30GB
        });

        webBuilder.Configure((context, app) => {
            // ---------- Response Compression ----------
            app.UseResponseCompression(); // 早启用，静态与 API 都受益

            // ---------- 全局异常处理 ----------
            app.UseExceptionHandler(errorApp => {
                errorApp.Run(async httpContext => {
                    httpContext.Response.StatusCode = 500;
                    httpContext.Response.ContentType = "application/json";
                    var ex = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
                    NLog.LogManager.GetCurrentClassLogger().Error($"系统异常 {ex}");
                    await httpContext.Response.WriteAsJsonAsync(new {
                        Result = false,
                        Msg = "系统异常"
                    });
                });
            });

            // ---------- 常规中间件 ----------
            app.UseRouting();
            app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            // ---------- 全局请求体缓存（允许后续组件重复读取；注意内存/IO 开销） ----------
            app.Use(next => async http => {
                http.Request.EnableBuffering();
                await next(http);
            });

            // ---------- Swagger ----------
            app.UseSwagger();
            app.UseSwaggerUI(opt => {
                opt.RoutePrefix = "swagger";
                opt.DocumentTitle = "ZakYip.Singulation ― API 文档";
                opt.SwaggerEndpoint("/swagger/v1/swagger.json", "ZakYip.Singulation API v1");
            });

            // ---------- 终结点 ----------
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapHub<EventsHub>("/hubs/events");

                // 如需健康检查（若项目已添加 AddHealthChecks）
                // endpoints.MapHealthChecks("/health");
            });
        });
    })
    // ---------- Windows Service ----------
#if !DEBUG
    .UseWindowsService()
#endif
    // ---------- NLog ----------
    .UseNLog()
    // ---------- 构建 Host ----------
    .Build();

try {
    // 阻止电脑睡眠/熄屏
    PowerGuard.SetThreadExecutionState(
        PowerGuard.EXECUTION_STATE.ES_CONTINUOUS |
        PowerGuard.EXECUTION_STATE.ES_SYSTEM_REQUIRED |
        PowerGuard.EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    host.Run();
}
catch (Exception e) {
    NLog.LogManager.GetCurrentClassLogger().Error(e, "运行异常");
}
finally {
    PowerGuard.SetThreadExecutionState(PowerGuard.EXECUTION_STATE.ES_CONTINUOUS);
}