using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace XWidget.Telepathy {
    /// <summary>
    /// 訊息包
    /// </summary>
    public class Package<TPayload> : Package {
        /// <summary>
        /// 附載
        /// </summary>
        public new TPayload Payload {
            get {
                var rawPayload = base.Payload;
                return ((JToken)rawPayload).ToObject<TPayload>();
            }
            set {
                base.Payload = JToken.FromObject(value);
            }
        }
    }
}
