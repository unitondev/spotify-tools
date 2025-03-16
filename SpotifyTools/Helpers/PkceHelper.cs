using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SpotifyTools;

public static class PkceHelper
{
    public static string GenerateCodeVerifier(uint size = 128)
    {
        if (size < 43 || size > 128)
        {
            size = 128;
        }

        const string unreservedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        Random random = new Random();
        char[] highEntropyCryptograph = new char[size];

        for (int i = 0; i < highEntropyCryptograph.Length; i++)
        {
            highEntropyCryptograph[i] = unreservedCharacters[random.Next(unreservedCharacters.Length)];
        }

        return new string(highEntropyCryptograph);
    }

    public static string GenerateCodeChallenge(string codeVerifier) =>
        Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));
}