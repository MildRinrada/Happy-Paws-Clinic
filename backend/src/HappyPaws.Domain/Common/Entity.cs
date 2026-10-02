namespace HappyPaws.Domain.Common;

/// <summary>
/// Base type for every persisted aggregate root.
/// <para>
/// The API contract states that every mutable resource carries a <c>version</c> which the client
/// echoes back in <c>If-Match</c>; a mismatch answers 409. <see cref="Version"/> is that value and
/// is mapped to a PostgreSQL <c>xmin</c> concurrency token in the Infrastructure layer.
/// </para>
/// </summary>
public abstract class Entity
{
    /// <summary>Opaque public identifier, e.g. <c>apt_24810</c>, <c>pet_luna</c>.</summary>
    public string Id { get; protected set; } = string.Empty;

    /// <summary>Optimistic concurrency token surfaced to clients as <c>version</c>.</summary>
    public uint Version { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset? UpdatedAt { get; protected set; }
}
