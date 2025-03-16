namespace SpotifyConsoleWorker.Models;

public class GetTracksResponse
{
    public int Total { get; set; }
    public IList<GetTracksSingleTrackResponse> Items { get; set; }
}

public class GetTracksSingleTrackResponse
{
    public Track Track { get; set; }
}

public class Track
{
    public string Uri { get; set; }
}