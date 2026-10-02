using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Documents;

/// <summary>
/// A document on a patient's record. <c>download_url</c> is a short-lived link, present only once
/// the file has arrived.
/// </summary>
public sealed record PetDocumentDto(
    string Id,
    string PetId,
    string Source,
    string Kind,
    string Title,
    string FileName,
    string ContentType,
    long? SizeBytes,
    bool VisibleToOwner,
    string? ConsultationId,
    DateTimeOffset AddedAt,
    DateTimeOffset? UploadedAt,
    string? DownloadUrl)
{
    /// <summary>Long enough to open the file, short enough that a forwarded link stops working.</summary>
    public static readonly TimeSpan DownloadLinkLifetime = TimeSpan.FromMinutes(10);

    public static PetDocumentDto From(PetDocument document, IFileStorage? storage = null) =>
        new(
            document.Id,
            document.PetId,
            WireEnum.Of(document.Source),
            document.Kind,
            document.Title,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.VisibleToOwner,
            document.ConsultationId,
            document.CreatedAt,
            document.UploadedAt,
            document.IsUploaded && storage is not null
                ? storage.CreateDownloadUrl(document.StorageKey, DownloadLinkLifetime)
                : null);
}

/// <summary>Where the client sends the file: <c>PUT</c> the bytes to <c>url</c> with <c>headers</c>.</summary>
public sealed record UploadTicketDto(string Url, string Method, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt)
{
    public static UploadTicketDto From(UploadTicket ticket) => new(ticket.Url, ticket.Method, ticket.Headers, ticket.ExpiresAt);
}

/// <summary>The document as recorded, and the pre-signed URL to upload its file to.</summary>
public sealed record DocumentUploadDto(PetDocumentDto Document, UploadTicketDto Upload);
