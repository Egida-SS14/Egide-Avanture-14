using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Egide.Bot
{
    /// <summary>
    /// Работа с основной базой данных сервера (тем же файлом/сервером, что использует игра).
    /// Таблица egide_discord_auth создаётся автоматически при первом запуске бота.
    /// </summary>
    public static class SqlSystem
    {
        private static string _engine = "sqlite";
        private static string _connectionString = "";

        public static void InitDatabase(BotConfig config)
        {
            _engine = config.DatabaseEngine;
            _connectionString = _engine == "postgres"
                ? config.DatabasePgConnectionString
                : $"Data Source={config.DatabaseSqlitePath}";

            BotLoggerSystem.Log(LogType.INFO, "Начало инициализации базы данных...");
            if (_engine != "postgres")
                SQLitePCL.Batteries_V2.Init();
            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS egide_discord_auth (
                    discord_id BIGINT PRIMARY KEY,
                    ckey TEXT NOT NULL,
                    sponsor TEXT,
                    sponsor_expires TEXT,
                    age_verified INTEGER NOT NULL DEFAULT 0
                )
                """;
            cmd.ExecuteNonQuery();
            BotLoggerSystem.Log(LogType.INFO, "База данных успешно инициализирована");
        }

        private static DbConnection OpenConnection()
        {
            DbConnection conn = _engine == "postgres"
                ? new NpgsqlConnection(_connectionString)
                : new SqliteConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private static DbCommand Command(DbConnection conn, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return cmd;
        }

        private static void AddParameter(DbCommand cmd, string name, object? value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        public static void AddPlayer(ulong discordId, string ckey)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Добавление игрока: discord_id={discordId}, ckey={ckey}");
            using var conn = OpenConnection();
            using var cmd = Command(conn,
                "INSERT INTO egide_discord_auth (discord_id, ckey) VALUES (@id, @ckey) " +
                "ON CONFLICT (discord_id) DO UPDATE SET ckey = EXCLUDED.ckey");
            AddParameter(cmd, "@id", (long)discordId);
            AddParameter(cmd, "@ckey", ckey);
            cmd.ExecuteNonQuery();
        }

        public static string? GetPlayerCkey(ulong discordId)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Получение ckey для discord_id={discordId}");
            using var conn = OpenConnection();
            using var cmd = Command(conn, "SELECT ckey FROM egide_discord_auth WHERE discord_id = @id");
            AddParameter(cmd, "@id", (long)discordId);
            return cmd.ExecuteScalar() as string;
        }

        public static string? GetPlayerSponsorTier(ulong discordId)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Получение спонсорского тира для discord_id={discordId}");
            using var conn = OpenConnection();
            using var cmd = Command(conn, "SELECT sponsor FROM egide_discord_auth WHERE discord_id = @id");
            AddParameter(cmd, "@id", (long)discordId);
            return cmd.ExecuteScalar() as string;
        }

        public static string? GetPlayerSponsorExpires(ulong discordId)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Получение времени подписки для discord_id={discordId}");
            using var conn = OpenConnection();
            using var cmd = Command(conn, "SELECT sponsor_expires FROM egide_discord_auth WHERE discord_id = @id");
            AddParameter(cmd, "@id", (long)discordId);
            return cmd.ExecuteScalar() as string;
        }

        public static void UpdatePlayerSponsor(ulong discordId, string? sponsor)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Обновление спонсора для discord_id={discordId}: {sponsor}");
            using var conn = OpenConnection();
            using var cmd = Command(conn, "UPDATE egide_discord_auth SET sponsor = @sponsor WHERE discord_id = @id");
            AddParameter(cmd, "@sponsor", sponsor);
            AddParameter(cmd, "@id", (long)discordId);
            cmd.ExecuteNonQuery();
        }

        public static List<(ulong discordId, string? sponsor)> GetAllPlayers()
        {
            BotLoggerSystem.Log(LogType.DEBG, "Получение всех игроков");
            var players = new List<(ulong, string?)>();
            using var conn = OpenConnection();
            using var cmd = Command(conn, "SELECT discord_id, sponsor FROM egide_discord_auth");
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
            using var conn = OpenConnection();
            using var cmd = Command(conn, "SELECT age_verified FROM egide_discord_auth WHERE discord_id = @id");
            AddParameter(cmd, "@id", (long)discordId);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value && Convert.ToInt64(result) == 1;
        }

        public static void SetPlayerAgeVerified(ulong discordId, bool verified)
        {
            BotLoggerSystem.Log(LogType.DEBG, $"Установка age_verified={verified} для discord_id={discordId}");
            using var conn = OpenConnection();
            using var cmd = Command(conn, "UPDATE egide_discord_auth SET age_verified = @verified WHERE discord_id = @id");
            AddParameter(cmd, "@verified", verified ? 1 : 0);
            AddParameter(cmd, "@id", (long)discordId);
            cmd.ExecuteNonQuery();
        }
    }
}
