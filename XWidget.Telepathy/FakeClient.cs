using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XWidget.Telepathy {
    public class FakeClient : IClientProxy {
        public HubConnection Client { get; set; }
        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken)) {
            return Client.SendCoreAsync(method, args, cancellationToken);
        }
    }
}
