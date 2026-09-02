using System.ComponentModel.DataAnnotations;

namespace PlanVest.Api.Infrastructure;

public sealed class RequestValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request is null) return await next(context);

        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(request, new ValidationContext(request), results, true))
            return await next(context);

        var errors = results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => new { member, message = result.ErrorMessage ?? "Invalid value." }))
            .GroupBy(value => value.member)
            .ToDictionary(group => group.Key, group => group.Select(value => value.message).ToArray());

        return Results.ValidationProblem(errors, title: "Request validation failed");
    }
}
