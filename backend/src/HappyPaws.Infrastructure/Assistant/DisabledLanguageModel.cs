using HappyPaws.Application.Assistant;

namespace HappyPaws.Infrastructure.Assistant;

/// <summary>No provider configured: the assistant is off, and says so rather than failing.</summary>
internal sealed class DisabledLanguageModel : ILanguageModel
{
    public bool IsConfigured => false;

    public Task<ModelTurn> CompleteAsync(ModelRequest request, CancellationToken cancellationToken = default) =>
        throw new LanguageModelException("The assistant has no language model configured.");
}
