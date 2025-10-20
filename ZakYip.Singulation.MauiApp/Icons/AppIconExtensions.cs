using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace ZakYip.Singulation.MauiApp.Icons;

/// <summary>枚举转 Glyph，结果缓存。</summary>
public static class AppIconExtensions
{
    private static readonly ConcurrentDictionary<AppIcon, string> Cache = new();

    /// <summary>将图标枚举转换为字体 Glyph 字符串。</summary>
    /// <param name="icon">图标枚举值</param>
    /// <returns>Unicode 字符串，用于字体显示</returns>
    public static string ToGlyph(this AppIcon icon)
        => Cache.GetOrAdd(icon, k =>
        {
            var mem = typeof(AppIcon).GetMember(k.ToString());
            return mem.Length > 0 ? mem[0].GetCustomAttribute<DescriptionAttribute>()?.Description ?? "" : "";
        });
}
