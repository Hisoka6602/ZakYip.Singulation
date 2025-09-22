using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Drivers.Abstractions.Events {

    // 小型状态载体，避免闭包分配
    public readonly struct EvState<T> {
        public readonly object Sender;
        public readonly EventHandler<T> Handler;
        public readonly T Args;

        public EvState(object sender, EventHandler<T> handler, T args) {
            Sender = sender; Handler = handler; Args = args;
        }
    }
}