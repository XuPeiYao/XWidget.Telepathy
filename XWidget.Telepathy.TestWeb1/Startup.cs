﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace XWidget.Telepathy.TestWeb1 {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            Thread.Sleep(1000 * 2);
            services.AddSignalR();
            services.AddTelepathy<string>("http://localhost:5001");
            RouterHub<string>.OnReceiveBroadcast += (object sender, string e) => {
                Console.WriteLine(e);
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
                    await RouterHub<string>.Broadcast("TEST MESSAGE! FROM WEB1");
                }
            });
        }
    }
}
