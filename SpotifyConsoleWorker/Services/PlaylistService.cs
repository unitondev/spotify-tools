using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SpotifyConsoleWorker.Models;
using SpotifyTools.Database;
using SpotifyTools.Models;

namespace SpotifyConsoleWorker.Services;

public class PlaylistService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SpotifySettings _spotifySettings;
    private readonly ImprovisedDatabase _improvisedDatabase;

    public PlaylistService(IHttpClientFactory httpClientFactory,
        ImprovisedDatabase improvisedDatabase,
        IOptions<SpotifySettings> spotifySettings)
    {
        _httpClientFactory = httpClientFactory;
        _improvisedDatabase = improvisedDatabase;
        _spotifySettings = spotifySettings.Value;
    }

    public async Task<GetMyPlaylistsResponse> GetPlaylistsList(PaginationModel paginationModel)
    {
        using var httpClient = _httpClientFactory.CreateClient();

        var parameters = new Dictionary<string, string>()
        {
            { "limit", $"{paginationModel.Limit}" },
            { "offset", $"{paginationModel.Offset}" }
        };
        var getMyPlaylistsUrl =
            new Uri(QueryHelpers.AddQueryString($"{_spotifySettings.ApiEndpoint}/me/playlists", parameters!));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            _improvisedDatabase.Get(KnownDatabaseFields.AccessToken));
        var httpResponseMessage = await httpClient.GetAsync(getMyPlaylistsUrl);
        
        var result = await httpResponseMessage.Content.ReadFromJsonAsync<GetMyPlaylistsResponse>();
        return result;
    }

    public async Task<List<Track>> GetTracksAsync(GetMyPlaylistsSinglePlaylist playlist)
    {
        var pagination = new PaginationModel
        {
            Limit = 50,
            Offset = 0,
        };
        
        var totalResult = new List<Track>();
        var total = 0;

        do
        {
            var result = await GetTracksAsync(playlist, pagination);
            totalResult.AddRange(result.Items.Select(i => i.Track));
            total = result.Total;
            pagination.Offset += pagination.Limit;
        } while (totalResult.Count < total);

        return totalResult;
    }

    public async Task<GetTracksResponse> GetTracksAsync(GetMyPlaylistsSinglePlaylist playlist, PaginationModel pagination)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        
        var parameters = new Dictionary<string, string>()
        {
            { "limit", $"{pagination.Limit}" },
            { "offset", $"{pagination.Offset}" },
            { "fields", "total, items(track(name,href,id, uri))" }
        };
        var uri = new Uri(QueryHelpers.AddQueryString($"{_spotifySettings.ApiEndpoint}/playlists/{playlist.Id}/tracks", parameters!));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            _improvisedDatabase.Get(KnownDatabaseFields.AccessToken));
        var httpResponseMessage = await httpClient.GetAsync(uri);

        return await httpResponseMessage.Content.ReadFromJsonAsync<GetTracksResponse>();
    }

    public async Task<string> CreatePlaylistAsync(string name)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            _improvisedDatabase.Get(KnownDatabaseFields.AccessToken));

        var httpResponseMessage = await httpClient.GetAsync($"{_spotifySettings.ApiEndpoint}/me");
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        var me = await httpResponseMessage.Content.ReadFromJsonAsync<GetMeResponse>();
        if (me == null)
        {
            return string.Empty;
        }

        var payload = new
        {
            name = name,
        };
        
        var httpResponseMessage2 = await httpClient.PostAsJsonAsync($"{_spotifySettings.ApiEndpoint}/users/{me.Id}/playlists", payload);
        if (!httpResponseMessage2.IsSuccessStatusCode)
        {
            var text = await httpResponseMessage2.Content.ReadAsStringAsync();
            Console.WriteLine($"status code: {httpResponseMessage2.StatusCode}, reason: {text}");
        }
        var response = await httpResponseMessage2.Content.ReadFromJsonAsync<CreatePlaylistResponse>();
        return response?.Id ?? string.Empty;
    }

    public async Task<bool> UpdatePlaylistAsync(string id, IList<string> trackUris)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            _improvisedDatabase.Get(KnownDatabaseFields.AccessToken));

        if (trackUris.Count > 100)
        {
            var left = trackUris.Count;
            var skip = 0;
            while (left > 0)
            {
                var batch = trackUris.Skip(skip).Take(100).ToList();
                var httpResponseMessage = await httpClient.PostAsJsonAsync($"{_spotifySettings.ApiEndpoint}/playlists/{id}/tracks", new
                {
                    uris = batch
                });
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    var text = await httpResponseMessage.Content.ReadAsStringAsync();
                    Console.WriteLine($"status code: {httpResponseMessage.StatusCode}, reason: {text}");
                }
                skip += batch.Count;
                left -= batch.Count;
            }
        }

        return true;
    }
}