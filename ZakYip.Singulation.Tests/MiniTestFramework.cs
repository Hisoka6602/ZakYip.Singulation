using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Tests {

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MiniFactAttribute : Attribute { }

    internal static class MiniAssert {
        public static void True(bool condition, string message) {
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void False(bool condition, string message) {
            if (condition) throw new InvalidOperationException(message);
        }

        public static void NotNull<T>(T? value, string message) where T : class {
            if (value is null) throw new InvalidOperationException(message);
        }

        public static void Null<T>(T? value, string message) where T : class {
            if (value is not null) throw new InvalidOperationException(message);
        }

        public static void Equal<T>(T expected, T actual, string message) {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
                throw new InvalidOperationException($"{message} —— 期望: {expected}, 实际: {actual}");
            }
        }

        public static void NotEqual<T>(T notExpected, T actual, string message) {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual)) {
                throw new InvalidOperationException($"{message} —— 不应等于: {notExpected}");
            }
        }

        public static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string message) {
            if (expected.Count != actual.Count) {
                throw new InvalidOperationException($"{message} —— 长度不一致: {expected.Count} vs {actual.Count}");
            }
            for (var i = 0; i < expected.Count; i++) {
                if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i])) {
                    throw new InvalidOperationException($"{message} —— 第 {i} 项不匹配: {expected[i]} vs {actual[i]}");
                }
            }
        }

        public static void Contains<T>(IEnumerable<T> collection, T item, string message) {
            if (!collection.Contains(item)) {
                throw new InvalidOperationException($"{message} —— 集合不包含项: {item}");
            }
        }

        public static void NotContains<T>(IEnumerable<T> collection, T item, string message) {
            if (collection.Contains(item)) {
                throw new InvalidOperationException($"{message} —— 集合不应包含项: {item}");
            }
        }

        public static void Empty<T>(IEnumerable<T> collection, string message) {
            if (collection.Any()) {
                throw new InvalidOperationException($"{message} —— 集合应为空");
            }
        }

        public static void NotEmpty<T>(IEnumerable<T> collection, string message) {
            if (!collection.Any()) {
                throw new InvalidOperationException($"{message} —— 集合不应为空");
            }
        }

        public static void Throws<TException>(Action action, string message) where TException : Exception {
            bool thrown = false;
            try {
                action();
            }
            catch (TException) {
            bool thrown = false;
            try {
                action();
            }
            catch (TException) {
                // Expected exception
                thrown = true;
            }
            catch (Exception ex) {
                throw new InvalidOperationException($"{message} —— 期望异常类型 {typeof(TException).Name}，实际: {ex.GetType().Name}");
            }
            if (!thrown) {
                throw new InvalidOperationException($"{message} —— 应抛出异常 {typeof(TException).Name}");
            }
        }

        public static async Task ThrowsAsync<TException>(Func<Task> action, string message) where TException : Exception {
            bool thrown = false;
            try {
                await action().ConfigureAwait(false);
            }
            catch (TException) {
                // Expected exception
                thrown = true;
            }
            catch (Exception ex) {
                throw new InvalidOperationException($"{message} —— 期望异常类型 {typeof(TException).Name}，实际: {ex.GetType().Name}");
            }
            if (!thrown) {
                throw new InvalidOperationException($"{message} —— 应抛出异常 {typeof(TException).Name}");
            }
            if (!thrown) {
                throw new InvalidOperationException($"{message} —— 应抛出异常 {typeof(TException).Name}");
            }
        }

        public static void InRange<T>(T value, T min, T max, string message) where T : IComparable<T> {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0) {
                throw new InvalidOperationException($"{message} —— 值 {value} 不在范围 [{min}, {max}] 内");
            }
        }
    }

    internal sealed record MiniTestResult(MethodInfo Method, bool Passed, Exception? Error);
}
