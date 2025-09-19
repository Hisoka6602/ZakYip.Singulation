using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Host.Transports;

namespace ZakYip.Singulation.Host.Runtime {

    public class RuntimeStatusProvider : IRuntimeStatusProvider {

        public RuntimeStatus Snapshot() {
            return null;
        }
    }
}