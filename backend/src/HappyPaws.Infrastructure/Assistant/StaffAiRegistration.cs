using HappyPaws.Application.Assistant;
using HappyPaws.Application.StaffAi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Assistant;

/// <summary>
/// The staff assistant's model: the website assistant's adapters, built from <c>StaffAi:*</c> with
/// the fallback to <c>Assistant:*</c> described on <see cref="StaffAiOptions"/>, on an HTTP client
/// of its own so its timeout is its own. No new SDK: the same two adapters, a different
/// configuration.
/// <para>
/// Decided when the model is first asked for, like the website assistant's, so configuration
/// layered on after registration (the environment, a test host) is seen.
/// </para>
/// </summary>
internal static class StaffAiRegistration
{
    public const string HttpClientName = "staff-ai";

    public static IServiceCollection AddStaffAiModel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StaffAiOptions>().Bind(configuration.GetSection(StaffAiOptions.SectionName));

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<StaffAiOptions>>().Value;
            return new StaffAiSettings(options.MonthlyCalls, options.MaxToolRounds, options.DailyBriefHour);
        });

        services.AddHttpClient(HttpClientName, (provider, client) =>
        {
            var seconds = provider.GetRequiredService<IOptions<StaffAiOptions>>().Value.TimeoutSeconds;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 180));
        });

        services.AddTransient<IStaffLanguageModel>(provider =>
        {
            var model = Resolve(
                provider.GetRequiredService<IOptions<StaffAiOptions>>().Value,
                provider.GetRequiredService<IOptions<AssistantOptions>>().Value);

            var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);

            ILanguageModel inner = model.Provider switch
            {
                "gemini" => new GeminiLanguageModel(http, Options.Create(model)),
                "openai" => new OpenAiCompatibleLanguageModel(http, Options.Create(model)),
                _ => new DisabledLanguageModel(),
            };

            return new StaffLanguageModel(inner);
        });

        return services;
    }

    /// <summary>The provider block the staff assistant uses, as the adapters' own options type.</summary>
    internal static AssistantOptions Resolve(StaffAiOptions staff, AssistantOptions website)
    {
        var own = staff.Provider.Trim().ToLowerInvariant();

        var (provider, model, key, baseUrl) = own switch
        {
            StaffAiOptions.Off => (string.Empty, string.Empty, string.Empty, string.Empty),
            "" => (website.Provider, website.Model, website.ApiKey, website.BaseUrl),
            _ => (own, staff.Model, staff.ApiKey, staff.BaseUrl),
        };

        return new AssistantOptions
        {
            Provider = provider.Trim().ToLowerInvariant(),
            Model = model,
            ApiKey = key,
            BaseUrl = baseUrl,
            Temperature = staff.Temperature,
            MaxOutputTokens = Math.Clamp(staff.MaxOutputTokens, 256, 8192),
            TimeoutSeconds = staff.TimeoutSeconds,
        };
    }

    /// <summary>The website assistant's adapter, under the staff assistant's own port.</summary>
    private sealed class StaffLanguageModel(ILanguageModel inner) : IStaffLanguageModel
    {
        public bool IsConfigured => inner.IsConfigured;

        public Task<ModelTurn> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default) =>
            inner.CompleteAsync(request, cancellationToken);
    }
}
