using System.Security.Cryptography;
using System.Text;

namespace SentinelFleet.Infrastructure.Devices;

public static class DeviceApiKeyHasher
{
    public static string Hash(string apiKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hash);
    }
}
