using HappyPaws.Application.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Assistant;

/// <summary>
/// Picks the assistant's model adapter from <c>Assistant:Provider</c>.
/// <para>
/// Decided when the model is first asked for, from the bound options, not while services are being
/// registered: configuration added after this runs (an environment variable layer, a test host)
/// would otherwise be missed, and the assistant would come up switched off.
/// </para>
/// </summary>
internal static class AssistantRegistration
{
    public static IServiceCollection AddAssistantModel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AssistantOptions>().Bind(configuration.GetSection(AssistantOptions.SectionName));

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AssistantOptions>>().Value;
            return new AssistantSettings(options.DailyConversationTurns, options.MaxToolRounds);
        });

        services.AddHttpClient<GeminiLanguageModel>(Configure);
        services.AddHttpClient<OpenAiCompatibleLanguageModel>(Configure);

        services.AddTransient<ILanguageModel>(provider =>
            provider.GetRequiredService<IOptions<AssistantOptions>>().Value.Provider.Trim().ToLowerInvariant() switch
            {
                "gemini" => provider.GetRequiredService<GeminiLanguageModel>(),
                "openai" => provider.GetRequiredService<OpenAiCompatibleLanguageModel>(),
                _ => new DisabledLanguageModel(),
            });

        return services;
    }

    private static void Configure(IServiceProvider provider, HttpClient client)
    {
        var seconds = provider.GetRequiredService<IOptions<AssistantOptions>>().Value.TimeoutSeconds;
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 120));
    }
}
