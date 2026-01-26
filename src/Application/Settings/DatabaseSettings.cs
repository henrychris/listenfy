using System.ComponentModel.DataAnnotations;

namespace Listenfy.Application.Settings;

public class DatabaseSettings
{
    [Required(AllowEmptyStrings = false)]
    public string UserId { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string Host { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    public string DatabaseName { get; set; } = null!;
    public int Port { get; set; } = 5432;

    /// <summary>
    /// If provided, it takes priority
    /// </summary>
    public string? ConnectionString { get; set; }
}
