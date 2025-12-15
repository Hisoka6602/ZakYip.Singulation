using ZakYip.Singulation.Tests.TestHelpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZakYip.Singulation.Drivers.Leadshine;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 雷赛批量 PDO 操作测试。
    /// 注：这些测试需要实际硬件才能完整执行，此处主要测试 API 设计和基本功能。
    /// </summary>
    internal sealed class LeadshineBatchPdoOperationsTests {

        [MiniFact]
        public void BatchWriteRequest_Constructor_SetsPropertiesCorrectly() {
            // 测试 int 类型
            var req1 = new LeadshineBatchPdoOperations.BatchWriteRequest(0x60FF, 1000);
            MiniAssert.Equal((ushort)0x60FF, req1.Index, "Index should be 0x60FF");
            MiniAssert.Equal((ushort)32, req1.BitLength, "BitLength should be 32 for int");
            MiniAssert.Equal(1000, req1.Value, "Value should be 1000");

            // 测试 ushort 类型
            var req2 = new LeadshineBatchPdoOperations.BatchWriteRequest(0x6040, (ushort)0x000F);
            MiniAssert.Equal((ushort)0x6040, req2.Index, "Index should be 0x6040");
            MiniAssert.Equal((ushort)16, req2.BitLength, "BitLength should be 16 for ushort");

            // 测试 byte 类型
            var req3 = new LeadshineBatchPdoOperations.BatchWriteRequest(0x6060, (byte)3);
            MiniAssert.Equal((ushort)0x6060, req3.Index, "Index should be 0x6060");
            MiniAssert.Equal((ushort)8, req3.BitLength, "BitLength should be 8 for byte");
        }

        [MiniFact]
        public void BatchReadRequest_Constructor_SetsPropertiesCorrectly() {
            var req = new LeadshineBatchPdoOperations.BatchReadRequest(0x606C, 32, 0);
            
            MiniAssert.Equal((ushort)0x606C, req.Index, "Index should be 0x606C");
            MiniAssert.Equal((ushort)32, req.BitLength, "BitLength should be 32");
            MiniAssert.Equal((byte)0, req.SubIndex, "SubIndex should be 0");
        }

        [MiniFact]
        public void BatchWriteResult_Constructor_SetsSuccessCorrectly() {
            var success = new LeadshineBatchPdoOperations.BatchWriteResult(0x60FF, 0);
            MiniAssert.True(success.IsSuccess, "Result with returnCode 0 should be successful");
            MiniAssert.Equal((short)0, success.ReturnCode, "ReturnCode should be 0");

            var failure = new LeadshineBatchPdoOperations.BatchWriteResult(0x60FF, -1);
            MiniAssert.True(!failure.IsSuccess, "Result with returnCode -1 should be unsuccessful");
            MiniAssert.Equal((short)-1, failure.ReturnCode, "ReturnCode should be -1");
        }

        [MiniFact]
        public void BatchReadResult_Constructor_SetsSuccessCorrectly() {
            var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var success = new LeadshineBatchPdoOperations.BatchReadResult(0x606C, 0, data);
            MiniAssert.True(success.IsSuccess, "Result with returnCode 0 should be successful");
            MiniAssert.True(success.Data != null, "Data should not be null");
            MiniAssert.Equal(4, success.Data!.Length, "Data length should be 4");

            var failure = new LeadshineBatchPdoOperations.BatchReadResult(0x606C, -1);
            MiniAssert.True(!failure.IsSuccess, "Result with returnCode -1 should be unsuccessful");
            MiniAssert.True(failure.Data == null, "Data should be null for failure");
        }

        [MiniFact]
        public void BatchStatistics_WriteResults_CalculatesCorrectly() {
            var results = new[] {
                new LeadshineBatchPdoOperations.BatchWriteResult(0x60FF, 0),
                new LeadshineBatchPdoOperations.BatchWriteResult(0x6040, 0),
                new LeadshineBatchPdoOperations.BatchWriteResult(0x6060, -1),
                new LeadshineBatchPdoOperations.BatchWriteResult(0x6083, 0),
                new LeadshineBatchPdoOperations.BatchWriteResult(0x6084, -2),
            };

            var stats = new LeadshineBatchPdoOperations.BatchStatistics(results);

            MiniAssert.Equal(5, stats.TotalRequests, "TotalRequests should be 5");
            MiniAssert.Equal(3, stats.SuccessCount, "SuccessCount should be 3");
            MiniAssert.Equal(2, stats.FailureCount, "FailureCount should be 2");
            MiniAssert.Equal(0.6, stats.SuccessRate, "SuccessRate should be 0.6");
        }

        [MiniFact]
        public void BatchStatistics_ReadResults_CalculatesCorrectly() {
            var results = new[] {
                new LeadshineBatchPdoOperations.BatchReadResult(0x606C, 0, new byte[4]),
                new LeadshineBatchPdoOperations.BatchReadResult(0x6041, 0, new byte[2]),
                new LeadshineBatchPdoOperations.BatchReadResult(0x6061, -1),
            };

            var stats = new LeadshineBatchPdoOperations.BatchStatistics(results);

            MiniAssert.Equal(3, stats.TotalRequests, "TotalRequests should be 3");
            MiniAssert.Equal(2, stats.SuccessCount, "SuccessCount should be 2");
            MiniAssert.Equal(1, stats.FailureCount, "FailureCount should be 1");
            MiniAssert.True(Math.Abs(stats.SuccessRate - 0.666666) < 0.001, "SuccessRate should be approximately 0.67");
        }

        [MiniFact]
        public async Task BatchWriteRxPdoAsync_EmptyRequests_ReturnsEmptyArray() {
            var results = await LeadshineBatchPdoOperations.BatchWriteRxPdoAsync(
                0, 0, 1, Array.Empty<LeadshineBatchPdoOperations.BatchWriteRequest>());

            MiniAssert.True(results != null, "Results should not be null");
            MiniAssert.Equal(0, results!.Length, "Results length should be 0");
        }

        [MiniFact]
        public async Task BatchReadTxPdoAsync_EmptyRequests_ReturnsEmptyArray() {
            var results = await LeadshineBatchPdoOperations.BatchReadTxPdoAsync(
                0, 0, 1, Array.Empty<LeadshineBatchPdoOperations.BatchReadRequest>());

            MiniAssert.True(results != null, "Results should not be null");
            MiniAssert.Equal(0, results!.Length, "Results length should be 0");
        }

        [MiniFact]
        public async Task BatchWriteRxPdoAsync_CancellationToken_CancelsOperation() {
            var cts = new CancellationTokenSource();
            cts.Cancel(); // 立即取消

            var requests = new[] {
                new LeadshineBatchPdoOperations.BatchWriteRequest(0x60FF, 1000),
                new LeadshineBatchPdoOperations.BatchWriteRequest(0x6040, (ushort)0x000F),
            };

            // 应该不会抛出异常，而是返回取消状态的结果
            var results = await LeadshineBatchPdoOperations.BatchWriteRxPdoAsync(
                0, 0, 1, requests, cts.Token);

            MiniAssert.True(results != null, "Results should not be null");
            // 由于立即取消，可能所有结果都是取消状态（返回码 -999）
            MiniAssert.True(results!.All(r => r.ReturnCode == -999 || r.ReturnCode != 0), "All results should be cancelled or failed");
        }

        [MiniFact]
        public async Task BatchReadTxPdoAsync_CancellationToken_CancelsOperation() {
            var cts = new CancellationTokenSource();
            cts.Cancel(); // 立即取消

            var requests = new[] {
                new LeadshineBatchPdoOperations.BatchReadRequest(0x606C, 32),
                new LeadshineBatchPdoOperations.BatchReadRequest(0x6041, 16),
            };

            // 应该不会抛出异常，而是返回取消状态的结果
            var results = await LeadshineBatchPdoOperations.BatchReadTxPdoAsync(
                0, 0, 1, requests, cts.Token);

            MiniAssert.True(results != null, "Results should not be null");
            // 由于立即取消，可能所有结果都是取消状态（返回码 -999）
            MiniAssert.True(results!.All(r => r.ReturnCode == -999 || r.ReturnCode != 0), "All results should be cancelled or failed");
        }

        [MiniFact]
        public async Task BatchAdapter_MultipleAxesWrite_ReturnsCorrectStructure() {
            var adapter = new LeadshineLtdmcBusAdapter(0, 0, null, new FakeSystemClock());

            var nodeIds = new ushort[] { 1, 2, 3 };
            var requests = new[] {
                new[] { new LeadshineBatchPdoOperations.BatchWriteRequest(0x60FF, 1000) },
                new[] { new LeadshineBatchPdoOperations.BatchWriteRequest(0x60FF, 2000) },
                new[] { new LeadshineBatchPdoOperations.BatchWriteRequest(0x60FF, 3000) },
            };

            var results = await adapter.BatchWriteMultipleAxesAsync(nodeIds, requests);

            MiniAssert.True(results != null, "Results should not be null");
            MiniAssert.Equal(3, results!.Count, "Results count should be 3");
            MiniAssert.True(results.ContainsKey(1), "Results should contain key 1");
            MiniAssert.True(results.ContainsKey(2), "Results should contain key 2");
            MiniAssert.True(results.ContainsKey(3), "Results should contain key 3");
        }

        [MiniFact]
        public async Task BatchAdapter_MultipleAxesRead_ReturnsCorrectStructure() {
            var adapter = new LeadshineLtdmcBusAdapter(0, 0, null, new FakeSystemClock());

            var nodeIds = new ushort[] { 1, 2 };
            var requests = new[] {
                new[] { new LeadshineBatchPdoOperations.BatchReadRequest(0x606C, 32) },
                new[] { new LeadshineBatchPdoOperations.BatchReadRequest(0x606C, 32) },
            };

            var results = await adapter.BatchReadMultipleAxesAsync(nodeIds, requests);

            MiniAssert.True(results != null, "Results should not be null");
            MiniAssert.Equal(2, results!.Count, "Results count should be 2");
            MiniAssert.True(results.ContainsKey(1), "Results should contain key 1");
            MiniAssert.True(results.ContainsKey(2), "Results should contain key 2");
        }
    }
}
