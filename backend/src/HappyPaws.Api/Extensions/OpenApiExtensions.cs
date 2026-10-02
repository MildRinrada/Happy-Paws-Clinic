using HappyPaws.Api.Authorization;
using HappyPaws.Api.Idempotency;
using Microsoft.OpenApi;

namespace HappyPaws.Api.Extensions;

/// <summary>
/// OpenAPI 3.1 document generation, matching the header of the API reference.
/// <para>
/// The transformers below make the generated document describe what the API actually enforces:
/// which endpoints need a bearer token, which scopes each one requires, and which ones demand an
/// <c>Idempotency-Key</c>. Generating that from the same attributes that enforce it means the
/// document cannot drift from the behaviour.
/// </para>
/// </summary>
public static class OpenApiExtensions
{
    private const string BearerScheme = "bearer";

    public static IServiceCollection AddClinicOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "PetCare OS · REST API",
                    Version = "v1",
                    Description = "สัญญา API สำหรับคลินิกหนึ่ง instance. Times are ISO 8601 with offset "
                        + "(Asia/Bangkok); money is an integer number of satang.",
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = BearerScheme,
                    BearerFormat = "JWT",
                    Description = "Access token from /auth/login or /auth/2fa/verify. Lifetime 15 minutes.",
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;

                var requiredScopes = metadata.OfType<RequireScopeAttribute>()
                    .Select(attribute => attribute.Scope)
                    .ToArray();

                if (requiredScopes.Length > 0)
                {
                    operation.Description = string.Join(
                        "\n\n",
                        new[] { operation.Description, "Required scopes: " + string.Join(", ", requiredScopes) }
                            .Where(part => !string.IsNullOrWhiteSpace(part)));
                }

                if (metadata.OfType<IdempotentAttribute>().Any())
                {
                    operation.Parameters ??= [];
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = "Idempotency-Key",
                        In = ParameterLocation.Header,
                        Required = true,
                        Description = "UUID. Required: retrying without one may double-book or double-charge.",
                    });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
