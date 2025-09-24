using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Core.Contracts {

    /// <summary>
    /// 轴设置存储抽象接口：持久化“限幅、机构参数”等可配置项。
    /// </summary>
    public interface IAxisSettingsStore {

        /// <summary>读取所有轴的设置（不存在的轴不返回）。</summary>
        Task<IReadOnlyDictionary<string, AxisSettingsDto>> GetAllAsync(CancellationToken ct = default);

        /// <summary>读取单轴设置；不存在返回 null。</summary>
        Task<AxisSettingsDto> GetAsync(string axisId, CancellationToken ct = default);

        /// <summary>批量写入或更新设置；不存在则插入。</summary>
        Task UpsertAsync(IDictionary<string, AxisSettingsDto> items, CancellationToken ct = default);

        /// <summary>删除指定轴设置。</summary>
        Task DeleteAsync(string axisId, CancellationToken ct = default);
    }
}