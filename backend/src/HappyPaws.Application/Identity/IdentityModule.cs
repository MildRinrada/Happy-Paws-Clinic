using HappyPaws.Application.Identity.Capabilities;
using HappyPaws.Application.Identity.ChangePassword;
using HappyPaws.Application.Identity.EnrolTwoFactor;
using HappyPaws.Application.Identity.GetMe;
using HappyPaws.Application.Identity.Login;
using HappyPaws.Application.Identity.Logout;
using HappyPaws.Application.Identity.ManageSessions;
using HappyPaws.Application.Identity.RefreshSession;
using HappyPaws.Application.Identity.Register;
using HappyPaws.Application.Identity.RequestOtp;
using HappyPaws.Application.Identity.ResetCredentials;
using HappyPaws.Application.Identity.ResetPassword;
using HappyPaws.Application.Identity.Sessions;
using HappyPaws.Application.Identity.SetUserStatus;
using HappyPaws.Application.Identity.StaffLogin;
using HappyPaws.Application.Identity.UpdateProfile;
using HappyPaws.Application.Identity.VerifyTwoFactor;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Identity;

/// <summary>
/// Registers this module's use cases. Each module owns its own registration file, so
/// <c>DependencyInjection</c> never becomes a list of every handler in the system.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddScoped<EffectiveScopeResolver>();
        services.AddScoped<SessionIssuer>();

        services.AddScoped<StaffLoginHandler>();
        services.AddScoped<VerifyTwoFactorHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<GetMeHandler>();
        services.AddScoped<ListSessionsHandler>();
        services.AddScoped<RevokeSessionHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<EnrolTwoFactorHandler>();
        services.AddScoped<SetUserStatusHandler>();

        // Customer sign-up and sign-in, and the reset link both audiences share.
        services.AddScoped<OtpVerifier>();
        services.AddScoped<RequestOtpHandler>();
        services.AddScoped<CustomerRecordLinker>();
        services.AddScoped<RegisterCustomerHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<ResetPasswordHandler>();

        // Operator commands — run from the server's command line, never mapped to a route.
        services.AddScoped<OperatorCredentials>();
        services.AddScoped<ResetCredentialsHandler>();

        return services;
    }
}
