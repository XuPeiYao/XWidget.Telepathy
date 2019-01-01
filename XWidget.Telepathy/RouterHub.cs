using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace XWidget.Telepathy {
    public class RouterHub<T> : Hub {
        public static Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Router客戶端
        /// </summary>
        public static Dictionary<Guid, IClientProxy> RouterClients { get; private set; } = new Dictionary<Guid, IClientProxy>();

        /// <summary>
        /// 拓譜
        /// </summary>
        public static Dictionary<Guid, Guid[]> Topologies { get; set; } = new Dictionary<Guid, Guid[]>();

        #region 連線事件
        public override Task OnConnectedAsync() {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception) {
            var serverId = RouterClients.FirstOrDefault(x => x.Value == Clients.Caller).Key;
            if (RouterClients.ContainsKey(serverId)) {
                RouterClients.Remove(serverId);
            }
            if (Topologies.ContainsKey(serverId)) {
                Topologies.Remove(serverId);
            }

            return base.OnDisconnectedAsync(exception);
        }
        #endregion

        #region 提供客端調用
        /// <summary>
        /// 連線調用
        /// </summary>
        /// <param name="serverId">客端唯一識別號</param>
        /// <returns></returns>
        public async Task Connect(Guid serverId) {
            RouterClients[serverId] = Clients.Caller;
            await Clients.Caller.SendAsync("ConnectCallback", RouterHub<T>.Id);
        }

        // 無作用
        public async Task ConnectCallback(Guid serverId) {
            throw new NotSupportedException();
        }


        public async Task Topology(Guid sourceId) {

        }

        public async Task Broadcast(Message<T> message) {
            // 曾經送過自己，拋棄包
            if (message.Path.Contains(RouterHub<T>.Id)) {
                return;
            }

            // 路徑加入自己
            message.Path.Add(RouterClients.SingleOrDefault(x => x.Value == Clients.Caller).Key);

            #region 廣播後處理程序
            OnRawOnReceiveBroadcast?.Invoke(this, message);
            OnReceiveBroadcast?.Invoke(this, message.Data);
            #endregion

            // TTL減少
            message.TTL--;

            // TTL大於0者繼續廣播
            if (message.TTL > 0) {
                // 取得本節點連接項目尚未送者
                var targets = RouterClients.Select(x => x.Key).Except(message.Sends).ToArray();

                // 加入自身
                message.Sends.Add(RouterHub<T>.Id);

                // 加入即將送出項目
                message.Sends.AddRange(targets);

                // 廣播
                Parallel.ForEach(targets, async client => {
                    await RouterClients[client].SendAsync("Broadcast", message);
                });
            }
        }
        #endregion

        /// <summary>
        /// 發佈廣播
        /// </summary>
        /// <param name="message">訊息包</param>
        /// <param name="ignoreSelf">是否忽略引動發信者自身事件，預設忽略</param>
        /// <returns></returns>
        public static async Task RawBroadcast(Message<T> message, bool ignoreSelf = false) {
            if (!ignoreSelf) {
                OnRawOnReceiveBroadcast?.Invoke(null, message);
                OnReceiveBroadcast?.Invoke(null, message.Data);
            }

            // 取得本節點連接項目尚未送者
            var targets = RouterClients.Select(x => x.Key).Except(message.Sends).ToArray();

            // 加入即將送出項目
            message.Sends.AddRange(targets);

            // 廣播
            Parallel.ForEach(targets, async (client) => {
                await RouterClients[client].SendAsync("Broadcast", message);
            });
        }

        /// <summary>
        /// 發佈廣播
        /// </summary>
        /// <param name="data">訊息內容</param>
        /// <param name="ignoreSelf">是否忽略引動發信者自身事件，預設忽略</param>
        /// <returns></returns>
        public static async Task Broadcast(T data, bool ignoreSelf = true) {
            var message = new Message<T>() {
                Data = data
            };
            message.Path.Add(RouterHub<T>.Id);
            message.Sends.Add(RouterHub<T>.Id);

            await RawBroadcast(message, ignoreSelf);
        }

        public static async Task RawSend(Guid serverId, Message<T> message, bool ignoreSelf = true) {

        }

        public static async Task Send(Guid serverId, T data, bool ignoreSelf = true) {

        }

        /// <summary>
        /// 當接收到原始廣播訊息包
        /// </summary>
        public static event EventHandler<Message<T>> OnRawOnReceiveBroadcast;

        /// <summary>
        /// 當街收到廣播訊息
        /// </summary>
        public static event EventHandler<T> OnReceiveBroadcast;

        /// <summary>
        /// 發動事件
        /// </summary>
        /// <param name="message">訊息包</param>
        internal static void FireOnReceiveBroadcast(Message<T> message) {
            OnRawOnReceiveBroadcast?.Invoke(null, message);
            OnReceiveBroadcast?.Invoke(null, message.Data);
        }
    }
}