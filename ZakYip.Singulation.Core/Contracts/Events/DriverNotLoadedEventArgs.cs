namespace ZakYip.Singulation.Core.Contracts.Events {

    /// <summary>
    /// 驱动库未加载事件参数。
    /// </summary>
    public sealed record class DriverNotLoadedEventArgs {
        /// <summary>未能加载的库名称（如 "LTDMC.dll"）。</summary>
        public required string LibraryName { get; init; }

        /// <summary>错误原因说明。</summary>
        public required string Message { get; init; }
    }
}