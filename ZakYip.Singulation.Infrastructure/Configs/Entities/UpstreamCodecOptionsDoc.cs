using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {
    public sealed record class UpstreamCodecOptionsDoc {
        public BsonValue Id { get; set; } = "default";
        public int MainCount { get; set; }
        public int EjectCount { get; set; }
    }
}