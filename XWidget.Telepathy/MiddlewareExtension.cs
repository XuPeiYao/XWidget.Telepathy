using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XWidget.Telepathy {
    public static class MiddlewareExtension {
        private static Dictionary<Guid, HubConnection> InternalClientMapping = new Dictionary<Guid, HubConnection>();

        public static IServiceCollection AddTelepathy<T>(
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

                    // 設定掛勾，當收到廣播時調用事件
                    connection.On<Message<T>>("Broadcast", (Message<T> data) => {
                        RouterHub<T>.FireOnReceiveBroadcast(data);
                    });

                    // 當遠端註冊 Client 後回呼客端方法
                    connection.On<Guid>("ConnectCallback", (Guid serverId) => {
                        RouterHub<T>.RouterClient[serverId] = new FakeClient() {
                            Client = connection
                        };
                        InternalClientMapping[serverId] = connection;
                    });

                    // 連線中斷
                    connection.Closed += async (Exception e) => {
                        var serverId = InternalClientMapping.First(x => x.Value == connection).Key;
                        RouterHub<T>.RouterClient.Remove(serverId);
                    };

                    // 調用遠端 Connect 方法註冊本地ID
                    connection.SendAsync("Connect", RouterHub<T>.Id);
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
