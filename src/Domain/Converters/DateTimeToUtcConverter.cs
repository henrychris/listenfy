using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Listenfy.Domain.Converters;

public class DateTimeToUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeToUtcConverter()
        : base(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        ) { }
}
