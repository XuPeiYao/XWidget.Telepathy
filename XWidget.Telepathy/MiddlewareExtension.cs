using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace XWidget.Telepathy {
    public static class MiddlewareExtension {
        public static IServiceCollection AddTelepathy<TPayload>(
            this IServiceCollection serviceCollection,
            params string[] serverList) {
            // 啟動拓譜更新迴圈
            RouterHub<TPayload>.StartTopographyUpdateLoop();

            // 本節點作為其他節點的Client
            foreach (var server in serverList) {
                // 建立連線
                var connection = new HubConnectionBuilder()
                    .WithUrl($"{server}/telepathy?serverId={RouterHub<TPayload>.Id}")
                    .Build();

                try {
                    var serverId = Guid.Parse(new HttpClient().GetStringAsync($"{server}/telepathy/id").GetAwaiter().GetResult());

                    var virtualRouter = new RouterHub<TPayload>();

                    // 設定掛勾，當收到Receive時調用事件
                    connection.On<Package<TPayload>>("Receive", async (Package<TPayload> package) => await virtualRouter.Receive(package));

                    // 設定掛勾，當收到TopographyUpdate時調用事件
                    connection.On<Package<TopographyInfo>>("TopographyUpdate", async (Package<TopographyInfo> package) => await virtualRouter.TopographyUpdate(package));

                    // 連線中斷
                    connection.Closed += async (Exception e) => await virtualRouter.OnDisconnectedAsync(e);

                    // 啟動客戶端
                    connection.StartAsync().GetAwaiter().GetResult();

                    RouterHub<TPayload>.RouterClients[serverId] = new FakeClient(connection);
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
            app.Use(async (context, next) => {
                if (context.Request.Path == "/telepathy/id") {
                    context.Response.ContentType = "text/plain";
                    var binaryId = Encoding.UTF8.GetBytes(RouterHub<T>.Id.ToString());
                    context.Response.Body.Write(binaryId, 0, binaryId.Length);
                    return;
                }
                await next();
            });
            return app.UseSignalR(config => {
                config.MapHub<RouterHub<T>>("/telepathy");
            });
        }
    }
}
