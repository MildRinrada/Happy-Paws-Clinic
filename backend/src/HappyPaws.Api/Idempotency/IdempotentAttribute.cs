namespace HappyPaws.Api.Idempotency;

/// <summary>
/// Marks an endpoint as requiring <c>Idempotency-Key</c>, per the contract's rule that every POST
/// creating money, an appointment or a queue ticket must carry one.
/// <para>
/// Declared per endpoint rather than inferred from the route, so the requirement is visible on the
/// action that needs it and shows up in the generated OpenAPI document. The endpoints it belongs
/// on are listed in <c>docs/assumptions.md § 8</c>.
/// </para>
/// <code>
/// [HttpPost]
/// [Idempotent]
/// [RequireScope(Scopes.Appointment.Write)]
/// public Task&lt;ActionResult&lt;AppointmentDto&gt;&gt; Create(...)
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute;
