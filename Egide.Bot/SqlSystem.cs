using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Egide.Bot
{
    public static class SqlSystem
    {
        private const string DbFile = "players.db";

        public static void InitDatabase(string name, bool hard)
        {
            BotLoggerSystem.Log(LogType.INFO, "Начало инициализации базы данных...");
            BotLoggerSystem.Log(LogType.DEBG, "Подключение sqlite3");

            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();

            BotLoggerSystem.Log(LogType.DEBG, "Инициализация базы данных...");
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS players (
                    discordid INTEGER PRIMARY KEY,
                    ckey TEXT,
                    uid TEXT,
                    sponsor TEXT,
                    sponsor_expires TIMESTAMP,
                    age_verified BOOLEAN
                )";
            cmd.ExecuteNonQuery();

            BotLoggerSystem.Log(LogType.INFO, "База данных успешно инициализирована");
        }

        public static void AddPlayer(ulong discordId, string ckey)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Добавление игрока: discord_id={discordId}, ckey={ckey}");
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO players (discordid, ckey) VALUES (@id, @ckey)";
            cmd.Parameters.AddWithValue("@id", (long)discordId);
            cmd.Parameters.AddWithValue("@ckey", ckey);
            cmd.ExecuteNonQuery();
        }

        public static string? GetPlayerCkey(ulong discordId)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Получение ckey для discord_id={discordId}");
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT ckey FROM players WHERE discordid = @id";
            cmd.Parameters.AddWithValue("@id", (long)discordId);
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        public static string? GetPlayerSponsorTier(ulong discordId)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Получение спонсорского тира для discord_id={discordId}");
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT sponsor FROM players WHERE discordid = @id";
            cmd.Parameters.AddWithValue("@id", (long)discordId);
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        public static string? GetPlayerSponsorExpires(ulong discordId)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Получение времени подписки для discord_id={discordId}");
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT sponsor_expires FROM players WHERE discordid = @id";
            cmd.Parameters.AddWithValue("@id", (long)discordId);
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        public static void UpdatePlayerSponsor(ulong discordId, string? sponsor)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Обновление спонсора для discord_id={discordId}: {sponsor}");
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE players SET sponsor = @sponsor WHERE discordid = @id";
            cmd.Parameters.AddWithValue("@sponsor", (object?)sponsor ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@id", (long)discordId);
            cmd.ExecuteNonQuery();
        }

        public static List<(ulong discordId, string? sponsor)> GetAllPlayers()
        {
            BotLoggerSystem.Log(LogType.DEBG, "Получение всех игроков");
            var players = new List<(ulong, string?)>();
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT discordid, sponsor FROM players";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                players.Add(((ulong)reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }
            return players;
        }

        public static bool GetPlayerAgeVerified(ulong discordId)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Получение age_verified для discord_id={discordId}");
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT age_verified FROM players WHERE discordid = @id";
            cmd.Parameters.AddWithValue("@id", (long)discordId);
            var result = cmd.ExecuteScalar();
            return result != null && result != System.DBNull.Value && (long)result == 1;
        }

        public static void SetPlayerAgeVerified(ulong discordId, bool verified)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Установка age_verified={verified} для discord_id={discordId}");
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE players SET age_verified = @verified WHERE discordid = @id";
            cmd.Parameters.AddWithValue("@verified", verified ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", (long)discordId);
            cmd.ExecuteNonQuery();
        }
    }
}
