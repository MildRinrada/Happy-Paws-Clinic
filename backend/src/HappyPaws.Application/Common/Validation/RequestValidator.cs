using FluentValidation;
using HappyPaws.Application.Common.Errors;

namespace HappyPaws.Application.Common.Validation;

/// <summary>
/// Runs the FluentValidation validator registered for a request and converts failures into the
/// shared error envelope.
/// <para>
/// Use cases call this as their first statement. Validation is deliberately <b>not</b> a hidden
/// pipeline behaviour: a reader of a handler can see that the input was checked, and a handler
/// with no validator does not silently look identical to one whose validator was forgotten.
/// </para>
/// <para>
/// A request with no registered validator passes. That is intentional for genuinely shapeless
/// inputs (a plain id in the route); anything with a body should have one.
/// </para>
/// </summary>
public sealed class RequestValidator(IServiceProvider services)
{
    public async Task ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        if (services.GetService(typeof(IValidator<TRequest>)) is not IValidator<TRequest> validator)
        {
            return;
        }

        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(failure => ToFieldName(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        throw new ValidationFailedException(errors);
    }

    /// <summary>
    /// <c>StartsAt</c> → <c>starts_at</c>. The envelope's <c>field</c> must name the field as the
    /// client sent it, not as the C# property is spelled.
    /// </summary>
    private static string ToFieldName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }

        var builder = new System.Text.StringBuilder(propertyName.Length + 8);

        for (var i = 0; i < propertyName.Length; i++)
        {
            var c = propertyName[i];

            if (char.IsUpper(c))
            {
                if (i > 0 && propertyName[i - 1] != '.')
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
