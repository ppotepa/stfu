using System.Security.Cryptography;
using System.Text;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveEvidenceHash
{
    public static string Compute(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Short(string value, int length = 12)
    {
        var hash = Compute(value);
        return hash[..Math.Clamp(length, 1, hash.Length)];
    }
}
