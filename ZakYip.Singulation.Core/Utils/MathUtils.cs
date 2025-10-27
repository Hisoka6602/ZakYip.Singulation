using System;
using System.Runtime.CompilerServices;

namespace ZakYip.Singulation.Core.Utils;

/// <summary>
/// 数学计算工具类，提供高性能的常用数学运算。
/// </summary>
/// <remarks>
/// 此类提供了一组优化的静态方法用于数学计算，包括：
/// - 限幅（Clamp）
/// - 插值
/// - 数值转换和舍入
/// - 范围检查
/// </remarks>
public static class MathUtils {

    /// <summary>
    /// 将值限制在指定范围内（包含边界）。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要限制的值。</param>
    /// <param name="min">最小值（包含）。</param>
    /// <param name="max">最大值（包含）。</param>
    /// <returns>限制后的值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Clamp<T>(T value, T min, T max) where T : IComparable<T> {
        if (value.CompareTo(min) < 0) return min;
        if (value.CompareTo(max) > 0) return max;
        return value;
    }

    /// <summary>
    /// 将 decimal 值限制为 UInt32 范围。
    /// </summary>
    /// <param name="value">要限制的 decimal 值。</param>
    /// <returns>限制后的 UInt32 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint ClampToUInt32(decimal value) {
        if (value <= 0m) return 0u;
        if (value >= uint.MaxValue) return uint.MaxValue;
        return (uint)Math.Round(value);
    }

    /// <summary>
    /// 将 decimal 值限制为 Int32 范围。
    /// </summary>
    /// <param name="value">要限制的 decimal 值。</param>
    /// <returns>限制后的 Int32 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ClampToInt32(decimal value) {
        if (value <= int.MinValue) return int.MinValue;
        if (value >= int.MaxValue) return int.MaxValue;
        return (int)Math.Round(value);
    }

    /// <summary>
    /// 将 decimal 值限制为 UInt16 范围。
    /// </summary>
    /// <param name="value">要限制的 decimal 值。</param>
    /// <returns>限制后的 UInt16 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ushort ClampToUInt16(decimal value) {
        if (value <= 0m) return 0;
        if (value >= ushort.MaxValue) return ushort.MaxValue;
        return (ushort)Math.Round(value);
    }

    /// <summary>
    /// 将 decimal 值限制为 Int16 范围。
    /// </summary>
    /// <param name="value">要限制的 decimal 值。</param>
    /// <returns>限制后的 Int16 值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static short ClampToInt16(decimal value) {
        if (value <= short.MinValue) return short.MinValue;
        if (value >= short.MaxValue) return short.MaxValue;
        return (short)Math.Round(value);
    }

    /// <summary>
    /// 线性插值。
    /// </summary>
    /// <param name="a">起始值。</param>
    /// <param name="b">结束值。</param>
    /// <param name="t">插值因子（0.0 到 1.0）。</param>
    /// <returns>插值结果。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static decimal Lerp(decimal a, decimal b, decimal t) {
        return a + (b - a) * Clamp(t, 0m, 1m);
    }

    /// <summary>
    /// 线性插值（double 版本）。
    /// </summary>
    /// <param name="a">起始值。</param>
    /// <param name="b">结束值。</param>
    /// <param name="t">插值因子（0.0 到 1.0）。</param>
    /// <returns>插值结果。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static double Lerp(double a, double b, double t) {
        return a + (b - a) * Clamp(t, 0.0, 1.0);
    }

    /// <summary>
    /// 反向线性插值，计算插值因子。
    /// </summary>
    /// <param name="a">起始值。</param>
    /// <param name="b">结束值。</param>
    /// <param name="value">当前值。</param>
    /// <returns>插值因子（0.0 到 1.0）。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static decimal InverseLerp(decimal a, decimal b, decimal value) {
        if (Math.Abs(b - a) < 0.00001m) return 0m;
        return Clamp((value - a) / (b - a), 0m, 1m);
    }

    /// <summary>
    /// 检查值是否在指定范围内（包含边界）。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要检查的值。</param>
    /// <param name="min">最小值（包含）。</param>
    /// <param name="max">最大值（包含）。</param>
    /// <returns>在范围内返回 true，否则返回 false。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsInRange<T>(T value, T min, T max) where T : IComparable<T> {
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    /// <summary>
    /// 将值映射到新的范围。
    /// </summary>
    /// <param name="value">要映射的值。</param>
    /// <param name="fromMin">原始范围最小值。</param>
    /// <param name="fromMax">原始范围最大值。</param>
    /// <param name="toMin">目标范围最小值。</param>
    /// <param name="toMax">目标范围最大值。</param>
    /// <returns>映射后的值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static decimal Map(decimal value, decimal fromMin, decimal fromMax, decimal toMin, decimal toMax) {
        var t = InverseLerp(fromMin, fromMax, value);
        return Lerp(toMin, toMax, t);
    }

    /// <summary>
    /// 计算两个值的最小值。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Min<T>(T a, T b) where T : IComparable<T> {
        return a.CompareTo(b) <= 0 ? a : b;
    }

    /// <summary>
    /// 计算两个值的最大值。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Max<T>(T a, T b) where T : IComparable<T> {
        return a.CompareTo(b) >= 0 ? a : b;
    }

    /// <summary>
    /// 计算三个值的最小值。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Min<T>(T a, T b, T c) where T : IComparable<T> {
        return Min(Min(a, b), c);
    }

    /// <summary>
    /// 计算三个值的最大值。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static T Max<T>(T a, T b, T c) where T : IComparable<T> {
        return Max(Max(a, b), c);
    }


    /// <summary>
    /// 安全除法，避免除以零。
    /// </summary>
    /// <param name="numerator">分子。</param>
    /// <param name="denominator">分母。</param>
    /// <param name="defaultValue">分母为零时的默认返回值。</param>
    /// <returns>除法结果或默认值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static decimal SafeDivide(decimal numerator, decimal denominator, decimal defaultValue = 0m) {
        return denominator != 0m ? numerator / denominator : defaultValue;
    }

    /// <summary>
    /// 指数增长的延迟计算（用于重试退避）。
    /// </summary>
    /// <param name="baseDelayMs">基础延迟（毫秒）。</param>
    /// <param name="attemptNumber">尝试次数（从 0 开始）。</param>
    /// <param name="maxDelayMs">最大延迟（毫秒）。</param>
    /// <returns>计算后的延迟（毫秒）。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int ExponentialBackoff(int baseDelayMs, int attemptNumber, int maxDelayMs) {
        var exponent = Math.Min(attemptNumber, 20); // 防止溢出
        var delay = baseDelayMs * Math.Pow(2, exponent);
        return (int)Math.Min(delay, maxDelayMs);
    }



}
