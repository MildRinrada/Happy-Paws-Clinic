using HappyPaws.Application.Assistant;

namespace HappyPaws.Application.StaffAi;

/// <summary>
/// The model behind the staff AI assistant: the same port and the same adapters as the website
/// assistant (<see cref="ILanguageModel"/>), configured from its own <c>StaffAi:*</c> section, which
/// falls back to <c>Assistant:*</c> when it names no provider. A separate type only so the two can
/// point at different models or keys; nothing about the protocol differs.
/// </summary>
public interface IStaffLanguageModel : ILanguageModel;
