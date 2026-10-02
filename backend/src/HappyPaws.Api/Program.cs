using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HappyPaws.Api.Errors;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Http;
using HappyPaws.Api.Idempotency;
using HappyPaws.Api.Middleware;
using HappyPaws.Api.Operator;
using HappyPaws.Application;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (OperatorCommands.IsRequested(args))
{
    // An operator command prints a few result lines, and bootstrap prints credentials shown only once.
    // Development's debug logging would bury them under a thousand lines, so commands log warnings and
    // above. EF's own SQL log is silenced: on an empty database it reports the migrations-history probe
    // as an error, and a real failure still surfaces as the command's exception.
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
        ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command"] = "None",
    });
}

// ---------------------------------------------------------------------------
// Composition root only. Each layer owns its own registration extension so this
// file stays readable as the 164 endpoints land.
// ---------------------------------------------------------------------------

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IRequestContext, RequestContext>();

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(ModelStateErrors.UseSharedEnvelope)
    .AddJsonOptions(options =>
    {
        // The contract's wire format is snake_case (next_cursor, message_en, starts_at).
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Thai messages travel unescaped so clients can display them directly.
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        // Every paginated list leaves in the contract's envelope — data[] and page.next_cursor —
        // whether or not the controller remembered to wrap it.
        options.JsonSerializerOptions.Converters.Add(new CursorPageJsonConverter());
    });

builder.Services.AddClinicAuthentication();
builder.Services.AddClinicAuthorization();
builder.Services.AddClinicOpenApi();
builder.Services.AddClinicRateLimiting(builder.Configuration);

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    // Clients need these to implement optimistic concurrency and request tracing.
    .WithExposedHeaders("ETag", RequestContext.RequestIdHeader, "Idempotent-Replay")));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Operator commands — migrate, bootstrap, reset-credentials — run against the
// same composition root and exit. The web host is never started for them, so
// none of what they do is reachable over HTTP. See Operator/OperatorCommands.cs.
// ---------------------------------------------------------------------------

if (OperatorCommands.IsRequested(args))
{
    return await OperatorCommands.RunAsync(app.Services, args, Console.Out);
}

// ---------------------------------------------------------------------------
// Pipeline order is load-bearing — see docs/architecture.md § 5.
// ---------------------------------------------------------------------------

// The contract's base URL ends in /v1 ("https://api.happypaws.co.th/v1"), and every path in it is
// written relative to that. Declared as a path base rather than typed into forty [Route]
// attributes: the routes stay exactly as the reference prints them, and the version lives in one
// line. Ahead of everything else, so routing and the refresh cookie's path both see it.
app.UsePathBase(ApiVersion.PathBase);

// First, so even a failure inside authentication carries the request id the client is shown.
app.UseExceptionHandler();
app.UseMiddleware<RequestContextMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors();

app.UseAuthentication();

// After authentication, and that ordering is the whole policy. The limiter partitions by token
// subject, falling back to the caller's IP when there is no token — but `context.User` is empty
// until authentication has run, so a limiter placed before it silently keys *every* request to the
// IP. A whole clinic sits behind one office NAT, which is exactly the case the partition exists to
// avoid: one runaway script would throttle every receptionist at the counter.
app.UseRateLimiter();

app.UseAuthorization();

// After authorization: the idempotency key is scoped per account, so it needs the caller
// resolved, and an unauthorized request must never claim a key.
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

app.Run();

return 0;

/// <summary>Exposed so <c>WebApplicationFactory</c> can boot the API in integration tests.</summary>
public partial class Program;
