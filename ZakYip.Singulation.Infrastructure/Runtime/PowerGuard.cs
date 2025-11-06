using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using ZakYip.Singulation.Core.Enums;

namespace ZakYip.Singulation.Infrastructure.Runtime {

    /// <summary>
    /// 电源防护工具类，防止系统休眠。
    /// </summary>
    public static class PowerGuard {

        [DllImport("kernel32.dll")]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
    }
}