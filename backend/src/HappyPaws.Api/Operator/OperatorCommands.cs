using HappyPaws.Application.Articles.SeedArticles;
using HappyPaws.Application.Clinics.Bootstrap;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Consultations.SeedTreatmentTemplates;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Identity.ResetCredentials;
using HappyPaws.Application.Staff.SeedStaff;

namespace HappyPaws.Api.Operator;

/// <summary>
/// The things only someone with the server can do, run as <c>HappyPaws.Api &lt;command&gt;</c>
/// instead of starting the web host:
/// <code>
/// migrate             apply pending schema migrations
/// bootstrap           create the starter roles, the first branch and the first owner
/// reset-credentials   re-issue an account's password and second factor
/// seed-articles       publish the sample health articles (skips any already there)
/// seed-staff          add sample staff in every department (skips addresses already taken)
/// seed-treatment-templates  add sample treatment templates (skips names already taken)
/// </code>
/// <para>
/// <b>These are commands, not endpoints, on purpose.</b> Each does something no request should be
/// able to do — change the schema, create an owner where there is none, replace credentials without
/// knowing the old ones. The contract has no route for any of them, and giving them one would mean
/// either an unauthenticated route that mints an owner, or a platform-admin credential this system
/// is explicitly built without ("ไม่มี platform admin"). Whoever can run a process on the host
/// already holds the database; this gives them a supported way to do what they could do anyway,
/// and one that writes an audit row.
/// </para>
/// <para>
/// The same binary, so a container image needs nothing extra:
/// <c>docker compose run --rm api bootstrap --owner-email …</c>.
/// </para>
/// </summary>
public static class OperatorCommands
{
    private const string Migrate = "migrate";
    private const string Bootstrap = "bootstrap";
    private const string ResetCredentials = "reset-credentials";
    private const string SeedArticles = "seed-articles";
    private const string SeedStaff = "seed-staff";
    private const string SeedTreatmentTemplates = "seed-treatment-templates";

    /// <summary>Set instead of <c>--owner-password</c> / <c>--password</c> to keep it out of shell history.</summary>
    private const string PasswordVariable = "HAPPYPAWS_OPERATOR_PASSWORD";

    /// <summary>True when the process was started to run a command rather than to serve requests.</summary>
    public static bool IsRequested(string[] args) =>
        args.Length > 0
        && args[0] is Migrate or Bootstrap or ResetCredentials or SeedArticles or SeedStaff or SeedTreatmentTemplates;

    /// <summary>Runs the command named by <c>args[0]</c> and returns the process exit code.</summary>
    public static async Task<int> RunAsync(IServiceProvider services, string[] args, TextWriter output)
    {
        var options = ParseOptions(args[1..]);

        await using var scope = services.CreateAsyncScope();

        try
        {
            switch (args[0])
            {
                case Migrate:
                    await MigrateAsync(scope.ServiceProvider, output);
                    break;

                case Bootstrap:
                    await BootstrapAsync(scope.ServiceProvider, options, output);
                    break;

                case ResetCredentials:
                    await ResetCredentialsAsync(scope.ServiceProvider, options, output);
                    break;

                case SeedArticles:
                    await SeedArticlesAsync(scope.ServiceProvider, output);
                    break;

                case SeedStaff:
                    await SeedStaffAsync(scope.ServiceProvider, output);
                    break;

                case SeedTreatmentTemplates:
                    await SeedTreatmentTemplatesAsync(scope.ServiceProvider, output);
                    break;
            }

            return 0;
        }
        catch (ValidationFailedException invalid)
        {
            foreach (var (field, messages) in invalid.Errors)
            {
                await output.WriteLineAsync($"error: {field}: {string.Join("; ", messages)}");
            }

            return 2;
        }
        catch (AppException refused)
        {
            await output.WriteLineAsync($"error: {refused.Code}: {refused.MessageEn ?? refused.Message}");

            return 1;
        }
    }

    private static async Task MigrateAsync(IServiceProvider services, TextWriter output)
    {
        var applied = await services.GetRequiredService<IDatabaseMigrator>().ApplyPendingAsync();

        await output.WriteLineAsync(applied.Count == 0
            ? "Database is up to date."
            : $"Applied {applied.Count} migration(s):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", applied)}");
    }

    private static async Task BootstrapAsync(
        IServiceProvider services,
        IReadOnlyDictionary<string, string> options,
        TextWriter output)
    {
        var result = await services.GetRequiredService<BootstrapClinicHandler>().HandleAsync(
            new BootstrapClinicCommand(
                options.GetValueOrDefault("owner-email"),
                options.GetValueOrDefault("owner-name"),
                options.GetValueOrDefault("owner-password") ?? Environment.GetEnvironmentVariable(PasswordVariable),
                options.GetValueOrDefault("clinic-name"),
                options.GetValueOrDefault("branch-code"),
                options.GetValueOrDefault("branch-name"),
                options.GetValueOrDefault("branch-rooms")?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));

        await output.WriteLineAsync(result.RolesCreated.Count == 0
            ? "Starter roles: already present."
            : $"Starter roles created: {string.Join(", ", result.RolesCreated)}");

        await output.WriteLineAsync(result.BranchId is null
            ? "Branch: one already exists."
            : $"Branch created: {result.BranchId}");

        if (result.Owner is null)
        {
            await output.WriteLineAsync("Owner: an active clinic_owner already exists. Nothing was created.");
            return;
        }

        await output.WriteLineAsync($"Owner created: {result.OwnerStaffId}");
        await WriteCredentialsAsync(result.Owner, output);
    }

    private static async Task ResetCredentialsAsync(
        IServiceProvider services,
        IReadOnlyDictionary<string, string> options,
        TextWriter output)
    {
        if (!options.TryGetValue("email", out var email))
        {
            throw new ValidationFailedException(
                new Dictionary<string, string[]> { ["email"] = ["--email is required"] });
        }

        var issued = await services.GetRequiredService<ResetCredentialsHandler>().HandleAsync(
            new ResetCredentialsCommand(
                email,
                options.GetValueOrDefault("password") ?? Environment.GetEnvironmentVariable(PasswordVariable)));

        await output.WriteLineAsync("Credentials reset. Every existing session for this account is revoked.");
        await WriteCredentialsAsync(issued, output);
    }

    private static async Task SeedArticlesAsync(IServiceProvider services, TextWriter output)
    {
        var result = await services.GetRequiredService<SeedArticlesHandler>().HandleAsync();

        await output.WriteLineAsync(result.Created.Count == 0
            ? "Articles: nothing new to publish."
            : $"Articles published: {string.Join(", ", result.Created)}");

        if (result.Skipped.Count > 0)
        {
            await output.WriteLineAsync($"Articles already present, left as they are: {string.Join(", ", result.Skipped)}");
        }
    }

    private static async Task SeedStaffAsync(IServiceProvider services, TextWriter output)
    {
        var result = await services.GetRequiredService<SeedStaffHandler>().HandleAsync();

        if (result.Created.Count == 0)
        {
            await output.WriteLineAsync("Staff: nothing new to add.");
        }
        else
        {
            await output.WriteLineAsync($"Staff added ({result.Created.Count}): {string.Join(", ", result.Created)}");
            await output.WriteLineAsync(
                "These accounts cannot sign in. To use one, run reset-credentials --email <address>.");
        }

        if (result.Skipped.Count > 0)
        {
            await output.WriteLineAsync($"Staff already present, left as they are: {string.Join(", ", result.Skipped)}");
        }
    }

    private static async Task SeedTreatmentTemplatesAsync(IServiceProvider services, TextWriter output)
    {
        var result = await services.GetRequiredService<SeedTreatmentTemplatesHandler>().HandleAsync();

        await output.WriteLineAsync(result.Created.Count == 0
            ? "Treatment templates: nothing new to add."
            : $"Treatment templates added: {string.Join(", ", result.Created)}");

        if (result.Skipped.Count > 0)
        {
            await output.WriteLineAsync(
                $"Treatment templates already present, left as they are: {string.Join(", ", result.Skipped)}");
        }
    }

    /// <summary>
    /// The one time these values are shown. They are printed for the person at the terminal and
    /// are not logged: the password is stored only as a hash, and nothing in the API returns the
    /// TOTP secret again.
    /// </summary>
    private static async Task WriteCredentialsAsync(IssuedCredentials issued, TextWriter output)
    {
        await output.WriteLineAsync();
        await output.WriteLineAsync("  ── shown once — store these now ─────────────────────────────");
        await output.WriteLineAsync($"  email        {issued.Email}");

        if (issued.GeneratedPassword is not null)
        {
            await output.WriteLineAsync($"  password     {issued.GeneratedPassword}");
        }

        await output.WriteLineAsync($"  totp secret  {issued.TotpSecret}");
        await output.WriteLineAsync($"  totp uri     {issued.OtpAuthUri}");
        await output.WriteLineAsync("  ──────────────────────────────────────────────────────────────");
        await output.WriteLineAsync("  Add the TOTP secret to an authenticator app before signing in.");
    }

    /// <summary>Reads <c>--name value</c> and <c>--name=value</c> pairs. Anything else is ignored.</summary>
    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var name = args[i][2..];
            var separator = name.IndexOf('=', StringComparison.Ordinal);

            if (separator > 0)
            {
                options[name[..separator]] = name[(separator + 1)..];
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options[name] = args[++i];
            }
        }

        return options;
    }
}
