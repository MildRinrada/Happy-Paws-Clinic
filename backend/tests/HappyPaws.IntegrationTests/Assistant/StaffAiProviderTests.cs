using HappyPaws.Infrastructure.Assistant;

namespace HappyPaws.IntegrationTests.Assistant;

/// <summary>
/// Which provider block the staff assistant uses (<c>StaffAiOptions</c>): its own, the website
/// assistant's, or none. The two blocks are never mixed.
/// </summary>
public class StaffAiProviderTests
{
    private static readonly AssistantOptions Website = new()
    {
        Provider = "gemini",
        Model = "gemini-2.5-flash",
        ApiKey = "website-key",
        BaseUrl = string.Empty,
    };

    [Fact]
    public void No_provider_of_its_own_borrows_the_whole_website_block()
    {
        var resolved = StaffAiRegistration.Resolve(new StaffAiOptions { Model = "ignored", MaxOutputTokens = 4096 }, Website);

        Assert.Equal(("gemini", "gemini-2.5-flash", "website-key"), (resolved.Provider, resolved.Model, resolved.ApiKey));
        Assert.Equal(4096, resolved.MaxOutputTokens);
    }

    [Fact]
    public void A_provider_of_its_own_takes_nothing_from_the_website_block()
    {
        var resolved = StaffAiRegistration.Resolve(
            new StaffAiOptions { Provider = "OpenAI", Model = "qwen2.5:7b", BaseUrl = "http://gpu.local:11434/v1" },
            Website);

        Assert.Equal("openai", resolved.Provider);
        Assert.Equal("qwen2.5:7b", resolved.Model);
        Assert.Equal(string.Empty, resolved.ApiKey);
        Assert.Equal("http://gpu.local:11434/v1", resolved.BaseUrl);
    }

    [Fact]
    public void Off_switches_the_staff_assistant_off_whatever_the_website_uses()
    {
        var resolved = StaffAiRegistration.Resolve(new StaffAiOptions { Provider = "off" }, Website);

        Assert.Equal(string.Empty, resolved.Provider);
        Assert.Equal(string.Empty, resolved.ApiKey);
    }

    [Fact]
    public void Nothing_set_anywhere_is_off()
    {
        Assert.Equal(string.Empty, StaffAiRegistration.Resolve(new StaffAiOptions(), new AssistantOptions()).Provider);
    }
}
