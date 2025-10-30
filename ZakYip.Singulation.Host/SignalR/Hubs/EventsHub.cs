using Polly;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace ZakYip.Singulation.Host.SignalR.Hubs {

    /// <summary>
    /// SignalR 事件中心，用于实时推送事件到客户端。
    /// </summary>
    public sealed class EventsHub : Hub {

        /// <summary>
        /// 加入指定的频道组。
        /// </summary>
        /// <param name="channel">频道名称。</param>
        /// <returns>异步任务。</returns>
        public Task Join(string channel)
            => Groups.AddToGroupAsync(Context.ConnectionId, channel);

        /// <summary>
        /// 离开指定的频道组。
        /// </summary>
        /// <param name="channel">频道名称。</param>
        /// <returns>异步任务。</returns>
        public Task Leave(string channel)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);

        /// <summary>
        /// 心跳检测方法，用于测量客户端延迟。
        /// </summary>
        /// <returns>异步任务。</returns>
        public Task Ping() => Task.CompletedTask;
    }
}