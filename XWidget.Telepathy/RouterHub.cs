using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace XWidget.Telepathy {
    public class RouterHub<T> : Hub {
        public static Guid Id { get; set; } = Guid.NewGuid();
        public static uint TTL { get; set; } = 64;
        public static Dictionary<Guid, IClientProxy> RouterClient { get; private set; } = new Dictionary<Guid, IClientProxy>();

        #region 連線事件
        public override Task OnConnectedAsync() {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception) {
            var serverId = RouterClient.FirstOrDefault(x => x.Value == Clients.Caller).Key;
            RouterClient.Remove(serverId);

            return base.OnDisconnectedAsync(exception);
        }
        #endregion

        public async Task Connect(Guid serverId) {
            RouterClient[serverId] = Clients.Caller;
            await Clients.Caller.SendAsync("ConnectCallback", RouterHub<T>.Id);
        }

        public async Task ConnectCallback(Guid serverId) {
            throw new NotSupportedException();
        }

        public async Task Broadcast(Message<T> message) {
            // 曾經送過自己
            if (message.Path.Contains(RouterHub<T>.Id)) {
                return;
            }

            // 路徑加入自己
            message.Path.Add(RouterClient.SingleOrDefault(x => x.Value == Clients.Caller).Key);

            #region 廣播後處理程序
            OnReceiveBroadcast?.Invoke(this, message.Data);
            #endregion

            // TTL減少
            message.TTL--;

            // TTL大於0者繼續廣播
            if (message.TTL > 0) {
                // 取得本節點連接項目尚未送者
                var targets = RouterClient.Select(x => x.Key).Except(message.Sends).ToArray();

                // 加入自身
                message.Sends.Add(RouterHub<T>.Id);

                // 加入即將送出項目
                message.Sends.AddRange(targets);

                // 廣播
                Parallel.ForEach(targets, async client => {
                    await RouterClient[client].SendAsync("Broadcast", message);
                });
            }
        }

        public static async Task Broadcast(T data) {
            var message = new Message<T>() {
                Data = data,
                TTL = RouterHub<T>.TTL
            };
            message.Path.Add(RouterHub<T>.Id);
            message.Sends.Add(RouterHub<T>.Id);

            OnReceiveBroadcast?.Invoke(null, message.Data);

            // 取得本節點連接項目尚未送者
            var targets = RouterClient.Select(x => x.Key).Except(message.Sends).ToArray();

            // 加入即將送出項目
            message.Sends.AddRange(targets);

            // 廣播
            foreach (var client in targets) {
                await RouterClient[client].SendAsync("Broadcast", message);
            };
        }

        public static event EventHandler<T> OnReceiveBroadcast;


        internal static void FireOnReceiveBroadcast(Message<T> message) {
            OnReceiveBroadcast?.Invoke(null, message.Data);
        }
    }
}