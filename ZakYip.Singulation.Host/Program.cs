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
using ZakYip.Singulation.Host.Workers;
using ZakYip.Singulation.Core.Configs;
using Microsoft.AspNetCore.Diagnostics;
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
using ZakYip.Singulation.Infrastructure.Transport;
using ZakYip.Singulation.Protocol.Vendors.Huarary;
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
        services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new OpenApiInfo {
                Title = "ZakYip.Singulation API",
                Version = "v1"
            });
            // 如需 XML 注释：
            // var xml = Path.Combine(AppContext.BaseDirectory, "ZakYip.Singulation.Host.xml");
            // if (File.Exists(xml)) c.IncludeXmlComments(xml, includeControllerXmlComments: true);
        });

        // 使用 IConfigureOptions 延迟配置 Swagger（避免在此处 BuildServiceProvider）
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        // ---------- Hosted Services ----------

        services.AddHostedService<SingulationWorker>();

        // 如果你有运行时状态上报/心跳等后台任务，可以按需再加：
        // services.AddHostedService<RuntimeStatusReporter>();

        // ---------- SignalR ----------
        services.AddSingulationSignalR();
        // ---------- 配置存储 ----------
        services.AddLiteDbAxisSettings().AddLiteDbAxisLayout().AddUpstreamFromLiteDb();
        // ---------- 设备相关注入 ----------
        services.AddSingleton<IDriveRegistry, DefaultDriveRegistry>();

        services.AddSingleton<IBusAdapter>(serviceProvider => {
            var ctrlOptsStore = serviceProvider.GetRequiredService<IControllerOptionsStore>();
            var tcs = new TaskCompletionSource<IBusAdapter>();

            // 启动一个后台任务来异步加载配置并创建适配器
            _ = Task.Run(async () => {
                try {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 超时保护
                    var ct = cts.Token;

                    // 1. 尝试从数据库读取
                    var dto = await ctrlOptsStore.GetAsync(ct);

                    // 2. 如果不存在，创建默认值并保存
                    if (dto is null) {
                        dto = new ControllerOptions {
                            Vendor = "leadshine",
                            ControllerIp = "192.168.5.11",
                            Template = new DriverOptionsTemplateOptions() {
                                Card = 8,
                                Port = 2,
                                GearRatio = 0.4m,
                                PulleyPitchDiameterMm = 79,
                            }
                        }; // 使用 record 默认值
                        await ctrlOptsStore.UpsertAsync(dto, ct);
                    }

                    // 3. 创建适配器
                    var adapter = new LeadshineLtdmcBusAdapter(
                        cardNo: (ushort)dto.Template.Card,
                        portNo: dto.Template.Port,
                        controllerIp: dto.ControllerIp
                    );

                    // 4. 完成 TaskCompletionSource
                    tcs.SetResult(adapter);
                }
                catch (Exception ex) {
                    tcs.SetException(ex);
                }
            });
            // 返回一个“未来会完成”的 IBusAdapter 代理
            return tcs.Task.GetAwaiter().GetResult();
        });
        services.AddSingleton<IAxisEventAggregator, AxisEventAggregator>();
        services.AddSingleton<IAxisController, AxisController>();
        // ---------- 上游数据连接Tcp相关注入 ----------
        services.AddUpstreamTcpFromLiteDb();
        // ---------- 解码器相关注入 ----------
        services.AddSingleton<IUpstreamCodec>(provider => {
            var store = provider.GetRequiredService<IUpstreamCodecOptionsStore>();

            // 获取配置（若不存在则返回默认）
            var options = store.GetAsync().GetAwaiter().GetResult();

            // 如果你想确保第一次启动时一定存入数据库
            store.UpsertAsync(options).GetAwaiter().GetResult();

            return new HuararyCodec(
                mainCount: options.MainCount,
                ejectCount: options.EjectCount
            );
        });

        // ---------- 事件泵 ----------
        services.AddHostedService<TransportEventPump>();
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
    host.Run();
}
catch (Exception e) {
    NLog.LogManager.GetCurrentClassLogger().Error(e, "运行异常");
}