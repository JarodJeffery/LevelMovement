using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LevelMovement.Api.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ApiKeyOptions> apiKeyOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string FailureMessage = "A valid API key is required.";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyOptions.HeaderName, out var supplied)
            || supplied.Count != 1 || string.IsNullOrWhiteSpace(supplied[0]))
            return Task.FromResult(AuthenticateResult.Fail(FailureMessage));

        // Compare fixed-length digests without including either credential in diagnostics.
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKeyOptions.Value.ApiKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0]!));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash))
            return Task.FromResult(AuthenticateResult.Fail(FailureMessage));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "api-client")], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = Scheme.Name;
        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized", detail: FailureMessage).ExecuteAsync(Context);
    }
}
