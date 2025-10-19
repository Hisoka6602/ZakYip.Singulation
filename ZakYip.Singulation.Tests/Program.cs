using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ZakYip.Singulation.Tests {

    internal static class Program {
        private static async Task<int> Main() {
            var assembly = Assembly.GetExecutingAssembly();
            var methods = assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                .Where(m => m.GetCustomAttribute<MiniFactAttribute>() is not null)
                .ToList();

            var results = new List<MiniTestResult>();
            foreach (var method in methods) {
                object? instance = null;
                try {
                    if (!method.IsStatic) {
                        instance = Activator.CreateInstance(method.DeclaringType!);
                    }

                    var returned = method.Invoke(instance, Array.Empty<object?>());
                    if (returned is Task task) {
                        await task.ConfigureAwait(false);
                    }

                    results.Add(new MiniTestResult(method, true, null));
                }
                catch (TargetInvocationException ex) {
                    results.Add(new MiniTestResult(method, false, ex.InnerException ?? ex));
                }
                catch (Exception ex) {
                    results.Add(new MiniTestResult(method, false, ex));
                }
                finally {
                    if (instance is IAsyncDisposable asyncDisposable) {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else if (instance is IDisposable disposable) {
                        disposable.Dispose();
                    }
                }
            }

            var passed = results.Count(r => r.Passed);
            var failed = results.Count - passed;

            Console.WriteLine($"共执行 {results.Count} 个测试，用时 {results.Count} 次调用。");
            foreach (var result in results.Where(r => !r.Passed)) {
                Console.WriteLine($"[失败] {result.Method.DeclaringType?.Name}.{result.Method.Name}: {result.Error}");
            }

            Console.WriteLine(failed == 0
                ? "所有测试均通过。"
                : $"共有 {failed} 个测试失败。");

            return failed == 0 ? 0 : 1;
        }
    }
}
