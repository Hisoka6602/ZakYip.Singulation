using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {
    public enum PlannerStatus {
        Idle,
        Running,
        Degraded,
        Faulted
    }
}