namespace SpotifyConsoleWorker.Models;

public class GetMyPlaylistsResponse
{
    /// <summary>
    /// The total number of available playlists
    /// </summary>
    public int Total { get; set; }

    public IList<GetMyPlaylistsSinglePlaylist> Items { get; set; }
}