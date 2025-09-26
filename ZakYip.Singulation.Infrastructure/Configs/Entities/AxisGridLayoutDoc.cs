using LiteDB;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Infrastructure.Configs.Entities {

    public sealed class AxisGridLayoutDoc {
        [BsonId] public string Id { get; set; } = "singleton";
        public int Rows { get; set; }
        public int Cols { get; set; }
    }
}