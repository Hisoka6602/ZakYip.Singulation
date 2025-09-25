using Polly;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace ZakYip.Singulation.Host.SignalR.Hubs {

    public sealed class EventsHub : Hub {

        public Task Join(string channel)
            => Groups.AddToGroupAsync(Context.ConnectionId, channel);

        public Task Leave(string channel)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
    }
}