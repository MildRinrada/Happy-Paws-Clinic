using System.Text.RegularExpressions;

namespace HappyPaws.Domain.PrivacyPolicies;

/// <summary>
/// What a privacy notice must be before it is kept, and what a version label may be
/// (<c>docs/contract-extensions.md § 35</c>). The answers are Thai, for the writer, keyed by the
/// field path the editor shows them under, e.g. <c>sections[2].body</c>.
/// </summary>
public static partial class PrivacyPolicyRules
{
    public const int MaxSections = 30;
    public const int AnchorMaxLength = 40;
    public const int HeadingMaxLength = 150;
    public const int BodyMaxLength = 10_000;
    public const int LabelMaxLength = 32;

    /// <summary>
    /// The first section of the public page is drawn from the clinic's own settings
    /// (<c>GET /clinic</c>, <c>GET /clinic/contacts</c>), under this anchor. A written section may not take it.
    /// </summary>
    public const string ControllerAnchor = "controller";

    /// <summary>
    /// The label the consents recorded before § 35 carry. It names the notice that was written into
    /// the web app's code, which is not a row here, so no published version may take it.
    /// </summary>
    public const string LegacyLabel = "2026-10";

    /// <summary>Cleans each section's text as it would be kept.</summary>
    public static IReadOnlyList<PrivacyPolicySection> Clean(IEnumerable<PrivacyPolicySection> sections) =>
    [
        .. sections.Select(section => new PrivacyPolicySection(
            (section.Anchor ?? string.Empty).Trim(),
            PrivacyPolicyText.CleanLine(section.Heading ?? string.Empty),
            PrivacyPolicyText.CleanBody(section.Body ?? string.Empty))),
    ];

    /// <summary>Every problem with these (already cleaned) sections. Empty when they may be kept.</summary>
    public static IReadOnlyDictionary<string, string[]> Problems(IReadOnlyList<PrivacyPolicySection> sections)
    {
        var problems = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (sections.Count == 0)
        {
            problems["sections"] = ["ต้องมีอย่างน้อยหนึ่งหัวข้อ"];
            return problems;
        }

        if (sections.Count > MaxSections)
        {
            problems["sections"] = [$"มีได้ไม่เกิน {MaxSections} หัวข้อ"];
            return problems;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            var at = $"sections[{index}]";

            if (!IsAnchor(section.Anchor))
            {
                problems[$"{at}.anchor"] =
                    [$"ชื่อจุดเชื่อมต้องเป็นตัวพิมพ์เล็กภาษาอังกฤษ ตัวเลข และขีดกลาง ขึ้นต้นด้วยตัวอักษร ยาวไม่เกิน {AnchorMaxLength} ตัว เช่น data-we-collect"];
            }
            else if (section.Anchor == ControllerAnchor)
            {
                problems[$"{at}.anchor"] = ["ชื่อ controller ใช้กับหัวข้อข้อมูลคลินิกที่ระบบเติมให้เอง กรุณาใช้ชื่ออื่น"];
            }
            else if (!seen.Add(section.Anchor))
            {
                problems[$"{at}.anchor"] = ["ชื่อจุดเชื่อมซ้ำกับหัวข้ออื่น"];
            }

            if (section.Heading.Length == 0)
            {
                problems[$"{at}.heading"] = ["ต้องมีชื่อหัวข้อ"];
            }
            else if (section.Heading.Length > HeadingMaxLength)
            {
                problems[$"{at}.heading"] = [$"ชื่อหัวข้อยาวได้ไม่เกิน {HeadingMaxLength} ตัวอักษร"];
            }

            if (section.Body.Length > BodyMaxLength)
            {
                problems[$"{at}.body"] = [$"เนื้อหาหนึ่งหัวข้อยาวได้ไม่เกิน {BodyMaxLength:N0} ตัวอักษร"];
            }
            else if (PrivacyPolicyText.Problems(section.Body) is { Count: > 0 } body)
            {
                problems[$"{at}.body"] = [.. body];
            }
        }

        return problems;
    }

    /// <summary>True for an anchor: <c>data-we-collect</c>, <c>cookies</c>, <c>ai-assistants</c>.</summary>
    public static bool IsAnchor(string anchor) => anchor.Length <= AnchorMaxLength && Anchor().IsMatch(anchor);

    /// <summary>
    /// True for a version label: <c>2026-11</c>, <c>2027-01a</c>, <c>v2</c>. It appears in the page's
    /// address, so letters, digits, dot, underscore and hyphen only, starting with a letter or digit.
    /// </summary>
    public static bool IsLabel(string label) => label.Length <= LabelMaxLength && Label().IsMatch(label);

    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$")]
    private static partial Regex Anchor();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex Label();
}
