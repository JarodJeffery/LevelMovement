namespace LevelMovement.Api.Security;

public sealed class ApiKeyOptions
{
    public const string SectionName = "Security";
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    public string ApiKey { get; set; } = string.Empty;
}
