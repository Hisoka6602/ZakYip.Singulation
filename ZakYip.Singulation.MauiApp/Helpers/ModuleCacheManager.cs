using System.Collections.Concurrent;

namespace ZakYip.Singulation.MauiApp.Helpers;

/// <summary>
/// 模块数据缓存管理器 - 提供内存缓存提升性能
/// </summary>
public class ModuleCacheManager
{
    private static readonly Lazy<ModuleCacheManager> _instance = 
        new Lazy<ModuleCacheManager>(() => new ModuleCacheManager());
    
    public static ModuleCacheManager Instance => _instance.Value;

    private readonly ConcurrentDictionary<string, CachedItem<object>> _cache = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

    private ModuleCacheManager()
    {
        // 启动后台清理任务
        _ = Task.Run(async () => await CleanupExpiredItemsAsync());
    }

    /// <summary>
    /// 获取缓存项
    /// </summary>
    public T? Get<T>(string key) where T : class
    {
        if (_cache.TryGetValue(key, out var item))
        {
            if (!item.IsExpired)
            {
                return item.Value as T;
            }
            // 过期则移除
            _cache.TryRemove(key, out _);
        }
        return null;
    }

    /// <summary>
    /// 设置缓存项
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var expirationTime = expiration ?? _defaultExpiration;
        var item = new CachedItem<object>
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(expirationTime)
        };
        _cache[key] = item;
    }

    /// <summary>
    /// 移除缓存项
    /// </summary>
    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// 获取缓存大小
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// 后台清理过期项
    /// </summary>
    private async Task CleanupExpiredItemsAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[ModuleCacheManager] Cleaned up {expiredKeys.Count} expired items");
            }
        }
    }

    /// <summary>
    /// 缓存项
    /// </summary>
    private class CachedItem<T> where T : class
    {
        public T? Value { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
