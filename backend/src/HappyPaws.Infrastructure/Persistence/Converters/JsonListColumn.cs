using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HappyPaws.Infrastructure.Persistence.Converters;

/// <summary>
/// Maps a list of small value objects onto one <c>jsonb</c> column.
/// <para>
/// For the handful of lists that belong to their row and to nothing else — a week of opening
/// hours, a service's price tiers and booking windows. They are read and replaced as a whole, never
/// queried into, so a child table would buy a join on every read and nothing in return. Lists of
/// plain strings do not come through here; those are <c>text[]</c> primitive collections.
/// </para>
/// <para>
/// The comparer is what makes saving work. EF compares a converted property by reference unless
/// told otherwise, and these lists are mutated in place behind a read-only view — so without a
/// comparer that looks at the contents, replacing a clinic's hours would change nothing in the
/// database and raise no error.
/// </para>
/// </summary>
internal static class JsonListColumn
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<IReadOnlyList<T>> HasJsonListConversion<T>(
        this PropertyBuilder<IReadOnlyList<T>> property) =>
        property
            .HasColumnType("jsonb")
            .HasConversion(
                new ValueConverter<IReadOnlyList<T>, string>(
                    list => Write(list),
                    json => Read<T>(json)),
                new ValueComparer<IReadOnlyList<T>>(
                    (first, second) => Write(first) == Write(second),
                    list => Write(list).GetHashCode(),
                    list => Read<T>(Write(list))));

    private static string Write<T>(IReadOnlyList<T>? list) => JsonSerializer.Serialize(list ?? [], Options);

    private static IReadOnlyList<T> Read<T>(string json) =>
        JsonSerializer.Deserialize<List<T>>(json, Options) ?? [];
}
