using System.Text.Json;
using SpotifyConsoleWorker.Models;
using SpotifyConsoleWorker.Services;
using SpotifyTools.Database;

namespace SpotifyConsoleWorker;

public class Worker : BackgroundService
{
    private readonly ImprovisedDatabase _improvisedDatabase;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ImprovisedDatabase improvisedDatabase, IServiceProvider serviceProvider)
    {
        _improvisedDatabase = improvisedDatabase;
        _serviceProvider = serviceProvider;
    }

    public async Task SavePlaylistAsync(PlaylistService playlistService)
    {
        GetMyPlaylistsSinglePlaylist selectedPlaylist = null;
        var paginationModel = new PaginationModel
        {
            Limit = 50,
            Offset = 0,
        };
        while (selectedPlaylist == null)
        {
            var playlists = await playlistService.GetPlaylistsList(paginationModel);
            Console.WriteLine($"Total items - {playlists.Total}, offset - {paginationModel.Offset}");
            for (int i = 0; i < playlists.Items.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {playlists.Items[i].Name}");
            }

            Console.WriteLine("[ - previous, ] - next, or choose index");
            var input = Console.ReadLine();
            switch (input.ToLower())
            {
                case "]":
                    paginationModel.Offset += paginationModel.Limit;
                    break;
                case "[":
                    if (paginationModel.Offset >= paginationModel.Limit)
                    {
                        paginationModel.Offset -= paginationModel.Limit;   
                    }
                    break;
                default:
                    if (int.TryParse(input, out int index))
                    {
                        if (index >= 0 && index < playlists.Items.Count)
                        {
                            var playlist = playlists.Items[index - 1];
                            if (playlist != null)
                            {
                                selectedPlaylist = playlist;
                            }
                        }
                    }
                    break;
            }
        }

        Console.WriteLine($"selected playlist {selectedPlaylist.Name}");
        Console.WriteLine("s - save all song links in playlist");
        var input2 = Console.ReadLine();
        if (string.Equals(input2, "s", StringComparison.InvariantCultureIgnoreCase))
        {
            var tracks = await playlistService.GetTracksAsync(selectedPlaylist);
            var json = JsonSerializer.Serialize(tracks.Select(r => r.Uri));
            await File.WriteAllTextAsync(
                $".{Path.DirectorySeparatorChar}json{Path.DirectorySeparatorChar}tracks_{selectedPlaylist.Name}_{DateTime.Now:dd_MM_yyyy}.json",
                json);
        }
    }

    public async Task CreatePlaylistAsync(PlaylistService playlistService)
    {
        var files = Directory.GetFiles($@".{Path.DirectorySeparatorChar}json", "*.json");
        for (var i = 0; i < files.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {files[i]}");
        }

        Console.WriteLine("choose json file");
        var input = Console.ReadLine();
        if (!int.TryParse(input, out int index) || index - 1 < 0 || index - 1 > files.Length)
        {
            Console.WriteLine("incorrect index");
            return;
        }
        
        var json = await File.ReadAllTextAsync(files[index - 1]);
        var tracksUri = JsonSerializer.Deserialize<List<string>>(json);
        if (tracksUri == null || tracksUri.Count == 0)
        {
            Console.WriteLine("incorrect json file");
            return;
        }
        Console.WriteLine("enter playlist name");
        var playlistName = Console.ReadLine();
        if (string.IsNullOrEmpty(playlistName))
        {
            Console.WriteLine("invalid playlist name");
            return;
        }
        var createdPlaylistId = await playlistService.CreatePlaylistAsync(playlistName);
        if (string.IsNullOrEmpty(createdPlaylistId))
        {
            Console.WriteLine("failed to create playlist");
            return;
        }
        
        var updateResult = await playlistService.UpdatePlaylistAsync(createdPlaylistId, tracksUri);
        if (!updateResult)
        {
            Console.WriteLine("failed to update playlist");
            return;
        }

        Console.WriteLine("done");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var PlaylistService = scope.ServiceProvider.GetRequiredService<PlaylistService>();
        while (true)
        {
            if (string.IsNullOrEmpty(_improvisedDatabase.Get(KnownDatabaseFields.AccessToken)))
            {
                Console.WriteLine("enter jwt");
                var jwt = Console.ReadLine();
                if (string.IsNullOrEmpty(jwt))
                {
                    Console.WriteLine("jwt is empty");
                    return;
                }

                _improvisedDatabase.Set(KnownDatabaseFields.AccessToken, jwt);
            }

            Console.WriteLine("1 - save playlist tracks to json, 2 - create playlist with json");
            var action = Console.ReadLine();
            if (int.TryParse(action, out var actionInt))
            {
                if (actionInt < 1 || actionInt > 2)
                {
                    Console.WriteLine("Invalid action");
                    return;
                }

                switch (actionInt)
                {
                    case 1:
                        await SavePlaylistAsync(PlaylistService);
                        break;
                    case 2:
                        await CreatePlaylistAsync(PlaylistService);
                        break;
                }
            }
        }
    }
}