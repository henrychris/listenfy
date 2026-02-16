using System.ComponentModel.DataAnnotations;

namespace Listenfy.Application.Settings;

public class SpotifySettings
{
    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string ClientSecret { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string ApiBaseUrl { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string AccountsBaseUrl { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string RedirectUrl { get; set; } = null!;

    [Required(AllowEmptyStrings = false), Range(1, 60)]
    public int FetchDataJobIntervalInMinutes { get; set; }
}
