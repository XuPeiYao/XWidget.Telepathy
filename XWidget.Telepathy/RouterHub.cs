using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XWidget.Telepathy {
    /// <summary>
    /// 路由
    /// </summary>
    public class RouterHub<TPayload> : Hub {
        public static Task TopographyUpdateLoop { get; set; }

        /// <summary>
        /// 路由唯一識別號
        /// </summary>
        public static Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// 路由客戶端
        /// </summary>
        public static Dictionary<Guid, IClientProxy> RouterClients { get; private set; } = new Dictionary<Guid, IClientProxy>();

        /// <summary>
        /// 拓譜
        /// </summary>
        public static Dictionary<Guid, Guid[]> Topography { get; private set; } = new Dictionary<Guid, Guid[]>();

        /// <summary>
        /// 取得目前連線的客戶端唯一識別號
        /// </summary>
        /// <returns>唯一識別號</returns>
        public Guid GetCurrentServerId() {
            return Guid.Parse(Context.GetHttpContext().Request.Query["serverId"]);
        }

        /// <summary>
        /// 取得轉送節點
        /// </summary>
        /// <param name="target">最終目標節點</param>
        /// <returns>轉送節點</returns>
        public static Guid? GetSendTarget(Guid target, uint ttl, out uint lastTTL) {
            if (Topography.ContainsKey(target)) {
                lastTTL = ttl - 1;
                return target;
            }

            if (ttl == 0) {
                lastTTL = 0;
                return null; //無法送達
            }

            var canSendToTargetServers = Topography.Where(x => x.Value.Contains(target));

            var result = canSendToTargetServers.Select(x => x.Key).Select(x => {
                var nextHop = GetSendTarget(x, ttl - 1, out uint _lastTTL);
                return new {
                    targetId = nextHop,
                    ttl = _lastTTL
                };
            }).Where(x => x.targetId.HasValue).OrderBy(x => x.ttl).FirstOrDefault();

            lastTTL = result?.ttl ?? 0;
            return result?.targetId;
        }

        #region 事件
        /// <summary>
        /// 連線事件
        /// </summary>
        /// <returns></returns>
        public override Task OnConnectedAsync() {
            var serverId = GetCurrentServerId();

            RouterClients[serverId] = Clients.Caller;

            SendTopographyUpdate();

            return base.OnConnectedAsync();
        }

        /// <summary>
        /// 斷線事件
        /// </summary>
        /// <param name="exception">例外</param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception exception) {
            var serverId = GetCurrentServerId();

            if (RouterClients.ContainsKey(serverId)) {
                RouterClients.Remove(serverId);
            }
            if (Topography.ContainsKey(serverId)) {
                Topography.Remove(serverId);
            }

            SendTopographyUpdate();

            return base.OnDisconnectedAsync(exception);
        }
        #endregion

        #region 提供遠端調用
        internal static async Task GenericReceiveProcess<T>(Package<T> package, string method, Action<Package<T>> process) {
            // 訊息包路徑更新
            package.Path = package.Path.Concat(new Guid[] { RouterHub<TPayload>.Id }).ToArray();

            // 減少存活時間
            package.TTL--;

            // 本地可接收項目
            if (package.Target == default(Guid) ||
                package.Target == RouterHub<TPayload>.Id) {
                process?.Invoke(package);
            }

            // 尚存活可進行轉傳的訊息包
            if (package.TTL > 0) {
                if (package.Target == default(Guid)) {
                    // 廣播，需要進行轉傳

                    // 過濾已經廣播過的項目，取得本節點要進行廣播的目標
                    var willSend = RouterClients.Keys.Except(package.Sent).ToArray();

                    // 將要進行廣播的項目作為已經送出的目標，防止目標持續轉送重複節點
                    package.Sent = package.Sent.Concat(willSend).ToArray();

                    // 廣播
                    Parallel.ForEach(willSend, async clientId => {
                        await RouterClients[clientId].SendAsync(method, package);
                    });
                } else if (package.Target != RouterHub<TPayload>.Id) {
                    // 不是廣播也不是傳給本節點，則表示本節點為中間節點，需要進行轉傳
                    var nextServer = GetSendTarget(package.Target, package.TTL, out _);
                    if (!nextServer.HasValue) {
                        return;
                    }

                    var willSend = new Guid[] { nextServer.Value };

                    // 將要進行廣播的項目作為已經送出的目標，防止目標持續轉送重複節點
                    package.Sent = package.Sent.Concat(willSend).ToArray();

                    // 廣播
                    Parallel.ForEach(willSend, async clientId => {
                        await RouterClients[clientId].SendAsync(method, package);
                    });
                }
            }
        }
        /// <summary>
        /// 訊息包接收
        /// </summary>
        /// <param name="package">訊息包</param>
        /// <returns></returns>
        public async Task Receive(Package<TPayload> package) {
            await GenericReceiveProcess(package, "Receive", p => {
                OnReceive?.Invoke(null, p);
            });
        }

        /// <summary>
        /// 拓譜更新
        /// </summary>
        /// <param name="package">拓補訊息包</param>
        /// <returns></returns>
        public async Task TopographyUpdate(Package<TopographyInfo> package) {
            await GenericReceiveProcess(package, "TopographyUpdate", p => {
                Topography[package.Payload.Source] = package.Payload.Targets;
            });
        }
        #endregion

        /// <summary>
        /// 送出訊息包
        /// </summary>
        /// <param name="package">訊息包</param>
        /// <returns></returns>
        public static async Task SendAsync(Package<TPayload> package) {
            package.Source = RouterHub<TPayload>.Id;
            package.Path = new Guid[] { package.Source };
            package.Sent = new Guid[] { package.Source };

            // 自我循環測試，直接調用本地方法
            if (package.Target == RouterHub<TPayload>.Id) {
                await GenericReceiveProcess(package, "Receive", p => {
                    OnReceive?.Invoke(null, p);
                });
                return;
            }

            // 廣播
            if (package.Target == default(Guid)) {
                // 過濾已經廣播過的項目，取得本節點要進行廣播的目標
                var willSend = RouterClients.Keys.Except(package.Sent).ToArray();

                // 將要進行廣播的項目作為已經送出的目標，防止目標持續轉送重複節點
                package.Sent = package.Sent.Concat(willSend).ToArray();

                OnReceive?.Invoke(null, package);

                // 廣播
                Parallel.ForEach(willSend, async clientId => {
                    await RouterClients[clientId].SendAsync("Receive", package);
                });
            } else {
                // 尋找合適節點發送訊息包
                var target = GetSendTarget(package.Target, package.TTL, out _);

                if (!target.HasValue) {
                    throw new InvalidOperationException("找不到可達目標節點的路徑");
                }

                // 轉送訊息包
                await RouterClients[target.Value].SendAsync("Receive", package);
            }
        }

        /// <summary>
        /// 當接收到給于本節點的訊息包事件
        /// </summary>
        public static event EventHandler<Package<TPayload>> OnReceive;

        internal static void StartTopographyUpdateLoop() {
            TopographyUpdateLoop = Task.Run(() => {
                for (; ; ) {
                    Thread.Sleep(1000 * 10);
                    SendTopographyUpdate();
                }
            });
        }

        internal static void SendTopographyUpdate() {
            try {
                var package = new Package<TopographyInfo>() {
                    Source = RouterHub<TPayload>.Id,
                    Path = new Guid[] { RouterHub<TPayload>.Id },
                    Sent = new Guid[] { RouterHub<TPayload>.Id },
                    Payload = new TopographyInfo() {
                        Source = RouterHub<TPayload>.Id,
                        Targets = RouterClients.Keys.ToArray()
                    }
                };


                // 取得本節點要進行廣播的目標
                var willSend = RouterClients.Keys.ToArray();

                // 將要進行廣播的項目作為已經送出的目標，防止目標持續轉送重複節點
                package.Sent = package.Sent.Concat(willSend).ToArray();

                // 廣播
                Parallel.ForEach(willSend, async clientId => {
                    await RouterClients[clientId].SendAsync("TopographyUpdate", package);
                });
            } catch { }
        }
    }
}
