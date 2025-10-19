using System.Text.Json;
using ZakYip.Singulation.MauiApp.Services;

namespace ZakYip.Singulation.MauiApp.Helpers;

/// <summary>
/// 服务缓存助手，用于缓存最近连接的服务信息
/// </summary>
public static class ServiceCacheHelper
{
    private const string CacheKey = "CachedServices";
    private const int MaxCachedServices = 5;
    
    /// <summary>
    /// 缓存服务信息
    /// </summary>
    public static void CacheService(DiscoveredService service)
    {
        try
        {
            var cached = GetCachedServices();
            
            // 移除已存在的相同服务
            cached.RemoveAll(s => s.IpAddress == service.IpAddress && s.HttpPort == service.HttpPort);
            
            // 添加到列表开头（最近使用的）
            cached.Insert(0, new CachedServiceInfo
            {
                ServiceName = service.ServiceName,
                IpAddress = service.IpAddress,
                HttpPort = service.HttpPort,
                HttpsPort = service.HttpsPort,
                SignalRPath = service.SignalRPath,
                LastConnected = DateTime.Now
            });
            
            // 只保留最近的N个
            if (cached.Count > MaxCachedServices)
                cached = cached.Take(MaxCachedServices).ToList();
            
            // 序列化并保存
            var json = JsonSerializer.Serialize(cached);
            Preferences.Set(CacheKey, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServiceCache] Failed to cache service: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 缓存当前API配置
    /// </summary>
    public static void CacheCurrentApiUrl(string apiBaseUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                return;
            
            // 解析URL
            if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri))
                return;
            
            var cached = GetCachedServices();
            
            // 移除已存在的相同服务
            cached.RemoveAll(s => s.IpAddress == uri.Host && s.HttpPort == uri.Port);
            
            // 添加到列表开头
            cached.Insert(0, new CachedServiceInfo
            {
                ServiceName = "手动配置",
                IpAddress = uri.Host,
                HttpPort = uri.Port,
                HttpsPort = uri.Scheme == "https" ? uri.Port : 0,
                SignalRPath = "/hubs/events",
                LastConnected = DateTime.Now
            });
            
            // 只保留最近的N个
            if (cached.Count > MaxCachedServices)
                cached = cached.Take(MaxCachedServices).ToList();
            
            // 序列化并保存
            var json = JsonSerializer.Serialize(cached);
            Preferences.Set(CacheKey, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServiceCache] Failed to cache API URL: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 获取缓存的服务列表
    /// </summary>
    public static List<CachedServiceInfo> GetCachedServices()
    {
        try
        {
            var json = Preferences.Get(CacheKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return new List<CachedServiceInfo>();
            
            var cached = JsonSerializer.Deserialize<List<CachedServiceInfo>>(json);
            return cached ?? new List<CachedServiceInfo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ServiceCache] Failed to load cached services: {ex.Message}");
            return new List<CachedServiceInfo>();
        }
    }
    
    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public static void ClearCache()
    {
        Preferences.Remove(CacheKey);
    }
    
    /// <summary>
    /// 获取最近使用的服务（如果有）
    /// </summary>
    public static CachedServiceInfo? GetMostRecentService()
    {
        var cached = GetCachedServices();
        return cached.FirstOrDefault();
    }
}

/// <summary>
/// 缓存的服务信息
/// </summary>
public class CachedServiceInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int HttpPort { get; set; }
    public int HttpsPort { get; set; }
    public string SignalRPath { get; set; } = string.Empty;
    public DateTime LastConnected { get; set; }
    
    public string HttpBaseUrl => $"http://{IpAddress}:{HttpPort}";
    public string HttpsBaseUrl => HttpsPort > 0 ? $"https://{IpAddress}:{HttpsPort}" : HttpBaseUrl;
    public string DisplayName => $"{ServiceName} ({IpAddress})";
    public string DisplayInfo => $"上次连接: {LastConnected:yyyy-MM-dd HH:mm}";
}
