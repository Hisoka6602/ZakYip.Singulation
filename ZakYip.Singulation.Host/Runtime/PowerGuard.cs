using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ZakYip.Singulation.Host.Runtime {

    internal static class PowerGuard {

        [Flags]
        public enum EXECUTION_STATE : uint {
            ES_CONTINUOUS = 0x80000000,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002
            // 还有 ES_AWAYMODE_REQUIRED 等可选
        }

        [DllImport("kernel32.dll")]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
    }
}