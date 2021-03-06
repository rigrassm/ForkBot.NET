﻿using Discord;
using Discord.Commands;
using Discord.Rest;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    [Summary("Generates and queues various silly trade additions")]
    public class GiveawayModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("giveawayqueue")]
        [Alias("gaq")]
        [Summary("Prints the users in the giveway queues.")]
        [RequireSudo]
        public async Task GetGiveawayListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Giveaways";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("giveawayupload")]
        [Alias("gu", "gup")]
        [Summary("Uploads the Pokémon you show via Link Trade to the Giveaway Pool.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveawayUploader))]
        public async Task GiveawayUploadAsync([Summary("Display Name")] string name, [Summary("Tag")] string tag)
        {
            int status = 0;

            PK8 messageContainer = new PK8
            {
                OT_Name = name,
                HT_Name = tag,
                Status_Condition = status
            };

            var sig = Context.User.GetFavor();
            var code = Info.GetRandomTradeCode();
            await Context.AddToQueueAsync(code, Context.User.Username, sig, messageContainer, PokeRoutineType.FlexTrade, PokeTradeType.GiveawayUpload).ConfigureAwait(false);
        }

        [Command("giveawaypool")]
        [Alias("gap")]
        [Summary("Show a list of Pokémon available for giveaway.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task DisplayGiveawayPoolCountAsync()
        {
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            var activePool = poolDB.GetPool(false);
            List<string> lines = new();

            if (activePool.Count > 0)
            {
                foreach (GiveawayPoolEntry entry in activePool)
                {

                    lines.Add(entry.GetSummary(false));
                }
                var msg = string.Join("\n", lines);
                await ListUtil("Giveaway Pool Details", msg).ConfigureAwait(false);
            }
            else await ReplyAsync($"Giveaway pool is empty.").ConfigureAwait(false);
        }

        [Command("giveawaypool")]
        [Alias("gap", "gas")]
        [Summary("Search the Giveaway Pool.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task SearchGiveawayPoolAsync([Remainder] string search)
        {
            var searchString = "%" + search + "%";
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            var activePool = poolDB.SearchPool(searchString);
            List<string> lines = new();

            if (activePool.Count > 0)
            {
                foreach (GiveawayPoolEntry entry in activePool)
                {
                    lines.Add(entry.GetSummary(false));
                }
                var msg = string.Join("\n", lines);
                await ListUtil("Giveaway Pool Details", msg).ConfigureAwait(false);
            }
            else await ReplyAsync($"Giveaway pool is empty.").ConfigureAwait(false);
        }

        [Command("giveawayitempool")]
        [Alias("gip")]
        [Summary("Search for Giveaway Items by name.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task GetItemEntries()
        {
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            var activePool = poolDB.GetPoolByTag("Item");
            List<string> lines = new();

            if (activePool.Count > 0)
            {
                foreach (GiveawayPoolEntry entry in activePool)
                {
                    lines.Add(entry.GetSummary(true));
                }
                var msg = string.Join("\n", lines);
                await ListUtil("Giveaway Item Pool Details", msg).ConfigureAwait(false);
            }
            else await ReplyAsync($"Giveaway Item pool is empty.").ConfigureAwait(false);
        }

        [Command("giveaway")]
        [Alias("ga", "giveme", "gimme", "gimmie", "gimmy", "goomy")]
        [Summary("Makes the bot trade you the specified giveaway Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveaway))]
        public async Task GiveawayAsync([Summary("ID/Name of Pokemon to Recieve")] string Item)
        {
            var code = Info.GetRandomTradeCode();
            PK8 pk;
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            var activePool = Info.Hub.GiveawayPoolDatabase.GetPool(true);
            GiveawayPoolEntry entry;
            if (activePool.Count == 0)
            {
                await ReplyAsync($"Giveaway pool is empty.").ConfigureAwait(false);
                return;
            }

            var content = ReusableActions.StripCodeBlock(Item);

            bool reqIsIndex = int.TryParse(content, out var poolId); // Check if the user provided an index rather than name
            if (!reqIsIndex) // Not an integer so treat it as a name
            {
                var requestName = string.Concat(content.Select(char.ToLower));
                entry = activePool.Find(x => x.Name.Equals(requestName));
                if (entry.Id != 0)
                {
                    pk = poolDB.GetEntryPK8(entry.Id);
                }
                else
                {
                    await ReplyAsync($"Requested Pokémon not available, use \"{Info.Hub.Config.Discord.CommandPrefix}giveawaypool\" for a full list of available giveaways!").ConfigureAwait(false);
                    return;
                }
            }
            else if (poolId <= activePool.Count) // Request is an integer and will be treated as an index so check that it's within range
            {
                entry = activePool.Find(x => x.Id.Equals(poolId));
                if (entry.Id != 0)
                {
                    pk = poolDB.GetEntryPK8(entry.Id);

                }
                else
                {
                    await ReplyAsync($"Provided index does not exist, use \"{Info.Hub.Config.Discord.CommandPrefix}giveawaypool\" for a full list of available giveaways!").ConfigureAwait(false);
                    return;
                }
            }
            else
            {
                await ReplyAsync($"Provided index does not exist, use \"{Info.Hub.Config.Discord.CommandPrefix}giveawaypool\" for a full list of available giveaways!").ConfigureAwait(false);
                return;
            }

            var sig = Context.User.GetFavor();
            await Context.AddToQueueAsync(code, Context.User.Username, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Giveaway, Context.User).ConfigureAwait(false);
        }

        [Command("update_entry_name")]
        [Summary("Updates the specified entries name.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveawayUploader))]
        public async Task GiveawayEntryUpdateName([Summary("Pool Entry ID")] string id, [Summary("Name")] string name)
        {
            var poolIDIsInt = int.TryParse(id, out var poolId);
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            int newEntryID = 0;
            if (poolIDIsInt)
            {
                newEntryID = poolDB.UpdateEntry(poolId, "Name", name);
            }
            if (newEntryID != 0)
            {
                await ReplyAsync($"Updated Entry name to " + name + " successfully").ConfigureAwait(false);
                return;
            }
            else
            {
                await ReplyAsync($"Error updating entry, please check the logs").ConfigureAwait(false);
                return;
            }
        }

        [Command("update_entry_tag")]
        [Summary("Updates the specified entries tag.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveawayUploader))]
        public async Task GiveawayEntryUpdateTag([Summary("Pool Entry ID")] string id, [Summary("Tag")] string tag)
        {
            var poolIDIsInt = int.TryParse(id, out var poolId);
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            int newEntryID = 0;
            if (poolIDIsInt)
            {
                 newEntryID = poolDB.UpdateEntry(poolId, "Tag", tag);
            }
            if (newEntryID != 0)
            {
                await ReplyAsync($"Updated Entry tag to " + tag + " successfully").ConfigureAwait(false);
                return;
            }
            else
            {
                await ReplyAsync($"Error updating entry, please check the logs").ConfigureAwait(false);
                return;
            }
        }

        [Command("update_entry_description")]
        [Summary("Updates the specified entries description.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveawayUploader))]
        public async Task GiveawayEntryUpdateDescription([Summary("Pool Entry ID")] string id, [Summary("Description")] string desc)
        {
            var poolIDIsInt = int.TryParse(id, out var poolId);
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            int newEntryID = 0;
            if (poolIDIsInt)
            {
                newEntryID = poolDB.UpdateEntry(poolId, "Description", desc);
            }

            if (newEntryID != 0)
            {
                await ReplyAsync($"Updated Entry description to `" + desc + "` description successfully").ConfigureAwait(false);
                return;
            }
            else
            {
                await ReplyAsync($"Error updating entry, please check the logs").ConfigureAwait(false);
                return;
            }
        }

        [Command("update_entry_status")]
        [Summary("Updates the specified entries name.")]
        [RequireQueueRole(nameof(DiscordManager.RolesGiveawayUploader))]
        public async Task GiveawayEntryUpdateStatus([Summary("Pool Entry ID")] string id, [Summary("Status")] string status)
        {
            var poolIDIsInt = int.TryParse(id, out var poolId);
            var poolDB = Info.Hub.GiveawayPoolDatabase;
            int newEntryID = 0;
            if (poolIDIsInt)
            {
                newEntryID = poolDB.UpdateEntry(poolId, "Status", status);
            }
            if (newEntryID != 0)
            {
                await ReplyAsync($"Updated Entry status to " + status + " successfully").ConfigureAwait(false);
                return;
            }
            else
            {
                await ReplyAsync($"Error updating entry, please check the logs").ConfigureAwait(false);
                return;
            }
        }
        private async Task ListUtil(string nameMsg, string entry)
        {
            var index = 0;
            List<string> pageContent = new();
            var emptyList = "No results found.";
            bool canReact = Context.Guild.CurrentUser.GetPermissions(Context.Channel as IGuildChannel).AddReactions;
            var round = Math.Round((decimal)entry.Length / 1024, MidpointRounding.AwayFromZero);

            if (entry.Length > 1024)
            {
                for (int i = 0; i <= round; i++)
                {
                    var splice = TradeExtensions.SpliceAtWord(entry, index, 1024);
                    index += splice.Count;
                    if (splice.Count == 0)
                        break;

                    pageContent.Add(string.Join(entry.Contains(",") ? ", " : "\n", splice));
                }
            }
            else pageContent.Add(entry == "" ? emptyList : entry);

            var embed = new EmbedBuilder { Color = Color.DarkBlue }.AddField(x =>
            {
                x.Name = nameMsg;
                x.Value = pageContent[0];
                x.IsInline = false;
            }).WithFooter(x =>
            {
                x.IconUrl = "https://i.imgur.com/nXNBrlr.png";
                x.Text = $"Page 1 of {pageContent.Count}";
            });

            if (!canReact && pageContent.Count > 1)
            {
                embed.AddField(x =>
                {
                    x.Name = "Missing \"Add Reactions\" Permission";
                    x.Value = "Displaying only the first page of the list due to embed field limits.";
                });
            }

            var msg = await Context.Message.Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
            if (pageContent.Count > 1 && canReact)
                _ = Task.Run(async () => await ReactionAwait(msg, nameMsg, pageContent).ConfigureAwait(false));
        }

        private async Task ReactionAwait(RestUserMessage msg, string nameMsg, List<string> pageContent)
        {
            int page = 0;
            var userId = Context.User.Id;
            IEmote[] reactions = { new Emoji("⬅️"), new Emoji("➡️") };
            await msg.AddReactionsAsync(reactions).ConfigureAwait(false);
            var sw = new Stopwatch();
            sw.Start();

            while (sw.ElapsedMilliseconds < 30_000)
            {
                var collectorBack = await msg.GetReactionUsersAsync(reactions[0], 100).FlattenAsync().ConfigureAwait(false);
                var collectorForward = await msg.GetReactionUsersAsync(reactions[1], 100).FlattenAsync().ConfigureAwait(false);
                IUser? UserReactionBack = collectorBack.FirstOrDefault(x => x.Id == userId && !x.IsBot);
                IUser? UserReactionForward = collectorForward.FirstOrDefault(x => x.Id == userId && !x.IsBot);

                if (UserReactionBack != null && page > 0)
                {
                    page--;
                    var embedBack = new EmbedBuilder { Color = Color.DarkBlue }.AddField(x =>
                    {
                        x.Name = nameMsg;
                        x.Value = pageContent[page];
                        x.IsInline = false;
                    }).WithFooter(x =>
                    {
                        x.IconUrl = "https://i.imgur.com/nXNBrlr.png";
                        x.Text = $"Page {page + 1 } of {pageContent.Count}";
                    }).Build();

                    await msg.RemoveReactionAsync(reactions[0], UserReactionBack);
                    await msg.ModifyAsync(msg => msg.Embed = embedBack).ConfigureAwait(false);
                    sw.Restart();
                }
                else if (UserReactionForward != null && page < pageContent.Count - 1)
                {
                    page++;
                    var embedForward = new EmbedBuilder { Color = Color.DarkBlue }.AddField(x =>
                    {
                        x.Name = nameMsg;
                        x.Value = pageContent[page];
                        x.IsInline = false;
                    }).WithFooter(x =>
                    {
                        x.IconUrl = "https://i.imgur.com/nXNBrlr.png";
                        x.Text = $"Page {page + 1} of {pageContent.Count}";
                    }).Build();

                    await msg.RemoveReactionAsync(reactions[1], UserReactionForward);
                    await msg.ModifyAsync(msg => msg.Embed = embedForward).ConfigureAwait(false);
                    sw.Restart();
                }
            }
            await msg.DeleteAsync().ConfigureAwait(false);
        }

        private string PokeImg(PKM pkm, bool canGmax, uint alcremieDeco)
        {
            bool md = false;
            bool fd = false;
            if (TradeExtensions.GenderDependent.Contains(pkm.Species) && !canGmax && pkm.AltForm == 0)
            {
                if (pkm.Gender == 0)
                    md = true;
                else fd = true;
            }

            var baseLink = "https://projectpokemon.org/images/sprites-models/homeimg/poke_capture_0001_000_mf_n_00000000_f_n.png".Split('_');
            baseLink[2] = pkm.Species < 10 ? $"000{pkm.Species}" : pkm.Species < 100 && pkm.Species > 9 ? $"00{pkm.Species}" : $"0{pkm.Species}";
            baseLink[3] = pkm.AltForm < 10 ? $"00{pkm.AltForm}" : $"0{pkm.AltForm}";
            baseLink[4] = pkm.PersonalInfo.OnlyFemale ? "fo" : pkm.PersonalInfo.OnlyMale ? "mo" : pkm.PersonalInfo.Genderless ? "uk" : fd ? "fd" : md ? "md" : "mf";
            baseLink[5] = canGmax ? "g" : "n";
            baseLink[6] = "0000000" + (pkm.Species == (int)Species.Alcremie ? alcremieDeco : 0);
            baseLink[8] = pkm.IsShiny ? "r.png" : "n.png";

            return string.Join("_", baseLink);
        }

        private async Task EmbedUtil(EmbedBuilder embed, string name, string value)
        {
            var splitName = name.Split(new string[] { "&^&" }, StringSplitOptions.None);
            var splitValue = value.Split(new string[] { "&^&" }, StringSplitOptions.None);
            for (int i = 0; i < splitName.Length; i++)
            {
                embed.AddField(x =>
                {
                    x.Name = splitName[i];
                    x.Value = splitValue[i];
                    x.IsInline = false;
                });
            }

            await Context.Message.Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
        }
    }
}
