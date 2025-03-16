using SpotifyConsoleWorker;
using SpotifyConsoleWorker.Services;
using SpotifyTools.Database;
using SpotifyTools.Models;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptions();
builder.Services.Configure<SpotifySettings>(builder.Configuration.GetSection("SpotifySettings"));
builder.Services.AddHttpClient();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<ImprovisedDatabase>(); // i'm too lazy to connect sqllite, sorry
builder.Services.AddScoped<PlaylistService>();

var host = builder.Build();
host.Run();