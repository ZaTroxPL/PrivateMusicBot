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

        [Command("help")]
        [Alias("h")]
        public async Task HelpAsync()
        {
            Dictionary<string, string> commands = new Dictionary<string, string>() 
            {
                ["-connect (-c)"] = "Make the bot connect to the voice channel.",
                ["-disconnect (-d)"] = "Make the bot dissconnect from the voice channel.",
                ["-play ***query*** (-p)"] = "Play or queue songs from YouTube, query is required.",
                ["-next (-n)"] = "Skip to the next song in the queue.",
                ["-queue ***page no*** (-q)"] = "Show queue, page number is optional.",
                ["-help (-h)"] = "Show this help section.",
                ["-volume ***value*** (-v)"] = "Set the volume of the bot, value is required. **DOESN'T WORK**",
                ["-test"] = "Check if the bot is working.",
            };

            var embedCommands = "";
            var embedNotes = "";

            foreach (var command in commands)
            {
                embedCommands += command.Key + "\n";
                embedNotes += command.Value + "\n";
            }

            var embed = new EmbedBuilder()
                .WithTitle("Help Section")
                .AddField("Commands", embedCommands, true)
                .AddField("Notes", embedNotes, true)
                .Build();
            
            await ReplyAsync(embed: embed);
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
                await ReplyAsync("I'm already disconnected from a voice channel!");
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
                await player.UpdateVolumeAsync(50);
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

            // set the volume for the next track
            var volume = player.Volume.ToString();
            ushort.TryParse(volume, out ushort nextVolume);
            await player.UpdateVolumeAsync(nextVolume);

            await ReplyAsync($"Playing the next song: {player.Track.Title}");
        }

        [Command("queue")]
        [Alias("q")]
        public async Task QueueAsync([Remainder] string query = null)
        {
            var parsed = int.TryParse(query, out int page);
            if (!parsed && query != null)
            {
                await ReplyAsync("Provided page number doesn't seem to be a numeric value.");
                return;
            }

            if (!lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            var player = lavaNode.GetPlayer(Context.Guild);
            if (player.Queue.Count == 0)
            {
                await ReplyAsync("There are no songs in the queue");
                return;
            }

            var queue = player.Queue;
            var currentTrack = player.Track;

            var embedSongTitle = "";
            var embedDuration = "";

            // counter for keeping track of how many times the loop executed (should max at 10)
            int counter = 0;
            // initiating an index to use in the loop
            int i = 0;
            // if page is above 1, set the index, if not, leave the index at 0
            if (page > 1)
            {
                // set the index to appropriate starting value, we subtract 1 from var page because we want the index to start at beginning of a page
                i = (page - 1) * 10;

                // if the index is out of range, show the last page
                if (i > queue.Count)
                {
                    if (queue.Count > 10)
                    {
                        i = queue.Count - 10;
                    }
                    else
                    {
                        i = 0;
                    }
                }
            }

            while (counter < 10 && i < queue.Count)
            {
                var songTitle = $"{i + 1}) " + queue.ElementAt(i).Title;
                if (songTitle.Length > 65)
                {
                    songTitle = songTitle.Substring(0, 65).Trim() + "...";
                }
                embedSongTitle += songTitle + "\n";
                embedDuration += $"{queue.ElementAt(i).Duration.Minutes}:{queue.ElementAt(i).Duration.Seconds}" + "\n";
                counter++;

                i++;
            }

            var embed1 = new EmbedBuilder()
                .WithTitle("Currently Playing")
                .AddField("Song", currentTrack.Title, true)
                .AddField("Time Elapsed", $"{currentTrack.Position.Minutes}:{currentTrack.Position.Seconds}", true)
                .AddField("Duration", $"{currentTrack.Duration.Minutes}:{currentTrack.Duration.Seconds}", true)
                .Build();

            await ReplyAsync(embed: embed1);
            
            var embed2 = new EmbedBuilder()
                .WithTitle("Queue")
                .AddField("Song(s)", embedSongTitle, true)
                .AddField("Duration", embedDuration, true)
                .WithFooter(queue.Count - 10 > 0 ? $"There are {queue.Count - 10} more tracks in the queue." : "There are no more tracks in the queue.")
                .Build();
            
            await ReplyAsync(embed: embed2);

        }

        [Command("volume")]
        [Alias("v")]
        public async Task VolumeAsync([Remainder] string query)
        {
            var parsed = ushort.TryParse(query, out ushort volume);
            if (!parsed)
            {
                await ReplyAsync("Provided volume value doesn't seem to be numeric or is not between the range of 0 - 65535.");
                return;
            }

            if (volume > 500)
            {
                await ReplyAsync("... Did you really think I would let someone increase the volume to like 60,000?");
                return;
            }

            if (!lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel!");
                return;
            }

            var player = lavaNode.GetPlayer(Context.Guild);
            var voiceState = Context.User as IVoiceState;

            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            if (voiceState.VoiceChannel != player.VoiceChannel)
            {
                await ReplyAsync("You must be connected to the same voice channel as me!");
                return;
            }

            await player.UpdateVolumeAsync(volume);
        }

        [Command("test")]
        public async Task TestAsync()
        {
            await ReplyAsync("working");
        }
    }
}
