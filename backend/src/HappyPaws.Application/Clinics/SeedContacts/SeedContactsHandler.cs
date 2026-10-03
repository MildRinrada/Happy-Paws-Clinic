using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.SeedContacts;

/// <summary>What <c>seed-contacts</c> did: how many entries it added, or that the clinic already had some.</summary>
public sealed record SeedContactsResult(int Created, bool AlreadyPresent);

/// <summary>
/// Adds the sample contact entries — the phone numbers, email, chat accounts and map the public site
/// used to have typed into its own source. Run by the operator's <c>seed-contacts</c> command, never
/// by a request.
/// <para>
/// Runs only on an empty list. A clinic that has entered even one entry of its own has started its
/// own list, and sample entries mixed into it would be worse than none.
/// </para>
/// </summary>
public sealed class SeedContactsHandler(IContactRepository contacts, IUnitOfWork unitOfWork)
{
    private static readonly (string Kind, string Label, string Value, string? Url)[] Samples =
    [
        (ContactKinds.Phone, "โทรศัพท์คลินิก", "02-123-4567", null),
        (ContactKinds.Emergency, "สายด่วนฉุกเฉิน 24 ชม.", "089-123-9111", null),
        (ContactKinds.Email, "อีเมล", "care@happypaws.co.th", null),
        (ContactKinds.Line, "LINE", "@happypaws", "https://line.me/R/ti/p/@happypaws"),
        (ContactKinds.Facebook, "Facebook", "Happy Paws Clinic", "https://www.facebook.com/"),
        (ContactKinds.Instagram, "Instagram", "@happypaws.clinic", "https://www.instagram.com/"),
        (ContactKinds.Map, "แผนที่", "88/4 ซอยสุขุมวิท 39 แขวงคลองตันเหนือ เขตวัฒนา กรุงเทพมหานคร 10110", "https://maps.google.com/?q=Sukhumvit+39+Bangkok"),
    ];

    public async Task<SeedContactsResult> HandleAsync(CancellationToken cancellationToken = default)
    {
        var created = 0;
        var present = false;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            created = 0;
            present = await contacts.AnyAsync(token);
            if (present) return;

            for (var index = 0; index < Samples.Length; index++)
            {
                var (kind, label, value, url) = Samples[index];
                contacts.Add(ClinicContact.Add(kind, label, value, url, null, null, (index + 1) * 10));
                created++;
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return new SeedContactsResult(created, present);
    }
}
