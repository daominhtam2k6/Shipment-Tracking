using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Tracking.Contracts;

public static class TrackingCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private static readonly Regex Pattern =
        new(@"^VN[0-9]{6}[A-HJ-NP-Z2-9]{6}\z", RegexOptions.CultureInvariant);

    public static string Generate(DateTimeOffset createdAt)
    {
        string date = createdAt.ToUniversalTime().ToString("yyMMdd", CultureInfo.InvariantCulture);
        char[] suffix = new char[6];

        for (int i = 0; i < suffix.Length; i++)
        {
            int randomIndex = RandomNumberGenerator.GetInt32(Alphabet.Length);
            suffix[i] = Alphabet[randomIndex];
        }

        return "VN" + date + new string(suffix);
    }

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var candidate = input.Trim().ToUpperInvariant();
        if (!Pattern.IsMatch(candidate))
            return false;

        normalized = candidate;
        return true;
    }
}