using System.Globalization;
using System.Security.Cryptography;

namespace Tracking.Api;

public static class TrackingCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate(DateTimeOffset createdAt)
    {
        string date = createdAt.ToString("yyMMdd", CultureInfo.InvariantCulture);
        char[] suffix = new char[6];

        for (int i = 0; i < suffix.Length; i++)
        {
            int randomIndex = RandomNumberGenerator.GetInt32(Alphabet.Length);
            suffix[i] = Alphabet[randomIndex];
        }

        return "VN" + date + new string(suffix);
    }
}   