using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace XWidget.Telepathy {
    public static class MiddlewareExtension {
        private static Dictionary<Guid, HubConnection> InternalClientMapping = new Dictionary<Guid, HubConnection>();

        public static IServiceCollection AddTelepathy<TPayload>(
            this IServiceCollection serviceCollection,
            params string[] serverList) {
            // 本節點作為其他節點的Client
            foreach (var server in serverList) {
                // 建立連線
                var connection = new HubConnectionBuilder()
                    .WithUrl($"{server}/telepathy")
                    .Build();
                try {
                    // 啟動客戶端
                    connection.StartAsync().GetAwaiter().GetResult();

                    // 設定掛勾，當收到Receive時調用事件
                    connection.On<Package<TPayload>>("Receive", async (Package<TPayload> package) => {
                        await RouterHub<TPayload>.GenericReceiveProcess(package, "Receive", async p => {
                            RouterHub<TPayload>.FireOnReceive(p);
                        });
                    });

                    // 設定掛勾，當收到TopographyUpdate時調用事件
                    connection.On<Package<KeyValuePair<Guid, Guid[]>>>("TopographyUpdate", async (Package<KeyValuePair<Guid, Guid[]>> package) => {
                        await RouterHub<TPayload>.GenericReceiveProcess(package, "Receive", async p => {
                            RouterHub<TPayload>.Topography[p.Payload.Key] = p.Payload.Value;
                        });
                    });

                    // 連線中斷
                    connection.Closed += async (Exception e) => {
                        var serverId = GetCurrentServerId();

                        if (RouterHub<TPayload>.RouterClients.ContainsKey(serverId)) {
                            RouterHub<TPayload>.RouterClients.Remove(serverId);
                        }
                        if (RouterHub<TPayload>.Topography.ContainsKey(serverId)) {
                            RouterHub<TPayload>.Topography.Remove(serverId);
                        }
                    };

                    connection.SendAsync("ServerIdQuery").GetAwaiter().GetResult();
                } catch { }
            }

            return serviceCollection;
        }

        /// <summary>
        /// 使用心電感應
        /// </summary>
        /// <param name="app"></param>
        /// <param name="serverList"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseTelepathy<T>(
            this IApplicationBuilder app) {
            return app.UseSignalR(config => {
                config.MapHub<RouterHub<T>>("/telepathy");
            });
        }
    }
}
