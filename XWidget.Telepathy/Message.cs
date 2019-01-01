using System;
using System.Collections.Generic;
using System.Text;

namespace XWidget.Telepathy {
    public class Message<T> {
        /// <summary>
        /// 訊息路徑
        /// </summary>
        public List<Guid> Path { get; set; } = new List<Guid>();

        /// <summary>
        /// 已經送出對象
        /// </summary>
        public List<Guid> Sends { get; set; } = new List<Guid>();

        /// <summary>
        /// 路徑限制
        /// </summary>
        public uint TTL { get; set; } = 64;

        /// <summary>
        /// 傳遞資料
        /// </summary>
        public T Data { get; set; }
    }
}
