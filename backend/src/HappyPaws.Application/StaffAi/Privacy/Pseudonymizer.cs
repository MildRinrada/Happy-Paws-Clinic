using System.Globalization;
using System.Text.RegularExpressions;

namespace HappyPaws.Application.StaffAi.Privacy;

/// <summary>What a placeholder stands for. The name becomes the token's prefix: <c>[OWNER_1]</c>.</summary>
public enum PersonalKind
{
    /// <summary>The pet's owner or a carer, or the customer writing in a thread.</summary>
    Owner,

    /// <summary>A member of the clinic's staff.</summary>
    Staff,

    Phone,
    Email,

    /// <summary>A 13-digit Thai national id number.</summary>
    NationalId,

    Address,
}

/// <summary>
/// Takes people's personal data out of text before it goes to the model, and puts it back in what
/// the model wrote.
/// <para>
/// <b>What is replaced.</b> Values registered for this request (owner and staff names, phone
/// numbers, emails, addresses) wherever they appear, longest first; the parts of a registered name
/// when they stand on their own or follow a Thai title such as <c>คุณ</c> or <c>หมอ</c>; and, in
/// any text, anything shaped like an email, a 13-digit national id or a Thai phone number. Each
/// distinct value gets one token for the request, <c>[OWNER_1]</c>, <c>[PHONE_2]</c>, so the model
/// can still tell two people apart and refer to them.
/// </para>
/// <para>
/// <b>What is not.</b> A name that was never registered and has no title before it (a neighbour
/// mentioned in a note), a name glued to other Thai words with no title, a street address typed into
/// free text, a LINE id. Thai has no spaces between words, so a name part that is also an everyday
/// word is only masked where it is clearly a name. These are the documented limits in
/// <c>docs/features/staff-ai.md</c>; the stronger guarantee is what is never put in the prompt at
/// all (no customer record, no contact field, no ids of people).
/// </para>
/// <para>
/// One instance per request. It holds real values, so it is never logged or kept.
/// </para>
/// </summary>
public sealed partial class Pseudonymizer
{
    /// <summary>Thai titles that mark the word after them as a person's name.</summary>
    private static readonly string[] Titles =
        ["คุณ", "นาย", "นางสาว", "นาง", "น.ส.", "หมอ", "สพ.ญ.", "น.สพ.", "ดร.", "พี่", "น้อง"];

    /// <summary>The shortest name part masked on its own. Shorter parts are too likely to be ordinary words.</summary>
    private const int MinimumPartLength = 3;

    private readonly Dictionary<string, string> _tokenByValue = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _valueByToken = new(StringComparer.Ordinal);
    private readonly Dictionary<PersonalKind, int> _counters = [];

    /// <summary>Name parts that are masked only where they clearly stand for the person.</summary>
    private readonly Dictionary<string, string> _partToken = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The tokens handed out so far, for tests and for checking a prompt.</summary>
    public IReadOnlyCollection<string> Tokens => _valueByToken.Keys;

    /// <summary>
    /// Registers a person's name and returns its token. A blank name returns null. The same name
    /// always gets the same token within one request.
    /// </summary>
    public string? Person(string? name, PersonalKind kind = PersonalKind.Owner)
    {
        var value = Clean(name);
        if (value is null)
        {
            return null;
        }

        var token = TokenFor(value, kind);

        foreach (var part in NameParts(value))
        {
            _partToken.TryAdd(part, token);
        }

        return token;
    }

    /// <summary>Registers a contact detail (phone, email, address) so it is masked wherever it appears.</summary>
    public string? Register(string? value, PersonalKind kind)
    {
        var cleaned = Clean(value);
        return cleaned is null ? null : TokenFor(cleaned, kind);
    }

    /// <summary>
    /// The text with every registered value and every pattern-shaped detail replaced by its token.
    /// Null and blank stay as they are.
    /// </summary>
    public string Mask(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var masked = text;

        // Registered values first, longest first, so "สมชาย ใจดี" is replaced before "สมชาย".
        foreach (var (value, token) in _tokenByValue.OrderByDescending(pair => pair.Key.Length))
        {
            masked = masked.Replace(value, token, StringComparison.OrdinalIgnoreCase);
        }

        // Name parts: after a title, or standing alone between spaces, punctuation or the ends.
        foreach (var (part, token) in _partToken.OrderByDescending(pair => pair.Key.Length))
        {
            masked = PartPattern(part).Replace(masked, match => match.Groups["title"].Value + token);
        }

        masked = EmailPattern().Replace(masked, match => TokenFor(match.Value, PersonalKind.Email));
        masked = NationalIdPattern().Replace(masked, match => TokenFor(match.Value, PersonalKind.NationalId));
        masked = PhonePattern().Replace(masked, match => TokenFor(match.Value, PersonalKind.Phone));

        return masked;
    }

    /// <summary>
    /// The model's text with every token it used put back to the real value. A token this request
    /// never handed out (the model made one up) is left as it is, visibly, rather than guessed at.
    /// </summary>
    public string Restore(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return TokenPattern().Replace(text, match =>
            _valueByToken.TryGetValue(match.Value, out var value) ? value : match.Value);
    }

    /// <summary>True when the text still holds any value this request registered or found.</summary>
    public bool Leaks(string text) =>
        _tokenByValue.Keys.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));

    private string TokenFor(string value, PersonalKind kind)
    {
        if (_tokenByValue.TryGetValue(value, out var existing))
        {
            return existing;
        }

        var next = _counters.GetValueOrDefault(kind) + 1;
        _counters[kind] = next;

        var token = string.Create(CultureInfo.InvariantCulture, $"[{Prefix(kind)}_{next}]");
        _tokenByValue[value] = token;
        _valueByToken[token] = value;

        return token;
    }

    private static string Prefix(PersonalKind kind) => kind switch
    {
        PersonalKind.Owner => "OWNER",
        PersonalKind.Staff => "STAFF",
        PersonalKind.Phone => "PHONE",
        PersonalKind.Email => "EMAIL",
        PersonalKind.NationalId => "ID",
        PersonalKind.Address => "ADDRESS",
        _ => "PERSON",
    };

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>"คุณสมชาย ใจดี" gives "สมชาย" and "ใจดี": the words of the name without a title.</summary>
    private static IEnumerable<string> NameParts(string name)
    {
        foreach (var word in name.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            // A title on its own ("สพ.ญ. มินท์") is not part of the name.
            if (Titles.Contains(word, StringComparer.Ordinal))
            {
                continue;
            }

            var part = word;
            foreach (var title in Titles.OrderByDescending(title => title.Length))
            {
                if (part.StartsWith(title, StringComparison.Ordinal) && part.Length > title.Length)
                {
                    part = part[title.Length..];
                    break;
                }
            }

            if (part.Length >= MinimumPartLength && !string.Equals(part, name, StringComparison.OrdinalIgnoreCase))
            {
                yield return part;
            }
        }
    }

    private static Regex PartPattern(string part)
    {
        var titles = string.Join("|", Titles.OrderByDescending(title => title.Length).Select(Regex.Escape));
        var escaped = Regex.Escape(part);

        // Either a title right before it (Thai writes "คุณสมชาย" with no space), or a boundary of
        // whitespace, punctuation or the text's ends on both sides.
        return new Regex(
            $@"(?<title>(?:{titles})\s?){escaped}|(?<title>)(?<=^|[\s\p{{P}}]){escaped}(?=$|[\s\p{{P}}])",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex EmailPattern();

    /// <summary>Thirteen digits, optionally grouped with spaces or dashes: 1-2345-67890-12-3.</summary>
    [GeneratedRegex(@"(?<!\d)\d(?:[\s\-]?\d){12}(?!\d)", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex NationalIdPattern();

    /// <summary>
    /// A Thai phone number: 0 then eight or nine digits (02 123 4567, 081-234-5678), or +66 then
    /// eight or nine, optionally grouped with spaces, dots or dashes.
    /// </summary>
    [GeneratedRegex(@"(?<![\d+])(?:\+66|0066|0)(?:[\s.\-]?\d){8,9}(?!\d)", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\[(?:OWNER|STAFF|PHONE|EMAIL|ID|ADDRESS|PERSON)_\d+\]", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TokenPattern();
}
