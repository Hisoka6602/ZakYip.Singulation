namespace ZakYip.Singulation.Core.Enums {

    /// <summary>
    /// IO 触发电平配置。
    /// </summary>
    public enum TriggerLevel {
        /// <summary>高电平有效/触发（常开按键）。</summary>
        ActiveHigh = 0,

        /// <summary>低电平有效/触发（常闭按键）。</summary>
        ActiveLow = 1
    }
}
