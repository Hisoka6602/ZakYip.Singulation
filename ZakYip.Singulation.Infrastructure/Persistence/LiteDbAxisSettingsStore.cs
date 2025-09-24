using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Infrastructure.Persistence {

    /// <summary>
    /// 基于 LiteDB 的轴设置存储（简化版）：直接使用 AxisSettingsDto 作为实体。
    /// 通过 BsonMapper 在内部把 AxisId 映射为主键，而不污染 Core DTO。
    /// </summary>
    public sealed class LiteDbAxisSettingsStore : IAxisSettingsStore {
        private readonly ILiteDatabase _db;
        private readonly ILiteCollection<AxisSettingsDto> _coll;

        /// <summary>
        /// 构造并配置实体映射。
        /// </summary>
        /// <param name="db">已创建的 LiteDB 实例。</param>
        public LiteDbAxisSettingsStore(ILiteDatabase db) {
            _db = db;

            var mapper = BsonMapper.Global;
            mapper.Entity<AxisSettingsDto>()
                .Id(x => x.AxisId, autoId: false);

            _coll = _db.GetCollection<AxisSettingsDto>("axis_settings");
            _coll.EnsureIndex(x => x.AxisId, unique: true);
        }

        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, AxisSettingsDto>> GetAllAsync(CancellationToken ct = default) {
            var map = _coll.FindAll().ToDictionary(d => d.AxisId, d => d);
            return Task.FromResult((IReadOnlyDictionary<string, AxisSettingsDto>)map);
        }

        /// <inheritdoc />
        public Task<AxisSettingsDto> GetAsync(string axisId, CancellationToken ct = default) {
            var dto = _coll.FindById(axisId);
            return Task.FromResult(dto);
        }

        /// <inheritdoc />
        public Task UpsertAsync(IDictionary<string, AxisSettingsDto> items, CancellationToken ct = default) {
            if (items.Count == 0) return Task.CompletedTask;

            // 统一修正实体的 AxisId 与字典键一致（避免调用方忘记填）
            var docs = new List<AxisSettingsDto>(items.Count);
            foreach (var (axisId, dto) in items) {
                if (dto.AxisId != axisId) {
                    docs.Add(new AxisSettingsDto {
                        AxisId = axisId,
                        Card = dto.Card,
                        Port = dto.Port,
                        NodeId = dto.NodeId,
                        IpAddress = dto.IpAddress,
                        GearRatio = dto.GearRatio,
                        ScrewPitchMm = dto.ScrewPitchMm,
                        PulleyDiameterMm = dto.PulleyDiameterMm,
                        PulleyPitchDiameterMm = dto.PulleyPitchDiameterMm,
                        MaxRpm = dto.MaxRpm,
                        MaxAccelRpmPerSec = dto.MaxAccelRpmPerSec,
                        MaxDecelRpmPerSec = dto.MaxDecelRpmPerSec
                    });
                }
                else {
                    docs.Add(dto);
                }
            }

            _coll.Upsert(docs);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteAsync(string axisId, CancellationToken ct = default) {
            _coll.Delete(axisId);
            return Task.CompletedTask;
        }
    }
}