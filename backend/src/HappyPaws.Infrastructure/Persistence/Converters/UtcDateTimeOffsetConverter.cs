using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HappyPaws.Infrastructure.Persistence.Converters;

/// <summary>
/// Normalises every <see cref="DateTimeOffset"/> to UTC on its way to PostgreSQL.
/// <para>
/// Npgsql refuses to write a <c>DateTimeOffset</c> whose offset is anything but zero into a
/// <c>timestamptz</c> column — it throws rather than converting. That is a problem here because the
/// contract's own wire format carries an offset: <c>2026-08-22T10:30:00+07:00</c>. Without this
/// converter, an appointment booked with a Bangkok offset — which is to say, every appointment
/// booked from Thailand — fails on insert, and every query filtered on a clinic-local day boundary
/// fails on the parameter.
/// </para>
/// <para>
/// Applied as a convention over all <c>DateTimeOffset</c> properties rather than per entity, so no
/// future mapping can quietly reintroduce the fault. Nothing is lost: <c>timestamptz</c> stores an
/// instant, and <c>ToUniversalTime</c> preserves the instant exactly — only the offset used to
/// print it is dropped, and PostgreSQL never stored that anyway.
/// </para>
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(
            value => value.ToUniversalTime(),
            // Read back as UTC, which is what the database holds. Rendering into clinic-local time
            // is a presentation concern and belongs at the edge, not in the mapping.
            value => value.ToUniversalTime())
    {
    }
}
