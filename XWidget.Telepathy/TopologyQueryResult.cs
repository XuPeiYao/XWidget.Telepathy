using System;
using System.Collections.Generic;
using System.Text;

namespace XWidget.Telepathy {
    public class TopologyQueryResult {
        public Guid Source { get; set; }
        public Guid[] Connections { get; set; }
    }
}
