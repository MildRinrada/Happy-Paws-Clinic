using FluentValidation;

namespace HappyPaws.Application.Identity.StaffLogin;

/// <summary>
/// Staff sign in with an email address; customers use a phone number and OTP instead.
/// The user agent and IP are recorded on the session so a device list is recognisable and the
/// audit log can answer "from which IP".
/// </summary>
public sealed record StaffLoginCommand(string Email, string Password, string? UserAgent, string? IpAddress);

public sealed class StaffLoginValidator : AbstractValidator<StaffLoginCommand>
{
    public StaffLoginValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        // A length floor only — no composition rules. Forced symbol classes push people toward
        // predictable substitutions, and the real protections here are rate limiting and 2FA.
        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(256);
    }
}
