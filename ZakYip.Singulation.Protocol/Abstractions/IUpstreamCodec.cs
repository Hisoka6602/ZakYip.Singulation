using System;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Enums;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Protocol.Abstractions {

    /// <summary>
    /// 上游协议编解码器：将一帧报文解码为统一语义（速度、位置、状态、参数），
    /// 并提供必要的命令编码（如启停、模式/速度、间距、参数、查询状态/参数）。
    /// </summary>
    public interface IUpstreamCodec {

        /// <summary>
        /// 从一帧报文解码出速度集合（mm/s）。
        /// </summary>
        /// <param name="frame">完整帧（通常含起止符，是否含控制码/长度由厂商决定）。</param>
        /// <param name="timestamp">速度帧的时间戳（UTC），由调用方提供。</param>
        /// <param name="set">成功时输出统一语义的 <see cref="SpeedSet"/>。</param>
        /// <returns>成功解码返回 <c>true</c>。</returns>
        bool TryDecodeSpeed(ReadOnlySpan<byte> frame, DateTime timestamp, out SpeedSet set);

        /// <summary>
        /// 从一帧报文解码出位姿列表（位置端）。
        /// </summary>
        bool TryDecodePositions(ReadOnlySpan<byte> frame, out IReadOnlyList<ParcelPose> poses);

        /// <summary>
        /// 从一帧报文解码出视觉状态快照（心跳/状态端）。
        /// </summary>
        bool TryDecodeStatus(ReadOnlySpan<byte> frame, out StatusSnapshot status);

        /// <summary>
        /// 从一帧报文解码出视觉参数（心跳/参数端）。
        /// </summary>
        bool TryDecodeParams(ReadOnlySpan<byte> frame, out VisionParams param);

        /// <summary>
        /// 编码“启动/停止”命令。
        /// </summary>
        /// <param name="writer">输出缓冲写入器。</param>
        /// <param name="start">true=启动；false=停止。</param>
        /// <returns>写入字节数。</returns>
        int EncodeStartStop(IBufferWriter<byte> writer, bool start);

        /// <summary>
        /// 编码“模式与运行速度上下限”的设置命令。
        /// </summary>
        /// <param name="writer">输出缓冲写入器。</param>
        /// <param name="mode">1=直通；2=分离（根据厂商定义）。</param>
        /// <param name="maxMmps">最大线速度（mm/s）。</param>
        /// <param name="minMmps">最小线速度（mm/s）。</param>
        int EncodeModeAndSpeed(IBufferWriter<byte> writer, byte mode, ushort maxMmps, ushort minMmps);

        /// <summary>
        /// 编码“设定包裹分离距离”的命令。
        /// </summary>
        int EncodeSpacing(IBufferWriter<byte> writer, ushort spacingMm);

        /// <summary>
        /// 编码“暂停/恢复”的命令（暂停后速度报文置零但软件不停止）。
        /// </summary>
        int EncodePause(IBufferWriter<byte> writer, bool pause);

        /// <summary>
        /// 编码“设置视觉软件参数”的命令（如疏散单元数量/速度、自动开始延时）。
        /// </summary>
        int EncodeSetParams(IBufferWriter<byte> writer, byte ejectCount, ushort ejectMmps, byte autoStartDelaySec);

        /// <summary>
        /// 编码“查询状态”的命令（视觉返回状态帧）。
        /// </summary>
        int EncodeQueryStatus(IBufferWriter<byte> writer);

        /// <summary>
        /// 编码“获取参数”的命令（视觉返回参数帧）。
        /// </summary>
        int EncodeGetParams(IBufferWriter<byte> writer);

        /// <summary>
        /// 设置解码器的轴布局（主/疏散轴数量）。用于运行时热更新，不需要重建实例。
        /// </summary>
        /// <param name="mainCount">主分离轴数量（可为 0）。</param>
        /// <param name="ejectCount">疏散轴数量（可为 0）。</param>
        void SetAxisLayout(int mainCount, int ejectCount);

        /// <summary>
        /// 设置二维网格布局与“横纵转换”选项
        /// </summary>
        /// <param name="source"></param>
        /// <param name="xCount"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        IReadOnlyList<int> SetGridLayout(IReadOnlyList<int> source, int xCount, bool enabled = true);

        /// <summary>
        /// 设置二维网格布局与“横纵转换”选项
        /// </summary>
        /// <param name="source"></param>
        /// <param name="xCount"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        SpeedSet SetGridLayout(SpeedSet source, int xCount, bool enabled = true);
    }
}