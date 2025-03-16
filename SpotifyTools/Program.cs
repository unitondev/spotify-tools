using SpotifyTools.Database;
using SpotifyTools.Models;
using SpotifyTools.Services;
using SpotifyTools.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddOptions();
builder.Services.AddHttpClient();
builder.Services.Configure<SpotifySettings>(builder.Configuration.GetSection("SpotifySettings"));
builder.Services.AddSingleton<ImprovisedDatabase>(); // i'm too lazy to connect sqllite, sorry
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IToolsService, ToolsService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
