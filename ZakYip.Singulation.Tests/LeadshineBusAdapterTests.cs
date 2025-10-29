using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ZakYip.Singulation.Drivers.Leadshine;

namespace ZakYip.Singulation.Tests {

    internal sealed class LeadshineBusAdapterTests {

        [MiniFact]
        public async Task SafeGenericCapturesErrorAndReturnsFallbackAsync() {
            var adapter = new LeadshineLtdmcBusAdapter(0, 0, null);
            var errorTcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            adapter.ErrorOccurred += (_, message) => errorTcs.TrySetResult(message);

            var genericSafe = typeof(LeadshineLtdmcBusAdapter)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .First(m => m.Name == "Safe" && m.IsGenericMethodDefinition)
                .MakeGenericMethod(typeof(int));

            var throwing = new Func<Task<int>>(() => Task.FromException<int>(new InvalidOperationException("boom")));
            var task = (Task<int>)genericSafe.Invoke(adapter, new object[] { throwing, "test-operation", 42 })!;
            var result = await task.ConfigureAwait(false);
            var error = await errorTcs.Task.ConfigureAwait(false);

            MiniAssert.Equal(42, result, "Result should be 42");
            MiniAssert.True(error != null && error.Contains("test-operation", StringComparison.Ordinal), "Error should contain 'test-operation'");
            MiniAssert.True(adapter.LastErrorMessage?.Contains("boom", StringComparison.OrdinalIgnoreCase) == true, "LastErrorMessage should contain 'boom'");
        }

        [MiniFact]
        public async Task SafeActionClearsPreviousErrorOnSuccessAsync() {
            var adapter = new LeadshineLtdmcBusAdapter(0, 0, null);

            var genericSafe = typeof(LeadshineLtdmcBusAdapter)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .First(m => m.Name == "Safe" && m.IsGenericMethodDefinition)
                .MakeGenericMethod(typeof(int));

            var throwing = new Func<Task<int>>(() => Task.FromException<int>(new InvalidOperationException("boom")));
            var task = (Task<int>)genericSafe.Invoke(adapter, new object[] { throwing, "first", 7 })!;
            await task.ConfigureAwait(false);
            MiniAssert.NotNull(adapter.LastErrorMessage, "LastErrorMessage should not be null");

            var actionSafe = typeof(LeadshineLtdmcBusAdapter)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .First(m => m.Name == "Safe" && !m.IsGenericMethod);

            var successfulAction = new Func<Task>(() => Task.CompletedTask);
            var actionTask = (Task<bool>)actionSafe.Invoke(adapter, new object[] { successfulAction, "cleanup" })!;
            var success = await actionTask.ConfigureAwait(false);

            MiniAssert.True(success, "Success should be true");
            MiniAssert.Null(adapter.LastErrorMessage, "LastErrorMessage should be null after successful operation");
        }
    }
}
