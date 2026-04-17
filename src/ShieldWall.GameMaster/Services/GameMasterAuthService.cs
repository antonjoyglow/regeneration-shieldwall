namespace ShieldWall.GameMaster.Services;

/// <summary>
/// Simple token-based authentication for the Game Master facilitator endpoints.
/// Validates username/password from configuration and issues a session token.
/// </summary>
public sealed class GameMasterAuthService(IConfiguration configuration)
{
    private string? _activeToken;
    private readonly Lock _lock = new();

    /// <summary>
    /// Validates credentials against configuration. Returns a bearer token on success, null on failure.
    /// Subsequent successful logins return the same token (single GM session).
    /// </summary>
    public string? Login(string username, string password)
    {
        var expectedUsername = configuration["GameMaster:Username"];
        var expectedPassword = configuration["GameMaster:Password"];

        if (string.IsNullOrEmpty(expectedUsername) || string.IsNullOrEmpty(expectedPassword))
            return null;

        if (!string.Equals(username, expectedUsername, StringComparison.Ordinal) ||
            !string.Equals(password, expectedPassword, StringComparison.Ordinal))
            return null;

        lock (_lock)
        {
            _activeToken ??= Guid.NewGuid().ToString("N");
            return _activeToken;
        }
    }

    /// <summary>Returns true if the token matches the active session token.</summary>
    public bool ValidateToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        lock (_lock)
        {
            return _activeToken is not null
                && string.Equals(_activeToken, token, StringComparison.Ordinal);
        }
    }
}
