using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace VCollab.Utils;

public static class RoomTokenUtils
{
    public const int TokenLength = 8 + TokenDataLength; // vcollab- (8) + token data
    public const string TokenPrefix = "vcollab-";

    private const string TokenCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRTSUVWXYZ0123456789";
    private const int TokenDataLength = 10;

    public static string GenerateToken()
    {
        var token = RandomNumberGenerator.GetItems(TokenCharacters.AsSpan(), TokenDataLength);

        return $"{TokenPrefix}{new string(token)}";
    }

    public static bool IsValidToken([NotNullWhen(true)] string? token)
        => token?.Length is TokenLength && token.StartsWith(TokenPrefix);
}