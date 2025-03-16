namespace SpotifyTools.Services.Interfaces;

public interface IAuthService
{
    Task<bool> RefreshTokenAsync();
}