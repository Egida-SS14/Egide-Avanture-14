namespace Egide.Bot
{
    public class BotConfig
    {
        public string BotToken { get; set; } = "";
        public ulong GuildId { get; set; }
        public bool DebugMode { get; set; }

        public string DatabaseEngine { get; set; } = "sqlite";

        public string DatabaseSqlitePath { get; set; } = "preferences.db";
        
        public string DatabasePgConnectionString { get; set; } = "";
    }
}
