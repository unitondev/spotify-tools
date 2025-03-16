using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SpotifyTools.Database;
using SpotifyTools.Models;

namespace SpotifyTools.Controllers;

/// <summary>
/// Auth controller
/// </summary>
[ApiController]
[Route("auth")]
public class AuthenticationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ImprovisedDatabase _improvisedDatabase;
    private readonly SpotifySettings _spotifySettings;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpClientFactory"></param>
    /// <param name="spotifySettings"></param>
    /// <param name="improvisedDatabase"></param>
    public AuthenticationController(IHttpClientFactory httpClientFactory, IOptions<SpotifySettings> spotifySettings, ImprovisedDatabase improvisedDatabase)
    {
        _httpClientFactory = httpClientFactory;
        _improvisedDatabase = improvisedDatabase;
        _spotifySettings = spotifySettings.Value;
    }

    /// <summary>
    /// This endpoint will give you a link to the spotify login, which you need to paste into your browser and log in.
    /// </summary>
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [HttpGet("login")]
    public IActionResult Login()
    {
        var codeVerifier = PkceHelper.GenerateCodeVerifier();
        _improvisedDatabase.Set(KnownDatabaseFields.CodeVerifier, codeVerifier);
        var codeChallenge = PkceHelper.GenerateCodeChallenge(codeVerifier);
        var parameters = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "client_id", _spotifySettings.ClientId },
            {
                "scope",
                "playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public"
            },
            { "code_challenge_method", "S256" },
            { "code_challenge", codeChallenge },
            { "redirect_uri", "https://localhost:7060/auth/callback" }
        };

        var url = new Uri(QueryHelpers.AddQueryString(_spotifySettings.AuthorizationEndpoint, parameters!)); 
        return Ok(url);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("callback")]
    public async Task<ActionResult> CallbackAsync([FromQuery] string? code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            return BadRequest(error);
        }

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", "https://localhost:7060/auth/callback" },
            { "client_id", _spotifySettings.ClientId },
            { "code_verifier", _improvisedDatabase.Get(KnownDatabaseFields.CodeVerifier) },
        };

        _improvisedDatabase.Remove(KnownDatabaseFields.CodeVerifier);
        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync(_spotifySettings.TokenEndpoint, new FormUrlEncodedContent(parameters));
        if (!response.IsSuccessStatusCode)
        {
            return BadRequest($"ErrorCode - {response.StatusCode}, Message - {await response.Content.ReadAsStringAsync()}");
        }

        var parsedResult = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (parsedResult == null)
        {
            return BadRequest("Failed to parse response");
        }

        _improvisedDatabase.Set(KnownDatabaseFields.AccessToken, parsedResult.AccessToken);
        _improvisedDatabase.Set(KnownDatabaseFields.RefreshToken, parsedResult.RefreshToken);
        
        return Ok("Success");
    }
}