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

        public static void Equal<T>(T expected, T actual, string message) {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
                throw new InvalidOperationException($"{message} —— 期望: {expected}, 实际: {actual}");
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
    }

    internal sealed record MiniTestResult(MethodInfo Method, bool Passed, Exception? Error);
}
