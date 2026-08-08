using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace Egide.Bot
{
    public static class PermissionsSystem
    {
        public static async Task<bool> CheckAdminPermission(SocketInteractionContext context)
        {
            if (context.Guild == null)
            {
                await context.Interaction.RespondAsync("Команда доступна только на сервере.", ephemeral: true);
                return false;
            }

            if (context.User is not SocketGuildUser guildUser || !guildUser.GuildPermissions.Administrator)
            {
                await context.Interaction.RespondAsync("Недостаточно прав.", ephemeral: true);
                return false;
            }

            return true;
        }
    }
}
