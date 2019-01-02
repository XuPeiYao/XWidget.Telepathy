using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XWidget.Telepathy {
    internal class FakeClient : IClientProxy {
        public HubConnection Connection { get; private set; }

        public FakeClient(HubConnection connection) {
            this.Connection = connection;
        }

        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken)) {
            return Connection.SendCoreAsync(method, args, cancellationToken);
        }
    }
}
