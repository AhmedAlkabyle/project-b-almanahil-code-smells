using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AlmanahilAPI.Swagger;

/// <summary>
/// Adds the "Bearer" security requirement to every operation that is protected by
/// <see cref="AuthorizeAttribute"/> (unless it also has <see cref="AllowAnonymousAttribute"/>).
///
/// This makes Swagger UI actually send the "Authorization: Bearer …" header to those
/// endpoints after you click Authorize. A per-operation requirement is more reliable
/// than a single global requirement, which was not attaching the token consistently.
/// </summary>
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Look for [Authorize] on either the action method or its controller.
        var hasAuthorize =
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
            || (context.MethodInfo.DeclaringType?
                    .GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false);

        // [AllowAnonymous] on the method wins over any [Authorize].
        var allowAnonymous =
            context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

        if (!hasAuthorize || allowAnonymous)
            return;

        // Attach a requirement referencing the "Bearer" scheme defined in Program.cs.
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
        });
    }
}
