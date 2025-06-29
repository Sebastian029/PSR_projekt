using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using App.Server;
using App.GrpcServer;
using System.Collections.Generic;
using Grpc.Core;
using Grpc.Net.Compression;

namespace MinimaxServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options =>
            {
                options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
                options.MaxSendMessageSize = 4 * 1024 * 1024;    // 4MB
                options.EnableDetailedErrors = true;
                options.CompressionProviders = new List<ICompressionProvider>
                {
                    new GzipCompressionProvider(System.IO.Compression.CompressionLevel.Fastest)
                };
                options.ResponseCompressionAlgorithm = "gzip";
                options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Fastest;
            });
            services.AddSingleton<IBoardEvaluator, Evaluator>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<CheckersEvaluationServiceImpl>();
                
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Checkers Minimax gRPC service is running.");
                });
            });
        }
    }
}