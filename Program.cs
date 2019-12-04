using System;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Net.Http;

namespace prdelbot
{
    // Contains all global variables variables
    public static class Globals
    {
        public static String json = System.IO.File.ReadAllText("config.json"); // Discord bot variables stored in json file - reads into string
        public static ConfigJson cfgjson = JsonConvert.DeserializeObject<ConfigJson>(json); // Serialize string into variables - uses struct in the bottom of Program.cs file
        public static string[] borgsoul = System.IO.File.ReadAllLines("BorgSoul");
        public static string[] egyhokecy = System.IO.File.ReadAllLines("EgyhoKecy");
	    public static string[] xinmeciar = System.IO.File.ReadAllLines("XinMeciar");
	    public static string[] catmehs = System.IO.File.ReadAllLines("CatMehs");
        public static string[] jolanda = System.IO.File.ReadAllLines("JolandaBeCiganka");
        public static string[] nicknames = System.IO.File.ReadAllLines("Nicknames");
        public static readonly HttpClient httpclient = new HttpClient();
    }

    public class Program
    {

        // ######################################################################### Variables  ###########################################################################################

        public static bool gameOngoing = false;
        // Create new fields for Discord client and command module and interactivity module
        public static DiscordClient discord;
        static CommandsNextModule commands;
        static InteractivityModule interactivity;

        // Main which calls MainAsync
        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        // MainAsync which is heart of the program
        public async Task MainAsync(string[] args)
        {          
            // Creates new discord client configuration - contains bot token and defines console logging
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Globals.cfgjson.Token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Info,                
            });                        
            // Creates commands module configuration - which prefix is used to command bot
            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = Globals.cfgjson.CommandPrefix
            });
            commands.RegisterCommands<MyCommands>(); // initialize command module within main program

            // Creates Interactivity module? I guess - not working as in docs
            interactivity = discord.UseInteractivity(new InteractivityConfiguration
                {
                }
            );

            // ??? KDE JE AUTO
            discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().StartsWith("???"))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Where the hell is the car???", DateTime.Now);
                    await e.Message.RespondAsync("KDE JE AUTO");
                }
            };

            // Dad backfired
            discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().Contains(", i'm dad"))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Dad tries to dad someone, performing vendada.", DateTime.Now);
                    await e.Message.RespondAsync("Hi dad, i'm prdel!");
                }
            };

            // Borgs soul which speaks to us even in afterlife
            discord.MessageCreated += async e =>
            {                
                if ((e.Message.Content.ToLower().Contains("borg")) | (e.Message.Content.ToLower().Contains("brgo")) | (e.Message.Content.ToLower().Contains("brok"))) 
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Someone woke up our Borg who says something just to pretend being awake whole time.", DateTime.Now);
                    var r = new Random();
                    var randomLineNumber = r.Next(0, Globals.borgsoul.Length - 1);
                    var borgline = Globals.borgsoul[randomLineNumber];

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "„" + borgline + "“",
                        ThumbnailUrl = "https://prdelka.eu/borg.jpg",
                    };
                    embed.WithAuthor("Borgoroth", "https://www.facebook.com/vasepravejmeno", "https://prdelka.eu/borg.jpg");
                    await e.Message.RespondAsync(embed: embed.Build());
                }
            };

	        // Xin soul which speaks to us even in afterlife
            discord.MessageCreated += async e =>
            {
                if ((e.Message.Content.ToLower().Contains("xin")) | (e.Message.Content.ToLower().Contains("milan")) | (e.Message.Content.ToLower().Contains("katana")))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Someone wants to fight mighty master sword Xin, get ready to fight.", DateTime.Now);
                    var r = new Random();
                    var randomLineNumber = r.Next(0, Globals.xinmeciar.Length - 1);
                    var xinline = Globals.xinmeciar[randomLineNumber];

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "„" + xinline + "“",
                        ThumbnailUrl = "https://prdelka.eu/xin.jpg",
                    };

		    // in case of katana
		    if (e.Message.Content.ToLower().Contains("katana"))
                    {
                        xinline = xinline.Replace("😄", ":dagger:");
                        xinline = xinline.Replace("😃", ":dagger:");
                        xinline = xinline.Replace("😭", ":dagger:");
                        embed.Title = "„" + xinline + " :dagger:“";
                    }

                    embed.WithAuthor("Xin", "https://www.facebook.com/Xin.andor", "https://prdelka.eu/xin.jpg");
                    await e.Message.RespondAsync(embed: embed.Build());
                }
            };

	        // Cat lines which are pretty bad even for her weak brain
            discord.MessageCreated += async e =>
            {
                if ((e.Message.Content.ToLower().Contains("cat")) | (e.Message.Content.ToLower().Contains("sparepartscat")) | (e.Message.Content.ToLower().Contains("ket")) | (e.Message.Content.ToLower().Contains("catori")) | (e.Message.Content.ToLower().Contains("katori")))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Someone wants to hear cat meow.", DateTime.Now);
                    var r = new Random();
                    var randomLineNumber = r.Next(0, Globals.catmehs.Length - 1);
                    var catline = Globals.catmehs[randomLineNumber];

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "„" + catline + "“",
                        ThumbnailUrl = "https://prdelka.eu/cat.jpg",
                    };
                    embed.WithAuthor("Cat", "https://www.twitch.tv/sparepartscat", "https://prdelka.eu/cat.jpg");
                    await e.Message.RespondAsync(embed: embed.Build());
                }
            };

            // Jolanda lines which are pretty awesome - smarter than Cat
            discord.MessageCreated += async e =>
            {
                if ((e.Message.Content.ToLower().Contains("jolanda")) | (e.Message.Content.ToLower().Contains("jolana")) | (e.Message.Content.ToLower().Contains("cigánka")) | (e.Message.Content.ToLower().Contains("cikánka")) | (e.Message.Content.ToLower().Contains("cikanka")))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Someone wants to know their fate.", DateTime.Now);
                    var r = new Random();
                    var randomLineNumber = r.Next(0, Globals.jolanda.Length - 1);
                    var jolandaline = Globals.jolanda[randomLineNumber];

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "„" + jolandaline + "“",
                        ThumbnailUrl = "https://prdelka.eu/jolanda.jpg",
                    };
                    embed.WithAuthor("Jolanda", "https://www.youtube.com/watch?v=GlL5a2nJ_YQ", "https://prdelka.eu/jolanda.jpg");
                    await e.Message.RespondAsync(embed: embed.Build());
                }
            };

            // Egy soul which speaks to us even in ragin' afterlife
            discord.MessageCreated += async e =>
            {
                if ((e.Message.Content.ToLower().Contains("egy")) | (e.Message.Content.ToLower().Contains("egouš")) | (e.Message.Content.ToLower().Contains("housle")) | (e.Message.Content.ToLower().Contains("shrek")) | (e.Message.Content.ToLower().Contains("nasrat")) | (e.Message.Content.ToLower().Contains("jaroušek")) | (e.Message.Content.ToLower().Contains("jaroslav")) | (e.Message.Content.ToLower().Contains("jarda")) | (e.Message.Content.ToLower().Contains("jaromír")))
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Someone woke up our Egy who says something because he always has to say something.", DateTime.Now);
                    var r = new Random();
                    var randomLineNumber = r.Next(0, Globals.egyhokecy.Length - 1);
                    var egyline = Globals.egyhokecy[randomLineNumber];

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "„" + egyline + "“",
                        ThumbnailUrl = "https://prdelka.eu/shreggy.jpg",
                    };
                    embed.WithAuthor("Egy", "https://cs-cz.facebook.com/jarousekegy.egyed", "https://prdelka.eu/shreggy.jpg");

                    // In case Shregy is called Heyo
                    if (e.Message.Content.ToLower().Contains("shregy") | e.Message.Content.ToLower().Contains("shrek") | e.Message.Content.ToLower().Contains("shreggy"))
                    {
                        embed.WithAuthor("Shregy", "https://youtu.be/ctG8MWx_gYg", "https://prdelka.eu/shreggy.jpg");
                        embed.ThumbnailUrl = "https://prdelka.eu/shreggy.jpg";
                    }

                    // In case Jaroslav is called >:^)
                    if (e.Message.Content.ToLower().Contains("jaroslav") | e.Message.Content.ToLower().Contains("jaroušek") | e.Message.Content.ToLower().Contains("jarda"))
                    {
                        embed.Title = "„" + egyline + "\na nejsem Jaroslav 😉 ale dobrej pokus“";
                    }

                    // In case of Housle
                    if (e.Message.Content.ToLower().Contains("housle"))
                    {
                        egyline = egyline.Replace("😄", ":violin:");
                        egyline = egyline.Replace("😃", ":violin:");
                        egyline = egyline.Replace("😭", ":violin:");
                        embed.Title = "„" + egyline + " :violin:“";
                    }
                    await e.Message.RespondAsync(embed: embed.Build());

                }
            };

            // #################################################### NSA spying Q.Q ###################################################################################
            // Logs every message to log file and downloads attachments and spies the shit out of everything because searching history in discord sucks ass
                    discord.MessageCreated += async e =>
            {
                string MessageDate = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                string OriginName = " ";                                                        // Guild name is used for log name or user name if private channel
                string Filepath = System.IO.Path.Combine(Globals.cfgjson.DataPath, "chat-log"); // Folder for storing chat logs                
                if (e.Channel.IsPrivate)                                                        // Path to log files - if DM detected then changes to subfolder private
                {
                    if (e.Author.IsBot) {return; }                          // Ignores if DM comes from bot - generally from itself
                    OriginName = $"{ e.Author.Username}.DM";                // Log file name uses Username
                    Filepath = System.IO.Path.Combine(Filepath,"private");  // Changes to chat-log/private
                }
                else
                {
                    OriginName = e.Guild.Name;                              // Otherwise log file name is guild name
                }                                         
                var LogPath = Path.Combine(Filepath, OriginName);           // Combines Path to Log and Log Name thus creating absolute path to result file    
                
                if (e.Message.Attachments.Count > 0)                                                            // If message contains attachment download it *yum*
                {
                    var attachmentsPath = System.IO.Path.Combine(Globals.cfgjson.DataPath, "attachments");      // Path to attachments folder
                    foreach (var attachment in e.Message.Attachments)                                           // Generally message has only one attachment, but in case it doesn't, it cycles them all
                    {                        
                        using (var client = new WebClient())                                                    // Uses Net.WebClient to download file from attachment url
                        {
                            var attachmentFileName = $"{MessageDate}_{e.Author.Username}-{attachment.FileName}";// Creates file name by Time - User - FileName
                            var attachmentFile = System.IO.Path.Combine(attachmentsPath, attachmentFileName);   // Combines Path to attachments folder and file name
                            client.DownloadFile(attachment.Url, attachmentFile);                                // Downloads file
                        }
                    }
                }

                string Message = "";
                if (e.Message.Attachments.Count > 0)                                                                                                // Logs DateTime-User-Content of msg separated by TAB
                {
                    Message = $"[{MessageDate}]\t[{e.Author.Username}]\t{e.Message.Content.ToString()}\tAttachments:{e.Message.Attachments.Count}"; // If has attachments then it logs count         
                }
                else
                {
                    Message = $"[{MessageDate}]\t[{e.Author.Username}]\t{e.Message.Content.ToString()}";                                            // If no attachments it doesnt count them
                }

                if (File.Exists(LogPath))                                  // If Log file exists, appends new line
                {
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(LogPath, true))
                        {
                            file.WriteLine(Message);
                        }
                }
                else                                                      // If log file doesn't exist it creates one and appends new line - also warns in syslog
                {
                    e.Client.DebugLogger.LogMessage(LogLevel.Warning, "prdelbot", $"Couldn't find log file for guild/user {OriginName}, creating one!", DateTime.Now); 
                    using (FileStream fs = File.Create(LogPath))
                    {
                    }
                    using (System.IO.StreamWriter file =
                        new System.IO.StreamWriter(LogPath, true))
                    {
                        file.WriteLine(Message);
                    }
                }
                await Task.CompletedTask;
            };
            // #################################################### end of NSA spying Q.Q ##############################################################################

            // ################################### Event handler - when bot enters Ready state it sets status and sends info to syslog #################################
            discord.Ready += Client_ReadyAsync;
            Task Client_ReadyAsync(ReadyEventArgs e)
            {
                e.Client.DebugLogger.LogMessage(LogLevel.Info, "prdelbot", "Booty-ing up the prdelbot service right now! Let's shake dat ass!", DateTime.Now); // Syslog info
                discord.UpdateStatusAsync(new DiscordGame("with masself"));                                                                                    // Bots status
                return Task.CompletedTask;   
            }
            // ###### End of Event handler

            // ###### Connects to Discord API ######
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }

    // #################################################################################################################################################################
    // ####################################################### STRUCTURE - CONVERTS JSON TO CONFIG #####################################################################
    // #################################################################################################################################################################
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }

        [JsonProperty("datapath")]
        public string DataPath { get; private set; }

        [JsonProperty("webpath")]
        public string WebPath { get; private set; }
    }
    // #################################################################################################################################################################
    // #######################################################         END OF JSON CONFIG          #####################################################################
    // #################################################################################################################################################################
}
