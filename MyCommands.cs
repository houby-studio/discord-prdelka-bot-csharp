using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;
using System.Net;

namespace prdelbot
{
    public class MyCommands
    {
        // ---------------------------------------------------------- VARIABLES ----------------------------------------------------------------------------------------------------------------------
        public List<DSharpPlus.Entities.DiscordUser> playerslist;                               // List of DiscordUsers containing defined players for certain game.

        public List<Tuple<DiscordUser, string>> nicknamelist = new List<Tuple<DiscordUser, string>>();
        public DSharpPlus.Entities.DiscordChannel storychannel = null;                          // Contains the ID of story channel which should every Discord have - Might be removed in the future
        public IReadOnlyList<DiscordDmChannel> DMChannels;
        public string gametype = "story";                                                       // Holds type of game that will be played after issuing start command
        public string storyoptions = "secret";                                                  // Holds the type of story game which we will play secret, normal, crazy
        public int round = 1;                                                                   // Holds the current round number (except starting from 0)
        public int currentplayer = 0;                                                           // Holds the ID of currently playing player
        public static bool firstsentence = true;                                                // Holds value whether the sentence is first thus title or not

        // Variables for Website output
        public static string webStoryTitle;
        public static string webStoryDate;
        public static string webStoryPageName;
        public static List<Tuple<string, string>> webStoryContent = new List<Tuple<string, string>>();

        // ###########################          Story stored in embed         ##############################
        public DSharpPlus.Entities.DiscordEmbedBuilder StoryEmbed = new DiscordEmbedBuilder
        {
            Color = new DiscordColor("#FFFFFF"),
            Title = $"Příběh",
        };

        // ############################## Template embed for GeneralMessages  ##############################
        public DSharpPlus.Entities.DiscordEmbedBuilder GeneralMessageEmbed = new DiscordEmbedBuilder
        {
            Color = new DiscordColor("#FFFFFF"),
            Title = "GeneralMessage",
        };

        // ############################## Template embed for NextSentenceGame ##############################
        public DSharpPlus.Entities.DiscordEmbedBuilder NextSentenceEmbed = new DiscordEmbedBuilder
        {
            Color = new DiscordColor("#FFFFFF"),
            Title = "NextSentenceMessage",
        };

        // ---------------------------------------------------------- COMMANDS ----------------------------------------------------------------------------------------------------------------------
        // Basic commands to greet the user
        [Command("hi")]
        public async Task Hi(CommandContext ctx)
        {
            ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot", "Saying hi", DateTime.Now);
            await ctx.RespondAsync($"👋 Hi, {ctx.User.Mention}!");
        }

        [Command("truth")]
        public async Task Truth(CommandContext ctx)
        {
            ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot", "Saying truth", DateTime.Now);
            await ctx.RespondAsync($"Borg smrdí.");
            var interactivity = ctx.Client.GetInteractivityModule();
            var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id && xm.Content.ToLower() == "how are you?", TimeSpan.FromMinutes(1));
            if (msg != null)
            {
                await ctx.RespondAsync($"I'm fine, thank you!");
            }
        }

        [Command("greet"), Description("Says hi to specified user."), Aliases("sayhi", "say_hi")]
        public async Task Greet(CommandContext ctx, [Description("The user to say hi to.")] DiscordMember member) // this command takes a member as an argument; you can pass one by username, nickname, id, or mention
        {
            ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot", "Greeting user", DateTime.Now);
            await ctx.TriggerTypingAsync();
            var emoji = DiscordEmoji.FromName(ctx.Client, ":wave:");
            // and finally, let's respond and greet the user.
            await ctx.RespondAsync($"{emoji} Hello, {member.Mention}!");
        }

        [Command("poll"), Description("Run a poll with reactions.")]
        public async Task Poll(CommandContext ctx, [Description("How long should the poll last.")] TimeSpan duration, [Description("What options should people have.")] params DiscordEmoji[] options)
        {
            ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot", "Setting up poll", DateTime.Now);
            // first retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivityModule();
            var poll_options = options.Select(xe => xe.ToString());

            // then let's present the poll
            var embed = new DiscordEmbedBuilder
            {
                Title = "Poll time!",
                Description = string.Join(" ", poll_options)
            };
            var msg = await ctx.RespondAsync(embed: embed);

            // add the options as reactions
            for (var i = 0; i < options.Length; i++)
                await msg.CreateReactionAsync(options[i]);

            // collect and filter responses
            var poll_result = await interactivity.CollectReactionsAsync(msg, duration);
            var results = poll_result.Reactions.Where(xkvp => options.Contains(xkvp.Key))
                .Select(xkvp => $"{xkvp.Key}: {xkvp.Value}");

            // and finally post the results
            await ctx.RespondAsync(string.Join("\n", results));
        }

        [Command("players")] // Functions as command to define new player set or to list already created player set stored in playerlist
        [Aliases("p", "playerlist", "pl", "gamesnici", "hraci")]
        [Description("Definuje seznam hráčů, kteří budou hrát nějakou z úžasných prdel her. Pokud bude příkaz použit bez parametrů, vypíše aktuální seznam hráčů.")]
        public async Task PlayerSet([Description("Zmiňte uživatele dle standardu Discordu, aby byl vytvořen seznam hráčů")]CommandContext ctx)
        {
            string playerliststring = " ";  // Transforms list of players into string so it can be send via message            
            if (ctx.Message.MentionedUsers.Count == 0)
            {
                if (playerslist == null)
                {
                    // If no players were mentioned or there are no players stored in variable, returns error message.
                    var embederror = new DiscordEmbedBuilder(GeneralMessageEmbed)
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "Nejsou definováni žádní hráči?!?!",
                        Description = "Pro pomoc s použitím příkazu použijte příkaz **help players**",
                    };
                    await ctx.Message.RespondAsync(embed: embederror.Build());
                    return;
                }
                // If no players were mentioned but there are players stored in variable already, it lists current players
                foreach (var player in playerslist) { playerliststring += (player.Username + " "); } // Serializes list of players into readable string to present to users
                var embedlist = new DiscordEmbedBuilder(GeneralMessageEmbed)
                {
                    Title = "Ač se to nezdá, seznam hráčů již existuje!",
                    Description = "Seznam hráčů: " + playerliststring,
                };
                embedlist.WithFooter("Promíchat pořadí hráčů můžete příkazem ~mingle a spustit hru můžete příkazem ~start", ctx.Client.CurrentUser.AvatarUrl);
                await ctx.Message.RespondAsync(embed: embedlist.Build());
                return;
            }
            else if (ctx.Message.MentionedUsers.Count == 1)
            {
                // There aint gonna be no single player in this game! Where all the fun?!
                var embederror = new DiscordEmbedBuilder(GeneralMessageEmbed)
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "Tohle není hra pro jednoho!",
                    Description = "Jestli chceš hrát a nemáš s kým, tak si běž honit finfulína, tohle chce miniálně 2 hráče!",
                };
                await ctx.Message.RespondAsync(embed: embederror.Build());
                return;
            }
            // If players were mentioned in command, then they will be added to list and presented to user
            playerslist = ctx.Message.MentionedUsers.ToList();
            // Serializes list of players into readable string to present to users and checks if no bots were mentioned
            foreach (var player in playerslist)
            {
                if (player.IsBot)
                {
                    var embederror = new DiscordEmbedBuilder(GeneralMessageEmbed)
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "Skutečně? BOT?",
                        Description = "To je to s tebou tak špatné, že chceš hrát i s botem? No tak, sežeň si opravdové lidi...",
                    };
                    await ctx.Message.RespondAsync(embed: embederror.Build());
                    return;
                }
                playerliststring += (player.Username + " ");
            }
            var embed = new DiscordEmbedBuilder(GeneralMessageEmbed)
            {
                Title = "Dámy a pánové! Dovoluji si představit nový úžasný tým!",
                Description = "Seznam hráčů: " + playerliststring,
            };
            embed.WithFooter("Promíchat pořadí hráčů můžete příkazem ~mingle a spustit hru můžete příkazem ~start", ctx.Client.CurrentUser.AvatarUrl);
            await ctx.Message.RespondAsync(embed: embed.Build());
            return;
        }

        [Command("storytype")]
        [Aliases("st")]
        [Description("Definuje typ hry, který se bude hrát.")]
        public async Task StoryType(CommandContext ctx, string storytype = null)
        {
            if (storytype == null)
            {
                var embed = new DiscordEmbedBuilder(GeneralMessageEmbed)
                {
                    Title = "Výběr režimu hry.",
                    Description = $"Vybraný režim hry je {storyoptions}!",
                };
                if (storyoptions == "normal") { embed.AddField("Normal mode", "Zvolili jste normální mód. Budete hrát pod svými přezdívkami a každý bude vědět po kom hraje. Nuda"); }
                if (storyoptions == "secret") { embed.AddField("Secret mode", "Každý hráč obdrží náhodnou přezdívku, pořadí se promíchá a budete vědět kulový kdo hraje po kom."); }
                if (storyoptions == "crazy") { embed.AddField("Crazy mode", "Obdobně jako u secret módu, každý hráč obdrží náhodnou přezdívku, akorát zde se pořadí promíchává každé kolo."); }
                embed.AddField("Změna režimu hry", "Režim hry můžete měnit příkazem **~storytype secret/normal/crazy**");
                await ctx.Message.RespondAsync(embed: embed.Build());
                return;
            }
            if (storytype != "normal" & storytype != "secret" & storytype != "crazy")
            {
                var embederror = new DiscordEmbedBuilder(GeneralMessageEmbed)
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "Špatný typ story hry!",
                    Description = "Definujte typ hry jednou z těchto možností: **~storytype normal** **~storytype secret** **~storytype crazy**",
                };
                await ctx.Message.RespondAsync(embed: embederror.Build());
                return;
            }
            else
            {
                storyoptions = storytype;
                var embed = new DiscordEmbedBuilder(GeneralMessageEmbed)
                {
                    Title = "Zvolen jiný typ hry!",
                    Description = $"Vybrali jste si herní mód {storyoptions}!",
                };
                if (storyoptions == "normal") { embed.AddField("Normal mode", "Zvolili jste normální mód. Budete hrát pod svými přezdívkami a každý bude vědět po kom hraje. Nuda"); }
                if (storyoptions == "secret") { embed.AddField("Secret mode", "Každý hráč obdrží náhodnou přezdívku, pořadí se promíchá a budete vědět kulový kdo hraje po kom."); }
                if (storyoptions == "crazy") { embed.AddField("Crazy mode", "Obdobně jako u secret módu, každý hráč obdrží náhodnou přezdívku, akorát zde se pořadí promíchává každé kolo."); }
                await ctx.Message.RespondAsync(embed: embed.Build());
            }
        }

        [Command("storygame")]
        [HiddenAttribute]
        public async Task StoryGame(CommandContext ctx)
        {
            try
            {
                // ############################### HANDLE IF NO PLAYERS ARE DEFINED ###############
                if (playerslist == null)
                {
                    // If there are on players in playerlist, will result in error.
                    var embederror = new DiscordEmbedBuilder(GeneralMessageEmbed)
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "Safra! Nejsou definováni žádní hráči",
                        Description = "Definujte hráče pomocí příkazu **~players**",
                    };
                    await ctx.Message.RespondAsync(embed: embederror.Build());
                    return;
                }

                // ############################### FIND OR CREATE STORY CHANNEL ##################
                // Will try to find story channel or create it and store it in variable
                var allchannels = ctx.Guild.GetChannelsAsync(); // Get all channels in a list
                foreach (var channel in ctx.Guild.Channels)
                {
                    // Cycle through all channels to find one named story
                    if (channel.Type == DSharpPlus.ChannelType.Text)
                    {
                        if (channel.Name == "story")
                        {
                            storychannel = channel;
                        }
                    }
                }
                // If no story channel was found it will try to create one
                if (storychannel == null)
                {
                    try
                    {
                        await ctx.Guild.CreateChannelAsync("story", DSharpPlus.ChannelType.Text);
                        // If there are on players in playerlist, will result in error.
                        var embedcreate = new DiscordEmbedBuilder(GeneralMessageEmbed)
                        {
                            Title = "Nový story channel vytvořen!",
                            Description = "Tomuto serveru chyběl důležitý story channel, tak jsem si jej dovolil vytvořit [GIGGLEFART]",
                        };
                        await ctx.Message.RespondAsync(embed: embedcreate.Build());
                        allchannels = ctx.Guild.GetChannelsAsync(); // Get all channels in a list
                        foreach (var channel in ctx.Guild.Channels)
                        {
                            // Cycle through all channels to find one named story
                            if (channel.Type == DSharpPlus.ChannelType.Text)
                            {
                                if (channel.Name == "story")
                                {
                                    storychannel = channel;
                                }
                            }
                        }
                    }
                    catch
                    {
                        var embederror = new DiscordEmbedBuilder(GeneralMessageEmbed)
                        {
                            Color = new DiscordColor("#FF0000"),
                            Title = "Zatracení NAZI MODS!",
                            Description = "Nenalezl jsem story channel a ani mi nejde vytvořit! Vytvořte nový text channel pojmenovaný **story** nebo mi sakra dejte práva! [ANGRY PSHOUK]",
                        };
                        await ctx.Message.RespondAsync(embed: embederror.Build());
                        return;
                    }
                }
                // ################## IF PREREQ IS OKAY THEN DETERMINE GAME MODE ###############
                // If everything is ready, the game will start!
                var embed = new DiscordEmbedBuilder(GeneralMessageEmbed)
                {
                    Title = "Držte si klobouky, hra začíná!",
                    Description = "Každý z Vás obdrží přezdívku a bot s Vámi bude komunikovat skrze PM.",
                };
                // ##################################################### SECRET MODE START BLOCKE #####################################################################################################
                if (storyoptions == "secret")
                {
                    nicknamelist.Clear();                                                                             // Clears nicknames if some were defined before
                    var rnd = new Random();                                                                           // Creates Random for reorder of list
                    playerslist = playerslist.OrderBy(item => rnd.Next()).ToList<DSharpPlus.Entities.DiscordUser>();  // Reorders list of players randomly
                    embed.AddField("Herní mód **Secret**", "Každý hráč obdrží náhodnou přezdívku, pořadí se promíchá a budete vědět kulový kdo hraje po kom.");
                    // Adds nickname to each user randomly
                    foreach (var player in playerslist)
                    {
                        var r = new Random();                                   // Creates Random for nickname generator
                        var randomLineN = r.Next(0, Globals.nicknames.Length - 1);      // Chooses random line (nickname) from file
                        var nickname = Globals.nicknames[randomLineN];                  // Sets the chosen nickname as temp variable
                        try
                        {
                            var tuple = new Tuple<DiscordUser, string>(player, nickname);   // Adds Discord user and its random nickname into list
                            nicknamelist.Add(tuple);
                        }
                        catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
                    }
                    foreach (var player in nicknamelist) // Creates DM channel with all players
                    {
                        await ctx.Client.CreateDmAsync(player.Item1);
                    }
                    System.Threading.Thread.Sleep(3000); // Sleeps 3 seconds because otherwise code continues faster than the dm channels are created
                    DMChannels = ctx.Client.PrivateChannels; // Stores Private channels in variable
                    foreach (var player in nicknamelist)
                    {
                        var playerDMchannel = DMChannels.Where(m => m.Recipients.First() == player.Item1).First(); // Finds each dm channel by discorduser
                        var nick = player.Item2; // users nickname
                        var embedplayer = new DiscordEmbedBuilder(GeneralMessageEmbed)
                        {
                            Title = $"Hej {nick}! Hra začíná!",
                            Description = $"Přesně tak, na tebe mluvím {nick}! Nyní budeš hrát tuto skvělou hru a budeš se mnou komunikovat skrze tento privátní channel! Vždy tě zavolám jakmile budeš na řadě.",
                        };
                        // Different messages for starting player and rest of the squad.
                        if (player == nicknamelist[currentplayer]) { embedplayer.AddField("Začínáš!", "Jelikož jsi první na řadě, tvůj první příkaz **~s** definuje **Název** celého příběhu, tak se snaž!"); }
                        else { embedplayer.AddField("Čekej!", "Jakmile hráč před tebou zašle větu, tak ti ji pošeptám do *ouška* a ty můžeš následně navázat svou dokonalou větou!"); }
                        embedplayer.AddField("Příkazy", "Abys zaslal novou větu příběhu, použij příkaz **~s** [Věta]\nPokud bys již chtěl ukončit příběh, použij příkaz **~end** [Věta]");
                        await playerDMchannel.SendMessageAsync(embed: embedplayer.Build());
                    }
                }
                // ##################################################### NORMAL MODE START BLOCKE #####################################################################################################
                if (storyoptions == "normal")
                {
                    embed.AddField("Herní mód **Normal**", "Normální mód používá běžné přezdívky a pořadí se nemění. Nuda");
                }
                // ##################################################### CRAZY MODE START BLOCKE ######################################################################################################
                if (storyoptions == "crazy")
                {
                    embed.AddField("Herní mód **Crazy**", "Obdobně jako u secret módu, každý hráč obdrží náhodnou přezdívku, akorát zde se pořadí promíchává každé kolo.");
                }
                // ##################################################### READY SET AN FUCKING GO! ####################################################################################################
                // ###################################################################################################################################################################################
                await ctx.Message.RespondAsync(embed: embed.Build());
                var status = $"{round}. - {nicknamelist[currentplayer].Item2}";
                var playerliststring = "Autoři: ";
                foreach (var player in nicknamelist) { playerliststring += $"[{player.Item1.Username}]{player.Item2} "; }
                StoryEmbed.WithAuthor(playerliststring);
                StoryEmbed.WithFooter("Příběh může být jako vždy nalezen na https://example.com/");
                await ctx.Client.UpdateStatusAsync(new DiscordGame($"{status}"));
            }
            catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
        }

        [Command("start")]
        [Description("Odstartuje vybranou hru.")]
        public async Task StartGame(CommandContext ctx)
        {
            if (gametype == "story") { await StoryGame(ctx); }
        }

        [Command("s")]
        [Description("Slouží k zaslání nové věty do příběhu.")]
        public async Task StorySentence(CommandContext ctx)
        {
            // #####################    Checks if user invoked this command in private DM channel  #####################
            if (ctx.Channel.Type != DSharpPlus.ChannelType.Private) { await ctx.RespondAsync("Si myslíš že jsi rebel nebo co? Tohle není privátní kanel! [ANGRYFART]"); return; }

            // #####################   Depending on game mode command executes differently  #####################
            if (storyoptions == "secret")
            {
                // #####################   Checks if user who invoked this command is supposed to play #####################
                if (ctx.User != nicknamelist[currentplayer].Item1) { await ctx.RespondAsync($"Aktuálně bohužel nejsi na řadě, právě by měl hrát {nicknamelist[currentplayer].Item2}"); return; }
                // #####################   Main command which adds new sentence to story and notifies another user  #####################                
                // ########   First blocke is for the first user who defines story title ###########
                if (firstsentence == true) // Handles Title and stuff
                {
                    StoryEmbed.WithTitle(ctx.Message.Content.Substring(3));
                    firstsentence = false;
                    webStoryTitle = Regex.Replace(ctx.Message.Content.Substring(3),@"\|+", "");
                    webStoryDate = DateTime.Now.ToString("HH:mm dd-MM-yyyy");
                    var playerDMchannel = DMChannels.Where(m => m.Recipients.First() == nicknamelist[currentplayer + 1].Item1).First(); // Finds required DM channel by DiscordUser object
                    var embedtitle = new DiscordEmbedBuilder(GeneralMessageEmbed)
                    {
                        Title = $"{webStoryTitle}",
                        Description = $"Tak už je to tady! {nicknamelist[currentplayer].Item2} již zvolil název příběhu!",
                    };
                    embedtitle.AddField("Rozbal to!", $"Započni epický příběh související s názvem příběhu **{webStoryTitle}** příkazem **~s** [Věta], následně vyčkej až opět budeš na řadě!");
                    await playerDMchannel.SendMessageAsync(embed: embedtitle.Build());
                    ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot-story", $"[{nicknamelist[currentplayer].Item1.Username}]{nicknamelist[currentplayer].Item2} - Set story title to {webStoryTitle}", DateTime.Now);
                    currentplayer++;
                }
                // ########   Second blocke is for regular sentences    ###########
                else
                {
                    var webStorySentence = Regex.Replace(ctx.Message.Content.Substring(3),@"\|+", ""); // Gets current sentence
                    var currentPlayerNickname = nicknamelist[currentplayer].Item2;
                    var currentPlayerUsername = nicknamelist[currentplayer].Item1.Username;
                    webStoryContent.Add(new Tuple<string, string>($"[{nicknamelist[currentplayer].Item1.Username}]{currentPlayerNickname}", webStorySentence));
                    // If current player is the very last member of list, then the new recipient is player[0] otherwise player++
                    if (currentplayer == nicknamelist.Count - 1) { currentplayer = 0; round++; }
                    else { currentplayer++; }
                    var playerDMchannel = DMChannels.Where(m => m.Recipients.First() == nicknamelist[currentplayer].Item1).First(); // Finds required DM channel by DiscordUser object
                    var embedstory = new DiscordEmbedBuilder(GeneralMessageEmbed)
                    {
                        Title = $"Jsi na řadě {nicknamelist[currentplayer].Item2}!",
                    };
                    embedstory.AddField($"Předchozí věta od hráče {currentPlayerNickname}", $"{webStorySentence}");
                    await playerDMchannel.SendMessageAsync(embed: embedstory.Build());
                    StoryEmbed.AddField($"{$"{currentPlayerNickname} "}", ctx.Message.Content.Substring(3));
                    ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot-story", $"[{currentPlayerUsername}]{currentPlayerNickname} - {webStorySentence}", DateTime.Now);
                }
                var status = $"{round}. - {nicknamelist[currentplayer].Item2}";
                await ctx.Client.UpdateStatusAsync(new DiscordGame($"{status}"));
            }
            if (storyoptions == "normal")
            {
                // #####################   Checks if user who invoked this command is supposed to play #####################
                if (ctx.User != playerslist[currentplayer]) { await ctx.RespondAsync("Si myslíš že jsi rebel nebo co? Tohle není privátní kanel! []"); }
            }
            if (storyoptions == "crazy") { }

        }

        [Command("end")]
        [Description("Ukončí vybranou hru")]
        public async Task EndGame(CommandContext ctx)
        {
            string endingplayer = nicknamelist.Where(m => m.Item1 == ctx.User).First().ToString(); // Finds each dm channel by discorduser
            if (ctx.User != nicknamelist[currentplayer].Item1)
            {
                var embedwarn = new DiscordEmbedBuilder(GeneralMessageEmbed)
                {
                    Color = new DiscordColor("#FFFF00"),
                    Title = "Právě nejsi na řadě!",
                    Description = "Jak můžeš vědět, že se má příběh ukončit?! Pokud se jedná o krizovku, kdy drzý drzoun utekl od svého zažízení, napiš pro potvrzení zprávu **reallyend**.",
                };
                await ctx.Message.RespondAsync(embed: embedwarn.Build());
                var interactivity = ctx.Client.GetInteractivityModule();
                var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == ctx.User.Id && xm.Content.ToLower() == "reallyend", TimeSpan.FromMinutes(1));
                if (msg != null)
                {
                    await storychannel.SendMessageAsync(embed: StoryEmbed.Build());
                    // Send shit to website etc
                    await PostStory(ctx);
                    foreach (var player in nicknamelist)
                    {
                        var playerDMchannel = DMChannels.Where(m => m.Recipients.First() == player.Item1).First(); // Finds each dm channel by discorduser
                        var nick = player.Item2; // users nickname
                        var embedplayer = new DiscordEmbedBuilder(GeneralMessageEmbed)
                        {
                            Title = $"Hej {nick}! Hra skončila!",
                            Description = $"Věř tomu nebo ne, hráč {endingplayer} vše skončil. Příběh nalezneš na https://nase.example.com/stories/cs/blog/{webStoryPageName}",
                        };
                        // Different messages for starting player and rest of the squad.                
                        await playerDMchannel.SendMessageAsync(embed: embedplayer.Build());
                    }
                    ctx.Client.DebugLogger.LogMessage(level: LogLevel.Info, application: "prdelbot-story", message: $"[{ctx.Message.Author.Username}] forced end of story.", timestamp: DateTime.Now);
                }
            }
            else {
                var webStorySentence = ctx.Message.Content.Substring(5); // Gets current sentence
                var currentPlayerNickname = nicknamelist[currentplayer].Item2;
                webStoryContent.Add(new Tuple<string, string>($"[{nicknamelist[currentplayer].Item1.Username}]{currentPlayerNickname}", webStorySentence));
                StoryEmbed.AddField($"{$"{currentPlayerNickname} "}", ctx.Message.Content.Substring(5));
                await storychannel.SendMessageAsync(embed: StoryEmbed.Build());
                // Send shit to website etc
                await PostStory(ctx);
                foreach (var player in nicknamelist)
                {
                    var playerDMchannel = DMChannels.Where(m => m.Recipients.First() == player.Item1).First(); // Finds each dm channel by discorduser
                    var nick = player.Item2; // users nickname
                    var embedplayer = new DiscordEmbedBuilder(GeneralMessageEmbed)
                    {
                        Title = $"Hej {nick}! Hra skončila!",
                        Description = $"Věř tomu nebo ne, hráč {endingplayer} vše skončil. Příběh nalezneš na https://nase.example.com/stories/cs/blog/{webStoryPageName}",
                    };
                    // Different messages for starting player and rest of the squad.                
                    await playerDMchannel.SendMessageAsync(embed: embedplayer.Build());
                }
                ctx.Client.DebugLogger.LogMessage(level: LogLevel.Info, application: "prdelbot-story", message: $"[{nicknamelist[currentplayer].Item1.Username}]{currentPlayerNickname} - {webStorySentence}", timestamp: DateTime.Now);
            }


            // Dispose all variables
            round = 1;
            currentplayer = 0;
            firstsentence = true;
            webStoryTitle = null;
            webStoryDate = null;
            webStoryContent.Clear();
            StoryEmbed.ClearFields();
            await ctx.Client.UpdateStatusAsync(new DiscordGame("with masself"));
        }

        [Command("poststory")]
        [HiddenAttribute]
        [Description("Odešle příběh na web - Příkaz end jej automaticky vyvolá.")]
        public async Task PostStory(CommandContext ctx)
        {
            // First we normalize the story title for page and folder naming
            webStoryPageName = webStoryTitle.ToLower().Trim(); // Convert all characters to lower and trim start and end spaces
            webStoryPageName = RemoveDiacritics(webStoryPageName); // Calls RemoveDiacritics method whici removes diacritics (Who would guess)
            webStoryPageName = Regex.Replace(webStoryPageName, "[^0-9a-z- ]+", ""); // Removes non standard characters
            webStoryPageName = Regex.Replace(webStoryPageName, @"\s+", "-"); // Replace space(s) to dashes
            webStoryPageName = Regex.Replace(webStoryPageName, @"-+", "-"); // Replace multiple dashes to single dash

            // Then we create folder with corresponding name and number - We also increase the current number
            string ciselnikPath = System.IO.Path.Combine(Globals.cfgjson.WebPath, "ciselnik");
            int currentPostNumber = Int32.Parse(System.IO.File.ReadLines(ciselnikPath).Take(1).First());
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(ciselnikPath, false))
            {
                file.WriteLine((currentPostNumber+1).ToString("000"));
            }
            string newPostPath = System.IO.Path.Combine(Globals.cfgjson.WebPath, (currentPostNumber.ToString("000")) + "." + webStoryPageName);
            System.IO.Directory.CreateDirectory(newPostPath);

            // Then we create the post file
            string playerliststring = "";
            foreach (var player in playerslist) { playerliststring += (player.Username + ", "); }
            playerliststring = playerliststring.Substring(0, playerliststring.Length - 2);
            string newPostFile = System.IO.Path.Combine(newPostPath, "post.cs.md");
            var postContent = String.Format("---\ntitle: '{0}'\ndate: '{1}'\nheadline: '{2}'\n---\n| Autor | Příběh |\n| --- | --- |\n", webStoryTitle, webStoryDate,playerliststring);
            foreach (var content in webStoryContent)
            {
                postContent += $"| {content.Item1} | {content.Item2} | \n";
            }
            using (FileStream fs = File.Create(newPostFile))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes(postContent);
                fs.Write(info, 0, info.Length);
            }
            await ctx.RespondAsync($"Příběh je dostupný na https://nase.example.com/stories/cs/blog/{webStoryPageName}");
        }

        [Command("reset")]
        [Description("Resetuje většinu proměnných")]
        public async Task ResetVar(CommandContext ctx)
        {

            try { playerslist.Clear(); } catch { }
            try { nicknamelist.Clear(); } catch { }
            try { storychannel = null; } catch { }
            try { gametype = "story"; } catch { }
            try { storyoptions = "secret"; } catch { }
            try { round = 1; } catch { }
            try { currentplayer = 0; } catch { }
            try { firstsentence = true; } catch { }
            try { webStoryTitle = null; } catch { }
            try { webStoryDate = null; } catch { }
            try { webStoryContent.Clear(); } catch { }
            try { StoryEmbed.ClearFields(); } catch { }
            await ctx.Client.UpdateStatusAsync(new DiscordGame("with masself"));

            await ctx.RespondAsync("Proměnné bota pročištěny!");
        }

        [Command("waitfortyping"), Description("Waits for a typing indicator.")]
        public async Task WaitForTyping(CommandContext ctx)
        {
            // first retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivityModule();

            // then wait for author's typing
            var chn = await interactivity.WaitForTypingChannelAsync(ctx.User, TimeSpan.FromSeconds(60));
            if (chn != null)
            {
                // got 'em
                await ctx.RespondAsync($"{ctx.User.Mention}, you typed in {chn.Channel.Mention}!");
            }
            else
            {
                await ctx.RespondAsync("*yawn*");
            }
        }

        [Command("mkdir"), Description("Creates folder and file to publish story")]
        public async Task Mkdir(CommandContext ctx)
        {
            // folder where sites are /var/www/nase.example.com/public_html/stories/user/pages
            string defaultfolder = Globals.cfgjson.DataPath;
            // name of the folder should be ascending number.name of the story
            string pathString = System.IO.Path.Combine(defaultfolder, "CisloNazevStory");
            // creates folder (eventually should probably copy from template?)
            System.IO.Directory.CreateDirectory(pathString);
            // creates file with content
            string fileName = "nazevstory.md";
            string filePathString = System.IO.Path.Combine(pathString, fileName);
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(filePathString, true))
            {
                file.WriteLine("Fourth line");
            }
            await ctx.RespondAsync("Created");
        }

        [Command("dumpstory"), Description("Dumps in emergency story to file")]
        public async Task DumpStory(CommandContext ctx)
        {
            var DumpName = "Story-Dump" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            var Filepath = @"/var/app";
            var path = Path.Combine(Filepath,DumpName);
            var story = $"| Autor | Příběh |\n";
            story += "| --- | --- |\n";
            foreach (var content in webStoryContent)
            {                
                story += $"| {content.Item1} | {content.Item2} | \n";
            }
            using (FileStream fs = File.Create(path))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes(story);
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }
            await ctx.RespondAsync($"Story was dumped into {path}");
            ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot", $"Contents of story variable was dumped to {path}", DateTime.Now);
        }


        [Command("cc"), Description("Creates folder and file to publish story")]
        public async Task CringeCity(CommandContext ctx)
        {
            await ctx.RespondAsync(":regional_indicator_c: :regional_indicator_r: :regional_indicator_i: :regional_indicator_n: :regional_indicator_g: :regional_indicator_e: :regional_indicator_c: :regional_indicator_i: :regional_indicator_t: :regional_indicator_y:");
            //await ctx.RespondAsync(Program.gameOngoing.ToString());
        }

        [Command("serverstatus"), Description("Checks Minecraft server status")]
        [Aliases("mc", "minecraftstatus", "mcstatus","status")]
        public async Task MinecraftStatus(CommandContext ctx)
        {
            //var values = new Dictionary<string, string>
            //{
            //   { "ip", "example.com" }
            //};
            //var httpcontent = new FormUrlEncodedContent(values);
            //var httpresponse = await Globals.httpclient.PostAsync("https://mcapi.us/server/status?ip=example.com", null);
            //var responseString = await httpresponse.Content.ReadAsStringAsync();
            //https://mcapi.us/server/status?ip=example.com
            //await ctx.RespondAsync(":regional_indicator_c: :regional_indicator_r: :regional_indicator_i: :regional_indicator_n: :regional_indicator_g: :regional_indicator_e: :regional_indicator_c: :regional_indicator_i: :regional_indicator_t: :regional_indicator_y:");

            string url = "https://mcapi.us/server/status?ip=example.com";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //Stream resStream = response.GetResponseStream();
            string responseText;
            var encoding = ASCIIEncoding.ASCII;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                responseText = reader.ReadToEnd();
            }
            await ctx.RespondAsync(responseText);
        }

        // Method which normalizes string for url
        public static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        // Minecraft server information
        [Command("minecraft")]
        public async Task MinecraftServerInfo(CommandContext ctx)
        {
            //ctx.Client.DebugLogger.LogMessage(DSharpPlus.LogLevel.Info, "prdelbot", "Saying hi", DateTime.Now);
            var embed = new DiscordEmbedBuilder
            {
                Title = "Minecraft Prdel Server",
                ThumbnailUrl = "https://example.com/ass_icon.png",
                ImageUrl = "https://example.com/ftb_direwolf_discord_cover.png",
                Url = "https://youtu.be/CR23GraAjis",
                Description = "Direwolf20 modpack v2.5.0, Minecraft v.1.12.2",
            };

            embed.AddField("Detaily serveru", "Název serveru: example.com\nModPack: FTB Presents Direwolf20\nVerze modpacku: v2.5.0\nVerze Minecraftu: 1.12.2\nRcon konzole: http://gaming.example.com/mc/", false);
            embed.AddField("Návod na připojení", "1. Stáhněte si [Twitch App](https://www.twitch.tv/downloads)\n2. Nainstalujte a přihlaste se do Twitch App\n3. V horní liště zvolte **Modifikace**\n4. Zvolte hru **Minecraft** a stiskněte **Instalovat**\n5. Přejděte na záložku **Prohlížet modpacky FTB**\n6. Nainstalujte **FTB Presents Direwolf20 1.12**\n7. Spusťte modpack a připojte se na **example.com**", false);
            embed.AddField("Pro naprosté Minecraft nooby", "Pokud nemáte ani Javu a ani Minecraft, případně nemáte vůbec páru co máte dělat s Twitch App, pak shlédněte toto video, ve kterém se vše zprovozní od nuly.\n\n[**Nejlepší video návod široko daleko**](https://youtu.be/CR23GraAjis)\n\nUžitečné odkazy k tomuto tématu:\n[Java ke stažení](https://www.java.com/download/)\n[Minecraft ke stažení](https://example.com/downloads/)", false);

            await ctx.Message.RespondAsync(embed: embed.Build());
        }
        
    } // End of MyCommands class


}