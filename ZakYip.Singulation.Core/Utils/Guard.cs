using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ZakYip.Singulation.Core.Utils;

/// <summary>
/// 参数验证辅助类，提供统一的参数检查和业务规则验证。
/// </summary>
/// <remarks>
/// 此类提供了一组静态方法用于验证方法参数、业务规则等，
/// 统一异常处理策略，提高代码可读性和一致性。
/// </remarks>
public static class Guard {
    
    /// <summary>
    /// 验证参数不为 null。
    /// </summary>
    /// <typeparam name="T">参数类型。</typeparam>
    /// <param name="value">要验证的参数值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentNullException">当参数为 null 时抛出。</exception>
    /// <returns>返回验证后的非空值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NotNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : class {
        if (value is null) {
            throw new ArgumentNullException(parameterName);
        }
        return value;
    }
    
    /// <summary>
    /// 验证字符串参数不为 null 或空字符串。
    /// </summary>
    /// <param name="value">要验证的字符串值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentException">当字符串为 null 或空时抛出。</exception>
    /// <returns>返回验证后的非空字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NotNullOrEmpty(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null) {
        if (string.IsNullOrEmpty(value)) {
            throw new ArgumentException("字符串参数不能为 null 或空。", parameterName);
        }
        return value;
    }
    
    /// <summary>
    /// 验证字符串参数不为 null、空字符串或仅包含空白字符。
    /// </summary>
    /// <param name="value">要验证的字符串值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentException">当字符串为 null、空或仅包含空白字符时抛出。</exception>
    /// <returns>返回验证后的非空字符串。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NotNullOrWhiteSpace(
        [NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("字符串参数不能为 null、空或仅包含空白字符。", parameterName);
        }
        return value;
    }
    
    /// <summary>
    /// 验证数值参数在指定范围内（包含边界）。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要验证的数值。</param>
    /// <param name="min">最小值（包含）。</param>
    /// <param name="max">最大值（包含）。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当数值超出指定范围时抛出。</exception>
    /// <returns>返回验证后的数值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T InRange<T>(
        T value,
        T min,
        T max,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T> {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"参数值必须在 [{min}, {max}] 范围内。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证数值参数大于等于指定的最小值。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要验证的数值。</param>
    /// <param name="min">最小值（包含）。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当数值小于最小值时抛出。</exception>
    /// <returns>返回验证后的数值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GreaterThanOrEqual<T>(
        T value,
        T min,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T> {
        if (value.CompareTo(min) < 0) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"参数值必须大于等于 {min}。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证数值参数大于指定的最小值。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要验证的数值。</param>
    /// <param name="min">最小值（不包含）。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当数值小于或等于最小值时抛出。</exception>
    /// <returns>返回验证后的数值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GreaterThan<T>(
        T value,
        T min,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T> {
        if (value.CompareTo(min) <= 0) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"参数值必须大于 {min}。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证数值参数小于等于指定的最大值。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要验证的数值。</param>
    /// <param name="max">最大值（包含）。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当数值大于最大值时抛出。</exception>
    /// <returns>返回验证后的数值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T LessThanOrEqual<T>(
        T value,
        T max,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T> {
        if (value.CompareTo(max) > 0) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"参数值必须小于等于 {max}。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证数值参数小于指定的最大值。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要验证的数值。</param>
    /// <param name="max">最大值（不包含）。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当数值大于或等于最大值时抛出。</exception>
    /// <returns>返回验证后的数值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T LessThan<T>(
        T value,
        T max,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T> {
        if (value.CompareTo(max) >= 0) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"参数值必须小于 {max}。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证数值参数为正数（大于 0）。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要验证的数值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当数值小于或等于 0 时抛出。</exception>
    /// <returns>返回验证后的正数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Positive<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T>, IConvertible {
        var zero = (T)Convert.ChangeType(0, typeof(T));
        if (value.CompareTo(zero) <= 0) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "参数值必须为正数（大于 0）。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证数值参数为非负数（大于或等于 0）。
    /// </summary>
    /// <typeparam name="T">数值类型。</typeparam>
    /// <param name="value">要验证的数值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当数值小于 0 时抛出。</exception>
    /// <returns>返回验证后的非负数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NonNegative<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where T : IComparable<T>, IConvertible {
        var zero = (T)Convert.ChangeType(0, typeof(T));
        if (value.CompareTo(zero) < 0) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "参数值必须为非负数（大于或等于 0）。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证条件为真，否则抛出异常。
    /// </summary>
    /// <param name="condition">要验证的条件。</param>
    /// <param name="message">条件为假时的错误消息。</param>
    /// <param name="parameterName">参数名称（可选）。</param>
    /// <exception cref="ArgumentException">当条件为假时抛出。</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void That(
        [DoesNotReturnIf(false)] bool condition,
        string message,
        string? parameterName = null) {
        if (!condition) {
            throw new ArgumentException(message, parameterName);
        }
    }
    
    /// <summary>
    /// 验证枚举值是否已定义。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <param name="value">要验证的枚举值。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentOutOfRangeException">当枚举值未定义时抛出。</exception>
    /// <returns>返回验证后的枚举值。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum EnumDefined<TEnum>(
        TEnum value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        where TEnum : struct, Enum {
        if (!Enum.IsDefined(typeof(TEnum), value)) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"枚举值 {value} 未在 {typeof(TEnum).Name} 中定义。");
        }
        return value;
    }
    
    /// <summary>
    /// 验证集合不为 null 且包含至少一个元素。
    /// </summary>
    /// <typeparam name="T">集合元素类型。</typeparam>
    /// <param name="collection">要验证的集合。</param>
    /// <param name="parameterName">参数名称。</param>
    /// <exception cref="ArgumentException">当集合为 null 或为空时抛出。</exception>
    /// <returns>返回验证后的非空集合。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NotNullOrEmpty<T>(
        [NotNull] T? collection,
        [CallerArgumentExpression(nameof(collection))] string? parameterName = null)
        where T : System.Collections.IEnumerable {
        if (collection is null) {
            throw new ArgumentNullException(parameterName);
        }
        if (!collection.GetEnumerator().MoveNext()) {
            throw new ArgumentException("集合不能为空。", parameterName);
        }
        return collection;
    }
}
