using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XWidget.Telepathy.Test {
    public class ConnectionTest {
        [Fact]
        public async Task TwoServerDoubleTest() {


            Startup.Ports.Add(10001);
            Startup.Ports.Add(10002);

            await WebHostBuild.Build(10001).StartAsync();
            await WebHostBuild.Build(10002).StartAsync();

        }
    }
}
