using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace HappyPaws.Infrastructure.Documents;

/// <summary>
/// The one-time QuestPDF setup every PDF in the system shares: the licence, and the Sarabun
/// typeface embedded in this assembly, so Thai reads the same on a server with no Thai fonts.
/// <para>
/// Used under QuestPDF's Community licence, which covers organisations under its revenue threshold.
/// A clinic above it needs a commercial licence; see <c>docs/features/clinical-records.md</c>.
/// </para>
/// </summary>
internal static class QuestPdfSetup
{
    /// <summary>The family name of the embedded typeface.</summary>
    public const string Font = "Sarabun";

    private static readonly Lazy<bool> Setup = new(() =>
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var assembly = typeof(QuestPdfSetup).Assembly;

        foreach (var name in assembly.GetManifestResourceNames().Where(resource => resource.EndsWith(".ttf", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            FontManager.RegisterFontFromStream(stream);
        }

        return true;
    });

    /// <summary>Runs the setup once per process; safe from any thread.</summary>
    public static void EnsureReady() => _ = Setup.Value;
}
