using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Abstractions.Events {

    /// <summary>
    /// 驱动库未加载事件参数。
    /// </summary>
    public sealed class DriverNotLoadedEventArgs : EventArgs {

        public DriverNotLoadedEventArgs(string libraryName, string message) {
            LibraryName = libraryName;
            Message = message;
        }

        /// <summary>未能加载的库名称（如 "LTDMC.dll"）。</summary>
        public string LibraryName { get; }

        /// <summary>错误原因说明。</summary>
        public string Message { get; }
    }
}