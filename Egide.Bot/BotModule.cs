using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Egide.Bot
{
    public class BotModule : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("hello", "Приветствие")]
        public async Task Hello()
        {
            await RespondAsync("hello");
        }

        [SlashCommand("connect", "Подключить аккаунт Space Station 14")]
        public async Task Connect(string ckey)
        {
            SqlSystem.AddPlayer(Context.User.Id, ckey);

            if (Context.Guild != null)
            {
                var role = Context.Guild.GetRole(1366301442067140628);
                if (role != null)
                {
                    var guildUser = Context.User as IGuildUser;
                    if (guildUser != null)
                    {
                        await guildUser.AddRoleAsync(role);
                        BotLoggerSystem.Log(LogType.DEBG, $"Выдана роль Authorized пользователю {Context.User.Id}");
                    }
                }
            }

            await RespondAsync($"Аккаунт `{ckey}` успешно подключен!");
        }

        [SlashCommand("myinfo", "Показать данные о вашем аккаунте")]
        public async Task MyInfo()
        {
            var ckey = SqlSystem.GetPlayerCkey(Context.User.Id);
            if (ckey == null)
            {
                await RespondAsync("У вас нет привязанного аккаунта. Используйте `/connect <c-key>` для привязки.");
                return;
            }

            var sponsor = SqlSystem.GetPlayerSponsorTier(Context.User.Id) ?? "Нету";
            var expires = SqlSystem.GetPlayerSponsorExpires(Context.User.Id) ?? "Бессрочно";
            var ageVerified = SqlSystem.GetPlayerAgeVerified(Context.User.Id);
            var verifiedStatus = ageVerified ? "Да" : "Нет";

            await RespondAsync(
                $"Ваш ckey: `{ckey}`\nУровень подписки: {sponsor}\nИстекает: {expires}\nВозраст верифицирован: {verifiedStatus}"
            );
        }

        [SlashCommand("resync", "Принудительный скан ролей (только для администрации)")]
        public async Task Resync()
        {
            if (!await PermissionsSystem.CheckAdminPermission(Context))
                return;

            await DeferAsync();

            if (Context.Guild == null)
            {
                await FollowupAsync("Команда доступна только на сервере.");
                return;
            }

            var t1Role = Context.Guild.GetRole(1367560820250382506);
            var t2Role = Context.Guild.GetRole(1463938183581532201);
            var authorizedRole = Context.Guild.GetRole(1366301442067140628);

            foreach (var member in Context.Guild.Users)
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

            await FollowupAsync("Скан ролей завершён.");
        }

        [SlashCommand("verify", "Верифицировать возраст пользователя (только для администрации)")]
        public async Task Verify(IGuildUser user)
        {
            if (!await PermissionsSystem.CheckAdminPermission(Context))
                return;

            var ckey = SqlSystem.GetPlayerCkey(user.Id);
            if (ckey == null)
            {
                await RespondAsync(
                    $"У пользователя {user.Mention} нет привязанного аккаунта.", ephemeral: true
                );
                return;
            }

            SqlSystem.SetPlayerAgeVerified(user.Id, true);

            var role = Context.Guild?.GetRole(1533145284316627095);
            if (role != null)
            {
                await user.AddRoleAsync(role);
                BotLoggerSystem.Log(LogType.DEBG, $"Выдана роль Age Verified пользователю {user.Id}");
            }

            await RespondAsync($"Пользователь {user.Mention} верифицирован.");
        }

        [SlashCommand("unverify", "Снять верификацию возраста пользователя (только для администрации)")]
        public async Task Unverify(IGuildUser user)
        {
            if (!await PermissionsSystem.CheckAdminPermission(Context))
                return;

            SqlSystem.SetPlayerAgeVerified(user.Id, false);

            var role = Context.Guild?.GetRole(1533145284316627095);
            if (role != null)
            {
                await user.RemoveRoleAsync(role);
                BotLoggerSystem.Log(LogType.DEBG, $"Снята роль Age Verified у пользователя {user.Id}");
            }

            await RespondAsync($"Верификация возраста пользователя {user.Mention} снята.");
        }
    }
}
