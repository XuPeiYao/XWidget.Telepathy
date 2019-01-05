using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace XWidget.Telepathy.TestWeb3 {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            RouterHub<string>.Id = Guid.Parse("00000000-0000-0000-0000-000000000003");

            Thread.Sleep(1000 * 2);
            services.AddSignalR();
            services.AddTelepathy<string>("http://localhost:5001");
            RouterHub<string>.OnReceive += (object sender, Package<string> e) => {
                Console.WriteLine(e.Payload);
            };
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseTelepathy<string>();

            app.Use(async (context, next) => {
                if (context.Request.Path == "/test") {
                    await RouterHub<string>.SendAsync(new Package<string>() {
                        Payload = "WEB3的廣播"
                    });
                }
                if (context.Request.Path == "/test2") {
                    Console.WriteLine("送出訊息給WEB2");
                    await RouterHub<string>.SendAsync(new Package<string>() {
                        Target = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Payload = "WEB3 TO WEB2"
                    });
                }
                if (context.Request.Path == "/test3") {
                    Console.WriteLine("送出訊息給WEB1");
                    await RouterHub<string>.SendAsync(new Package<string>() {
                        Target = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Payload = "WEB3 TO WEB1"
                    });
                }
            });
        }
    }
}
