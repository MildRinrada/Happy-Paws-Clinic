using System.Text.RegularExpressions;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Domain.PrivacyPolicies;

/// <summary>A run of a paragraph or a list item: plain text, or a link to a page of the clinic's own site.</summary>
/// <param name="Text">What readers see.</param>
/// <param name="Path">The site path the text links to, or null for plain text.</param>
public sealed record PolicyInline(string Text, string? Path = null)
{
    public bool IsLink => Path is not null;
}

/// <summary>A paragraph, or a list whose items are each a run of inlines.</summary>
/// <param name="Items">One entry for a paragraph; one per item for a list.</param>
/// <param name="IsList">True for a list.</param>
public sealed record PolicyBlock(IReadOnlyList<IReadOnlyList<PolicyInline>> Items, bool IsList);

/// <summary>
/// The body format of a privacy notice section (<c>docs/contract-extensions.md § 35</c>): plain text
/// with exactly three rules, so the clinic can write it in a textarea and nothing in it can ever run
/// in a reader's browser.
/// <list type="number">
/// <item>A blank line starts a new paragraph. Lines next to each other are one paragraph, joined by a space.</item>
/// <item>A line starting with <c>- </c> (hyphen, space) is a list item. Items next to each other are one list.</item>
/// <item><c>[label](/path)</c> links <c>label</c> to a page of the clinic's own site. The path starts with
/// one <c>/</c>; anything else in the brackets stays text.</item>
/// </list>
/// <para>
/// Not Markdown and not HTML, for the reason articles are not (<c>§ 26</c>): the page renders text
/// nodes, so there is no sanitiser to keep up to date. On top of that, a notice that links away
/// from the clinic's site would be the clinic vouching for a page it does not control, so a link
/// that is not a site path, and anything that looks like an HTML tag, is refused when the text is
/// saved (<see cref="Problems"/>). The reader (<see cref="Parse"/>) is still strict on its own: a
/// link it does not accept is shown as the characters that were typed.
/// </para>
/// <para>
/// The web app has a copy of <see cref="Parse"/> for its renderer and the editor's preview
/// (<c>frontend/src/features/privacy/lib/policy-body.ts</c>). The two must read text the same way.
/// </para>
/// </summary>
public static partial class PrivacyPolicyText
{
    public const int PathMaxLength = 300;
    public const int LabelMaxLength = 120;

    /// <summary>Cleans a body as articles are cleaned: control and bidirectional characters out, one blank line between paragraphs.</summary>
    public static string CleanBody(string body) => ArticleText.CleanBody(body);

    /// <summary>Cleans a heading: one line, single spaces.</summary>
    public static string CleanLine(string line) => ArticleText.CleanLine(line);

    /// <summary>True for a path on the clinic's own site: one leading <c>/</c>, no scheme, no host, no spaces.</summary>
    public static bool IsSitePath(string path) =>
        path.Length <= PathMaxLength && SitePath().IsMatch(path) && !path.StartsWith("//", StringComparison.Ordinal);

    /// <summary>The paragraphs and lists of a body, as the public page draws them.</summary>
    public static IReadOnlyList<PolicyBlock> Parse(string body)
    {
        var blocks = new List<PolicyBlock>();
        var paragraph = new List<string>();
        var list = new List<IReadOnlyList<PolicyInline>>();

        void FlushParagraph()
        {
            if (paragraph.Count > 0)
            {
                blocks.Add(new PolicyBlock([Inlines(string.Join(' ', paragraph))], IsList: false));
                paragraph.Clear();
            }
        }

        void FlushList()
        {
            if (list.Count > 0)
            {
                blocks.Add(new PolicyBlock([.. list], IsList: true));
                list.Clear();
            }
        }

        foreach (var raw in Lines(body))
        {
            var line = raw.Trim();

            if (line.Length == 0)
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            if (ListItem(line) is { } item)
            {
                FlushParagraph();

                if (item.Length > 0)
                {
                    list.Add(Inlines(item));
                }

                continue;
            }

            FlushList();
            paragraph.Add(line);
        }

        FlushParagraph();
        FlushList();

        return blocks;
    }

    /// <summary>
    /// What is wrong with a body, in Thai, for the writer. Empty when it may be saved. Checked on
    /// the cleaned body.
    /// </summary>
    public static IReadOnlyList<string> Problems(string body)
    {
        var problems = new List<string>();

        if (HtmlTag().IsMatch(body))
        {
            problems.Add("เนื้อหาต้องเป็นข้อความธรรมดา ห้ามใส่แท็ก HTML เช่น <a> หรือ <script>");
        }

        foreach (Match link in Link().Matches(body))
        {
            var path = link.Groups["path"].Value;

            if (!IsSitePath(path))
            {
                problems.Add($"ลิงก์ \"{link.Groups["label"].Value}\" ต้องชี้ไปหน้าในเว็บของคลินิกเอง ขึ้นต้นด้วย / เช่น (/portal/account) ห้ามลิงก์ออกนอกเว็บ");
            }
        }

        foreach (var raw in Lines(body))
        {
            if (ListItem(raw.Trim()) is { Length: 0 })
            {
                problems.Add("รายการที่ขึ้นต้นด้วย - ต้องมีข้อความต่อท้าย");
                break;
            }
        }

        if (problems.Count == 0 && Parse(body).Count == 0)
        {
            problems.Add("ต้องมีเนื้อหา");
        }

        return problems;
    }

    private static string[] Lines(string body) =>
        body.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

    /// <summary>The text of a list item line, empty for a bare "-", or null when the line is not a list item.</summary>
    private static string? ListItem(string line) =>
        line == "-" ? string.Empty
        : line.StartsWith("- ", StringComparison.Ordinal) ? line[2..].Trim()
        : null;

    private static List<PolicyInline> Inlines(string text)
    {
        var inlines = new List<PolicyInline>();
        var at = 0;

        foreach (Match link in Link().Matches(text))
        {
            var path = link.Groups["path"].Value;

            if (!IsSitePath(path))
            {
                // Left as text: it becomes part of the run around it.
                continue;
            }

            if (link.Index > at)
            {
                inlines.Add(new PolicyInline(text[at..link.Index]));
            }

            inlines.Add(new PolicyInline(link.Groups["label"].Value.Trim(), path));
            at = link.Index + link.Length;
        }

        if (at < text.Length)
        {
            inlines.Add(new PolicyInline(text[at..]));
        }

        return inlines;
    }

    [GeneratedRegex(@"\[(?<label>[^\[\]\n]{1,120})\]\((?<path>[^()\s]{1,300})\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"^/[A-Za-z0-9\-._~/?#=&%+]*$")]
    private static partial Regex SitePath();

    [GeneratedRegex(@"<\s*/?\s*[A-Za-z!?]")]
    private static partial Regex HtmlTag();
}
