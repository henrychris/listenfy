using NetCord.Rest;
using NetCord.Services;

namespace Listenfy.Application.Modules;

internal static class InteractionGuards
{
    public static Task BlockUsageOutsideServerAsync(IInteractionContext context)
    {
        return context.Interaction.SendResponseAsync(InteractionCallback.Message("❌ This command can only be used in a server!"));
    }
}
