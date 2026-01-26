namespace Listenfy.Domain.Models;

public abstract class BaseEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
