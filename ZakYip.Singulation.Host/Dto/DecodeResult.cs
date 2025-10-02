using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Core.Contracts.Dto;

namespace ZakYip.Singulation.Host.Dto {

    public sealed class DecodeResult {
        public bool Ok { get; init; }
        public string Kind { get; init; } = "unknown";
        public int RawLength { get; init; }
        public SpeedSet? Speed { get; init; }
    }
}