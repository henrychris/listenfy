using System.ComponentModel.DataAnnotations;

namespace Listenfy.Application.Settings;

public class DiscordSettings
{
    [Required(AllowEmptyStrings = false)]
    public string BotToken { get; set; } = null!;
}
