using System;
using System.Collections.Generic;
using System.Text;

namespace XWidget.Telepathy {
    /// <summary>
    /// 訊息包
    /// </summary>
    public class Package {
        /// <summary>
        /// 來源
        /// </summary>
        public Guid Source { get; set; }

        /// <summary>
        /// 目標，<see cref="default(Guid)"/>為廣播
        /// </summary>
        public Guid Target { get; set; }

        /// <summary>
        /// 傳輸路徑
        /// </summary>
        public Guid[] Path { get; set; }

        /// <summary>
        /// 已送出
        /// </summary>
        public Guid[] Sent { get; set; }

        /// <summary>
        /// 存活時間
        /// </summary>
        public uint TTL { get; set; } = 64;

        /// <summary>
        /// 附載
        /// </summary>
        public object Payload { get; set; }
    }
}
