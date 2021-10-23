using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace PrivateMusicBot.Commands
{
    public class General : ModuleBase<SocketCommandContext>
    {
        private readonly LavaNode lavaNode;

        public General(LavaNode lavaNode)
        {
            this.lavaNode = lavaNode;
        }

        [Command("connect")]
        [Alias("c")]
        public async Task ConnectAsync()
        {
            if (lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                await lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Joined ${voiceState.VoiceChannel.Name}");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("disconnect")]
        [Alias("d")]
        public async Task DisnonnectAsync()
        {
            if (!lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm already disconnected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                await lavaNode.LeaveAsync(voiceState.VoiceChannel);
                await ReplyAsync($"Until next time!");
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("play")]
        [Alias("p")]
        public async Task PlayAsync([Remainder] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            if (!lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            
            var searchResponse = await lavaNode.SearchAsync(SearchType.YouTube, query);
            if (searchResponse.Status == SearchStatus.LoadFailed ||
                searchResponse.Status == SearchStatus.NoMatches)
            {
                await ReplyAsync($"I wasn't able to find anything for `{query}`.");
                return;
            }

            var player = lavaNode.GetPlayer(Context.Guild);

            if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
            {
                var track = searchResponse.Tracks.First();
                player.Queue.Enqueue(track);
                await ReplyAsync($"Enqueued: {track.Title}");
                
            }
            else
            {
                var track = searchResponse.Tracks.First();                
                await player.PlayAsync(track);
                await ReplyAsync($"Now Playing: {track.Title}");                
            }
            
        }     
        
        [Command("next")]
        [Alias("n")]
        public async Task NextAsync()
        {
            if (!lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            var player = lavaNode.GetPlayer(Context.Guild);
            if (voiceState.VoiceChannel != player.VoiceChannel)
            {
                await ReplyAsync("You must be connected to the same voice channel as me!");
                return;
            }

            if (player.Queue.Count == 0)
            {
                await ReplyAsync("There are no songs in the queue");
                return;
            }

            await player.SkipAsync();
            await ReplyAsync($"Playing the next song: {player.Track.Title}");
        }

        [Command("test")]
        public async Task TestAsync()
        {
            await ReplyAsync("working");
        }
    }
}
