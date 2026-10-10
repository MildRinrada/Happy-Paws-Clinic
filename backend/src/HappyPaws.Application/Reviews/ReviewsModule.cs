using HappyPaws.Application.Reviews.Invitations;
using HappyPaws.Application.Reviews.Portal;
using HappyPaws.Application.Reviews.Public;
using HappyPaws.Application.Reviews.Staff;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Application.Reviews;

/// <summary>
/// Registers customer reviews (<c>docs/contract-extensions.md § 36</c>): writing one in the portal
/// or through an invitation link, the staff moderation desk, the public list, and the invitation
/// the <c>visit_completed</c> reminder carries.
/// </summary>
public static class ReviewsModule
{
    public static IServiceCollection AddReviewsModule(this IServiceCollection services)
    {
        services.AddScoped<ListPendingReviewsHandler>();
        services.AddScoped<SubmitPortalReviewHandler>();
        services.AddScoped<GetReviewInvitationHandler>();
        services.AddScoped<SubmitInvitationReviewHandler>();
        services.AddScoped<ReviewInvitationIssuer>();
        services.AddScoped<ReviewModeration>();
        services.AddScoped<ListStaffReviewsHandler>();
        services.AddScoped<PublishReviewHandler>();
        services.AddScoped<RejectReviewHandler>();
        services.AddScoped<ReplyToReviewHandler>();
        services.AddScoped<ListPublicReviewsHandler>();

        return services;
    }
}
