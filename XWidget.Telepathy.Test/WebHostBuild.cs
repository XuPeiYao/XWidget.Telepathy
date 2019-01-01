using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Net;
using Xunit;

namespace XWidget.Telepathy.Test {
    public class WebHostBuild {
        public static IWebHost Build(int port) =>
            WebHost.CreateDefaultBuilder(new string[0])
                .UseKestrel(options => {
                    options.Listen(IPAddress.Loopback, port);
                })
                .UseStartup<Startup>()
                .Build();

    }
}
