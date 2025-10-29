using ZakYip.Singulation.Core.Configs;
using ZakYip.Singulation.Core.Contracts.ValueObjects;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 测试数据构建器，提供便捷的测试数据创建方法。
    /// </summary>
    internal static class TestDataBuilder {
        /// <summary>
        /// 创建默认的 ControllerOptions 实例用于测试。
        /// </summary>
        public static ControllerOptions CreateDefaultControllerOptions() {
            return new ControllerOptions {
                Vendor = "leadshine",
                ControllerIp = "192.168.5.11"
            };
        }

        /// <summary>
        /// 创建自定义的 ControllerOptions 实例用于测试。
        /// </summary>
        public static ControllerOptions CreateControllerOptions(string vendor, string controllerIp) {
            return new ControllerOptions {
                Vendor = vendor,
                ControllerIp = controllerIp
            };
        }

        /// <summary>
        /// 创建测试用的轴标识。
        /// </summary>
        public static AxisId CreateAxisId(int value = 1) {
            return new AxisId(value);
        }

        /// <summary>
        /// 创建测试用的轴标识列表。
        /// </summary>
        public static List<AxisId> CreateAxisIdList(int count) {
            var list = new List<AxisId>(count);
            for (int i = 0; i < count; i++) {
                list.Add(new AxisId(i + 1));
            }
            return list;
        }

        /// <summary>
        /// 创建测试用的 PprRatio。
        /// </summary>
        public static PprRatio CreatePprRatio(int numerator, int denominator) {
            return new PprRatio(numerator, denominator);
        }
    }
}
