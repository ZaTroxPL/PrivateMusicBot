using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace PrivateMusicBot.Services
{
    public class CommandHandler : InitializedService
    {
        private readonly IServiceProvider provider;
        private readonly DiscordSocketClient client;
        private readonly CommandService service;
        private readonly IConfiguration configuration;
        private readonly LavaNode lavaNode;

        public CommandHandler(IServiceProvider provider, DiscordSocketClient client, CommandService service, IConfiguration configuration, LavaNode lavaNode)
        {
            this.provider = provider;
            this.client = client;
            this.service = service;
            this.configuration = configuration;
            this.lavaNode = lavaNode;
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            client.MessageReceived += OnMessageReceived;
            client.Ready += OnReadyAsync;
            service.CommandExecuted += OnCommandExecuted;
            await service.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
        }

        private async Task OnReadyAsync()
        {
            if (!lavaNode.IsConnected)
            {
                await lavaNode.ConnectAsync();
            }

            // Other ready related stuff
            lavaNode.OnTrackEnded += OnTrackEnded;
        }

        private async Task OnTrackEnded(TrackEndedEventArgs eventArgs)
        {
            if (eventArgs.Reason == TrackEndReason.Replaced)
            {
                return;
            }
            
            var player = eventArgs.Player;
            if (player.Queue == null)
            {
                return;
            }

            if (!player.Queue.TryDequeue(out var queueable))
            {
                await player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks to rock n' roll!");
                return;
            }

            if (!(queueable is LavaTrack track))
            {
                await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }

            // set the volume for the next track
            var volume = player.Volume.ToString();
            ushort.TryParse(volume, out ushort nextVolume);

            await eventArgs.Player.PlayAsync(track);
            await eventArgs.Player.UpdateVolumeAsync(nextVolume);
            await eventArgs.Player.TextChannel.SendMessageAsync($"{eventArgs.Reason}: {eventArgs.Track.Title}\nNow playing: {track.Title}");
        }

        private async Task OnCommandExecuted(Optional<CommandInfo> commandInfo, ICommandContext commandContext, IResult result)
        {
            if (result.IsSuccess) return;

            await commandContext.Channel.SendMessageAsync(result.ErrorReason);
        }

        private async Task OnMessageReceived(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var position = 0;
            if (!message.HasStringPrefix(configuration["Prefix"],ref position)) return;

            var context = new SocketCommandContext(client, message);
            await service.ExecuteAsync(context, position, provider);
        }
    }
}
