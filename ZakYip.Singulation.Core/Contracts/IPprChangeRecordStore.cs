using ZakYip.Singulation.Core.Configs;

namespace ZakYip.Singulation.Core.Contracts {
    /// <summary>
    /// PPR 变化记录存储接口
    /// </summary>
    public interface IPprChangeRecordStore {
        /// <summary>
        /// 保存 PPR 变化记录
        /// </summary>
        Task SaveAsync(PprChangeRecord record, CancellationToken ct = default);

        /// <summary>
        /// 获取指定轴的 PPR 变化历史
        /// </summary>
        Task<List<PprChangeRecord>> GetByAxisIdAsync(string axisId, CancellationToken ct = default);

        /// <summary>
        /// 获取所有 PPR 变化记录
        /// </summary>
        Task<List<PprChangeRecord>> GetAllAsync(int skip = 0, int take = 100, CancellationToken ct = default);

        /// <summary>
        /// 获取异常变化记录
        /// </summary>
        Task<List<PprChangeRecord>> GetAnomalousAsync(CancellationToken ct = default);

        /// <summary>
        /// 删除指定时间之前的记录
        /// </summary>
        Task DeleteOlderThanAsync(DateTime before, CancellationToken ct = default);
    }
}
