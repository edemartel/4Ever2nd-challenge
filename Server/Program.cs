using System;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blitz.Server
{
    public class Program
    {
        private const int DefaultPort = 27178;
        
        private sealed class Options
        {
            [Option('p', "port", HelpText = "Set port number (default: 27178)", Default = DefaultPort)]
            public int Port { get; set; }
        }

        public static async Task Main(string[] args)
        {
            var port = DefaultPort;
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                port = o.Port;
            });

            var webHost = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:" + port);
                    webBuilder.ConfigureLogging(logging => logging.ClearProviders());
                })
                .Build();

            webHost.Services.GetService<MyBot>();
            await webHost.RunAsync();
        }
    }
}