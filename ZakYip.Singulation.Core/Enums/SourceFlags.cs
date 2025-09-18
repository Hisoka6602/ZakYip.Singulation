using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {
    [Flags]
    public enum SourceFlags {
        None = 0,
        Vision = 1,
        Simulated = 2
    }
}