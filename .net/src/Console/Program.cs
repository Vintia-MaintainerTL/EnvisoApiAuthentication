using System.Text.Json;
using Library;
using Library.Models;

namespace EnvisoConsole;

internal class Program
{
    // Checked-in demo values from the original sample — confirmed dead against the staging
    // API (403 Forbidden), kept only so the expected shape of each value is still documented.
    private const string DefaultTenantSecretKey = "mosIgBkcR0qKeZenWmpE/A==";
    private const string DefaultApiKey = "L5MhJYSCp06SpYlI2cjbHg==";
    private const string DefaultPublicRsaKey = @"-----BEGIN PUBLIC KEY-----
MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQChrE9eekbvWaz7Rv80UWpq7lwz
zQlQOQoTU7OxEFDVsftVyHus/MLQbCbIgZoo3i16ocY5VKKjqP8EiCORP+CU5SBA
oLGfsgIRLqzPT+6DcWZckmkpZRfKd51O/6QByIFCwQKWYcrqrZDzJCGBiZSuv8rd
85RRfYuXSHNyachyvwIDAQAB
-----END PUBLIC KEY-----";

    private static async Task Main()
    {
        Console.WriteLine("Creating a login request for enviso.");

        string apiKey, rsaPublicKey, tenantSecretKey;
        if (PromptUseDemoCredentials())
        {
            apiKey = DefaultApiKey;
            rsaPublicKey = DefaultPublicRsaKey;
            tenantSecretKey = DefaultTenantSecretKey;
        }
        else
        {
            apiKey = PromptRequired("APIKEY");
            rsaPublicKey = PromptRequired("Public RSA Key");
            tenantSecretKey = PromptRequired("tenantsecret key");
        }

        await ExecuteSimpleCallAsync(apiKey, rsaPublicKey, tenantSecretKey);

        Console.ReadLine();
    }

    private static bool PromptUseDemoCredentials()
    {
        Console.WriteLine(
            "Use the checked-in demo credentials (known dead against staging as of the last " +
            "check) instead of entering your own? [y/N]");
        var response = Console.ReadLine();
        return string.Equals(response?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
    }

    private static string PromptRequired(string label)
    {
        while (true)
        {
            Console.WriteLine($"Please fill in your {label}:");
            var value = Console.ReadLine();
            if (value is null)
            {
                throw new InvalidOperationException($"No input available for {label} (stdin closed).");
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Console.WriteLine($"{label} cannot be blank.");
        }
    }

    public static async Task ExecuteSimpleCallAsync(string apiKey, string rsaPublicKey, string tenantSecretKey)
    {
        using var envisoClient = new EnvisoClient(apiKey, rsaPublicKey, tenantSecretKey);
        await envisoClient.InitializeAsync();

        var venues = await envisoClient.GetAsync<IEnumerable<VenueModelDTO>>(envisoClient.VenuesUri);
        Console.WriteLine($"Result of get venues is {JsonSerializer.Serialize(venues)}");
    }
}
