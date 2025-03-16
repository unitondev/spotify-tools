using Microsoft.Extensions.Options;
using SpotifyTools.Database;
using SpotifyTools.Models;
using SpotifyTools.Services.Interfaces;

namespace SpotifyTools.Services;

public class AuthService : IAuthService
{
    private readonly ImprovisedDatabase _improvisedDatabase;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SpotifySettings _spotifySettings;

    public AuthService(ImprovisedDatabase improvisedDatabase, IHttpClientFactory httpClientFactory, IOptions<SpotifySettings> spotifySettings)
    {
        _improvisedDatabase = improvisedDatabase;
        _httpClientFactory = httpClientFactory;
        _spotifySettings = spotifySettings.Value;
    }
    
    public async Task<bool> RefreshTokenAsync()
    {
        var refreshToken = _improvisedDatabase.Get(KnownDatabaseFields.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return false;
        }

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", _spotifySettings.ClientId }
        };
        
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync(_spotifySettings.TokenEndpoint, new FormUrlEncodedContent(parameters));
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var parsedResult = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (parsedResult == null)
        {
            return false;
        }

        _improvisedDatabase.Set(KnownDatabaseFields.AccessToken, parsedResult.AccessToken);
        _improvisedDatabase.Set(KnownDatabaseFields.RefreshToken, parsedResult.RefreshToken);

        return true;
    }
}