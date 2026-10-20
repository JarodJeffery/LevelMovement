using Microsoft.AspNetCore.Authentication;

namespace LevelMovement.Api.Security;

public static class ApiKeySecurityExtensions
{
    public static IServiceCollection AddApiKeySecurity(this IServiceCollection services)
    {
        services.AddOptions<ApiKeyOptions>()
            .BindConfiguration(ApiKeyOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "Security:ApiKey must be configured through user secrets or Security__ApiKey before starting the API.")
            .ValidateOnStart();

        services.AddAuthentication(ApiKeyOptions.Scheme)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyOptions.Scheme, _ => { });
        services.AddAuthorization();
        return services;
    }
}
