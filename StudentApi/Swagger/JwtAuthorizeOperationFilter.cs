using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StudentApi.Swagger;

/// <summary>Show the lock icon only for actions requiring JWT, not anonymous actions.</summary>
public sealed class JwtAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var controller = method.DeclaringType;
        if (Attribute.IsDefined(method, typeof(AllowAnonymousAttribute), true)
            || (controller is not null && Attribute.IsDefined(controller, typeof(AllowAnonymousAttribute), true)))
            return;

        var requiresAuth = Attribute.IsDefined(method, typeof(AuthorizeAttribute), true)
            || (controller is not null && Attribute.IsDefined(controller, typeof(AuthorizeAttribute), true));
        if (!requiresAuth)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    }
}
