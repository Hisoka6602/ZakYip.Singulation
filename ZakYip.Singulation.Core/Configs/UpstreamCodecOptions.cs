using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Configs {
    public sealed record UpstreamCodecOptions {
        /// <summary>分离轴数量</summary>
        public int MainCount { get; init; } = 28;
        /// <summary>疏散轴数量</summary>
        public int EjectCount { get; init; } = 3;
    }
}