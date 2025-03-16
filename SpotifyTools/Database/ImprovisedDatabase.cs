namespace SpotifyTools.Database;

public static class KnownDatabaseFields
{
    public const string CodeVerifier = "CodeVerifier";
    public const string AccessToken = "AccessToken";
    public const string RefreshToken = "RefreshToken";
}
public class ImprovisedDatabase
{
    private readonly Dictionary<string, string> _improvisedDatabase = new();

    public bool Set(string key, string value) => _improvisedDatabase.TryAdd(key, value);

    public string Get(string key)
    {
        var contains = _improvisedDatabase.TryGetValue(key, out var value);
        return contains ? value! : string.Empty;
    }

    public bool Remove(string key) => _improvisedDatabase.Remove(key);
}