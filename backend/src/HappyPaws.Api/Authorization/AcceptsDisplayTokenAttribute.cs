namespace HappyPaws.Api.Authorization;

/// <summary>
/// Marks the routes a waiting-room screen's token may open: <c>GET /queue/display-board</c> and
/// <c>GET /queue/stream</c>, and no others (<c>docs/contract-extensions.md § 30</c>).
/// <para>
/// Allow-listed, not deny-listed. <see cref="DisplayTokenAuthenticationHandler"/> refuses a display
/// token on every route without this marker before authorization runs, so a route added later is
/// closed to screens until someone decides otherwise, and the decision is visible on the route.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class AcceptsDisplayTokenAttribute : Attribute;
