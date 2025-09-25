using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZakYip.Singulation.Core.Enums {

    public enum TransportRole {

        /// <summary>
        /// 客户端
        /// </summary>
        [Description("客户端")]
        Client = 0,

        /// <summary>
        /// 服务端
        /// </summary>
        [Description("服务端")]
        Server = 1
    }
}