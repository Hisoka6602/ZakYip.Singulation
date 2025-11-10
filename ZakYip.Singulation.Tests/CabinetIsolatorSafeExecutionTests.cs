using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ZakYip.Singulation.Core.Abstractions.Realtime;
using ZakYip.Singulation.Infrastructure.Cabinet;

namespace ZakYip.Singulation.Tests;

/// <summary>
/// 测试 CabinetIsolator 的安全执行功能（统一的安全隔离器）
/// </summary>
public class CabinetIsolatorSafeExecutionTests
{
    private class FakeRealtimeNotifier : IRealtimeNotifier
    {
        public ValueTask PublishAsync(string channel, object payload, CancellationToken ct = default) => ValueTask.CompletedTask;
        public Task PublishDeviceAsync(object payload) => Task.CompletedTask;
        public Task PublishScopedAsync(string connectionId, object payload) => Task.CompletedTask;
        public Task PublishClientAsync(string scope, object payload) => Task.CompletedTask;
    }

    [MiniFact]
    public void SafeExecute_WithSuccessfulAction_ShouldReturnTrue()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());
        bool actionExecuted = false;

        // Act
        bool result = isolator.SafeExecute(() => {
            actionExecuted = true;
        }, "测试操作");

        // Assert
        MiniAssert.True(result, "操作应该成功");
        MiniAssert.True(actionExecuted, "操作应该被执行");
    }

    [MiniFact]
    public void SafeExecute_WithThrowingAction_ShouldReturnFalse()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        bool result = isolator.SafeExecute(() => {
            throw new InvalidOperationException("测试异常");
        }, "测试操作");

        // Assert
        MiniAssert.False(result, "操作应该失败");
    }

    [MiniFact]
    public void SafeExecute_WithThrowingAction_ShouldCallErrorHandler()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());
        Exception? caughtException = null;

        // Act
        bool result = isolator.SafeExecute(
            () => throw new InvalidOperationException("测试异常"),
            "测试操作",
            ex => caughtException = ex
        );

        // Assert
        MiniAssert.False(result, "操作应该失败");
        MiniAssert.NotNull(caughtException, "错误处理器应该被调用");
        MiniAssert.True(caughtException is InvalidOperationException, "应该捕获正确的异常类型");
    }

    [MiniFact]
    public void SafeExecuteWithReturn_WithSuccessfulFunc_ShouldReturnResult()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        int result = isolator.SafeExecute(() => 42, "测试操作", 0);

        // Assert
        MiniAssert.Equal(42, result, "应该返回正确的结果");
    }

    [MiniFact]
    public void SafeExecuteWithReturn_WithThrowingFunc_ShouldReturnDefaultValue()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        int result = isolator.SafeExecute<int>(
            () => throw new InvalidOperationException("测试异常"),
            "测试操作",
            99
        );

        // Assert
        MiniAssert.Equal(99, result, "应该返回默认值");
    }

    [MiniFact]
    public void SafeExecuteNullable_WithSuccessfulFunc_ShouldReturnResult()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        string? result = isolator.SafeExecuteNullable(() => "success", "测试操作");

        // Assert
        MiniAssert.NotNull(result, "应该返回结果");
        MiniAssert.Equal("success", result, "应该返回正确的值");
    }

    [MiniFact]
    public void SafeExecuteNullable_WithThrowingFunc_ShouldReturnNull()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        string? result = isolator.SafeExecuteNullable<string>(
            () => throw new InvalidOperationException("测试异常"),
            "测试操作"
        );

        // Assert
        MiniAssert.Null(result, "应该返回 null");
    }

    [MiniFact]
    public void SafeExecuteBatch_WithAllSuccessfulActions_ShouldReturnCorrectCount()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());
        int counter = 0;
        var actions = new Action[] {
            () => counter++,
            () => counter++,
            () => counter++
        };

        // Act
        int successCount = isolator.SafeExecuteBatch(actions, "批量测试");

        // Assert
        MiniAssert.Equal(3, successCount, "应该成功执行所有操作");
        MiniAssert.Equal(3, counter, "所有操作应该被执行");
    }

    [MiniFact]
    public void SafeExecuteBatch_WithSomeFailingActions_ShouldContinueByDefault()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());
        int counter = 0;
        var actions = new Action[] {
            () => counter++,
            () => throw new InvalidOperationException("第二个操作失败"),
            () => counter++
        };

        // Act
        int successCount = isolator.SafeExecuteBatch(actions, "批量测试");

        // Assert
        MiniAssert.Equal(2, successCount, "应该有两个操作成功");
        MiniAssert.Equal(2, counter, "成功的操作应该被执行");
    }

    [MiniFact]
    public void SafeExecuteBatch_WithStopOnFirstError_ShouldStopOnError()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());
        int counter = 0;
        var actions = new Action[] {
            () => counter++,
            () => throw new InvalidOperationException("第二个操作失败"),
            () => counter++
        };

        // Act
        int successCount = isolator.SafeExecuteBatch(actions, "批量测试", stopOnFirstError: true);

        // Assert
        MiniAssert.Equal(1, successCount, "应该只有一个操作成功");
        MiniAssert.Equal(1, counter, "第三个操作不应该被执行");
    }

    [MiniFact]
    public async Task SafeExecuteAsync_WithSuccessfulAction_ShouldReturnTrue()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());
        bool actionExecuted = false;

        // Act
        bool result = await isolator.SafeExecuteAsync(async () => {
            await Task.Delay(1);
            actionExecuted = true;
        }, "测试异步操作");

        // Assert
        MiniAssert.True(result, "异步操作应该成功");
        MiniAssert.True(actionExecuted, "异步操作应该被执行");
    }

    [MiniFact]
    public async Task SafeExecuteAsync_WithThrowingAction_ShouldReturnFalse()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        bool result = await isolator.SafeExecuteAsync(async () => {
            await Task.Delay(1);
            throw new InvalidOperationException("测试异常");
        }, "测试异步操作");

        // Assert
        MiniAssert.False(result, "异步操作应该失败");
    }

    [MiniFact]
    public async Task SafeExecuteAsyncWithReturn_WithSuccessfulFunc_ShouldReturnResult()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        int result = await isolator.SafeExecuteAsync(async () => {
            await Task.Delay(1);
            return 42;
        }, "测试异步操作", 0);

        // Assert
        MiniAssert.Equal(42, result, "应该返回正确的结果");
    }

    [MiniFact]
    public async Task SafeExecuteAsyncWithReturn_WithThrowingFunc_ShouldReturnDefaultValue()
    {
        // Arrange
        var isolator = new CabinetIsolator(
            NullLogger<CabinetIsolator>.Instance,
            new FakeRealtimeNotifier());

        // Act
        int result = await isolator.SafeExecuteAsync<int>(async () => {
            await Task.Delay(1);
            throw new InvalidOperationException("测试异常");
        }, "测试异步操作", 99);

        // Assert
        MiniAssert.Equal(99, result, "应该返回默认值");
    }
}
