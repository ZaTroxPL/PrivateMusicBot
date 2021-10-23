﻿using Discord;
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
            this.client.MessageReceived += OnMessageReceived;
            this.client.Ready += OnReadyAsync;
            this.service.CommandExecuted += OnCommandExecuted;
            await this.service.AddModulesAsync(Assembly.GetEntryAssembly(), this.provider);
        }

        private async Task OnReadyAsync()
        {
            if (!this.lavaNode.IsConnected)
            {
                await this.lavaNode.ConnectAsync();
            }

            // Other ready related stuff
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
            if (!message.HasStringPrefix(this.configuration["Prefix"],ref position)) return;

            var context = new SocketCommandContext(this.client, message);
            await this.service.ExecuteAsync(context, position, this.provider);
        }
    }
}