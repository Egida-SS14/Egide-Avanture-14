using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace Egide.Bot
{
    class Program
    {
        private const ulong SponsorT1RoleId = 1367560820250382506;
        private const ulong SponsorT2RoleId = 1463938183581532201;
        private const ulong AuthorizedRoleId = 1366301442067140628;
        private const ulong AgeVerifiedRoleId = 1533145284316627095;

        private DiscordSocketClient _client = null!;
        private InteractionService _interactions = null!;
        private BotConfig _config = null!;

        private Timer? _syncTimer;

        static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunAsync();
        }

        public async Task RunAsync()
        {
            _config = LoadConfig();
            BotLoggerSystem.SetDebugMode(_config.DebugMode);

            var socketConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(socketConfig);
            _interactions = new InteractionService(_client.Rest);
            await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), null);

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteractionAsync;
            _client.JoinedGuild += JoinedGuildAsync;
            _client.GuildAvailable += GuildAvailableAsync;

            await _client.LoginAsync(TokenType.Bot, _config.BotToken);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private BotConfig LoadConfig()
        {
            var content = File.ReadAllText("config.yml");
            var stream = new StringReader(content);
            var yaml = new YamlStream();
            yaml.Load(stream);

            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            return new BotConfig
            {
                BotToken = mapping["bot_token"]?.ToString() ?? "",
                GuildId = ulong.Parse(mapping["guild_id"]?.ToString() ?? "0"),
                DebugMode = mapping["debug_mode"]?.ToString()?.ToLower() == "true",
                DatabaseEngine = mapping["database_engine"]?.ToString() ?? "sqlite",
                DatabaseSqlitePath = mapping["database_sqlite_path"]?.ToString() ?? "preferences.db",
                DatabasePgConnectionString = mapping["database_pg_connection_string"]?.ToString() ?? ""
            };
        }

        private Task LogAsync(LogMessage log)
        {
            BotLoggerSystem.Log(LogType.INFO, log.Message);
            if (log.Exception != null)
                BotLoggerSystem.Log(LogType.CRIT, log.Exception.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            BotLoggerSystem.Log(LogType.INFO, "Инициализация...");
            SqlSystem.InitDatabase(_config);

            var guild = _client.GetGuild(_config.GuildId);
            if (guild != null)
            {
                await _interactions.RegisterCommandsToGuildAsync(guild.Id);
                BotLoggerSystem.Log(LogType.INFO, $"Синхронизированы команды для сервера {guild.Name}");
                await SyncGuildRoles(guild);
            }
            else
            {
                BotLoggerSystem.Log(LogType.WARN, $"Сервер {_config.GuildId} не найден");
            }

            _syncTimer = new Timer(async _ =>
            {
                var g = _client.GetGuild(_config.GuildId);
                if (g != null)
                    await SyncGuildRoles(g);
                else
                    BotLoggerSystem.Log(LogType.WARN, $"Сервер {_config.GuildId} не найден");
            }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            BotLoggerSystem.Log(LogType.INFO, "Бот готов к работе!");
        }

        private async Task JoinedGuildAsync(SocketGuild guild)
        {
            await _interactions.RegisterCommandsToGuildAsync(guild.Id);
        }

        private async Task GuildAvailableAsync(SocketGuild guild)
        {
            if (guild.Id == _config.GuildId)
            {
                await _interactions.RegisterCommandsToGuildAsync(guild.Id);
            }
        }

        private async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            var result = await _interactions.ExecuteCommandAsync(context, null);

            if (!result.IsSuccess)
                BotLoggerSystem.Log(LogType.WARN, $"Command error: {result.Error} - {result.ErrorReason}");
        }

        private async Task SyncGuildRoles(SocketGuild guild)
        {
            BotLoggerSystem.Log(LogType.INFO, $"Скан ролей на сервере {guild.Name}...");
            var t1Role = guild.GetRole(SponsorT1RoleId);
            var t2Role = guild.GetRole(SponsorT2RoleId);
            var authorizedRole = guild.GetRole(AuthorizedRoleId);

            foreach (var member in guild.Users)
            {
                var ckey = SqlSystem.GetPlayerCkey(member.Id);
                if (ckey == null)
                    continue;

                if (authorizedRole != null && !member.Roles.Contains(authorizedRole))
                {
                    await member.AddRoleAsync(authorizedRole);
                    BotLoggerSystem.Log(LogType.DEBG, $"Выдана роль Authorized пользователю {member.Id}");
                }

                string? newSponsor;
                if (t2Role != null && member.Roles.Contains(t2Role))
                    newSponsor = "T2";
                else if (t1Role != null && member.Roles.Contains(t1Role))
                    newSponsor = "T1";
                else
                    newSponsor = null;

                var currentSponsor = SqlSystem.GetPlayerSponsorTier(member.Id);
                if (currentSponsor != newSponsor)
                {
                    SqlSystem.UpdatePlayerSponsor(member.Id, newSponsor);
                    BotLoggerSystem.Log(LogType.DEBG, $"Спонсор {member.Id}: {currentSponsor} -> {newSponsor}");
                }
            }

            BotLoggerSystem.Log(LogType.INFO, "Скан ролей завершён");
        }
    }
}
