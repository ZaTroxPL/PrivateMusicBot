using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

namespace PrivateMusicBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder().ConfigureAppConfiguration(x =>
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("settings.json", false, true)
                    .Build();

                x.AddConfiguration(config);
            })
            .ConfigureLogging(x =>
            {
                x.AddConsole();
                x.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureDiscordHost((context, config) =>
            {
                config.SocketConfig = new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Debug,
                    AlwaysDownloadUsers = false,
                    MessageCacheSize = 100
                };

                config.Token = context.Configuration["Token"];
            })
            .UseCommandService((context, config) => 
            {
                config.CaseSensitiveCommands = false;
                config.LogLevel = LogSeverity.Debug;
                config.DefaultRunMode = RunMode.Sync;
            })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<Services.CommandHandler>()
                    .AddLavaNode(x =>
                    {
                        x.SelfDeaf = true;                        
                    });
            })
            .UseConsoleLifetime();

            var host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
