using System.ComponentModel.DataAnnotations;

namespace Listenfy.Application.Settings;

public class CorsSettings
{
    [Required(AllowEmptyStrings = false)]
    public string AllowedUrls { get; set; } = null!;
}
