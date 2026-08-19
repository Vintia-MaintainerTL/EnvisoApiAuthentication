using System.Text.Json;
using Library;
using Library.Models;

namespace EnvisoConsole;

internal class Program
{
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

        Console.WriteLine($"Please fill in your APIKEY: {Environment.NewLine} eg:{Environment.NewLine}{DefaultApiKey}");
        var apiKey = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = DefaultApiKey;
        }

        Console.WriteLine($"Please fill in your Public RSA Key: {Environment.NewLine}eg: {Environment.NewLine}{DefaultPublicRsaKey}");
        var rsaPublicKey = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(rsaPublicKey))
        {
            rsaPublicKey = DefaultPublicRsaKey;
        }

        Console.WriteLine($"Please fill in your tenantsecret key: {Environment.NewLine}eg: {Environment.NewLine}{DefaultTenantSecretKey}");
        var tenantSecretKey = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(tenantSecretKey))
        {
            tenantSecretKey = DefaultTenantSecretKey;
        }

        await ExecuteSimpleCallAsync(apiKey, rsaPublicKey, tenantSecretKey);

        Console.ReadLine();
    }

    public static async Task ExecuteSimpleCallAsync(string apiKey, string rsaPublicKey, string tenantSecretKey)
    {
        using var envisoClient = new EnvisoClient(apiKey, rsaPublicKey, tenantSecretKey);
        await envisoClient.InitializeAsync();

        var venues = await envisoClient.GetAsync<IEnumerable<VenueModelDTO>>(envisoClient.VenuesUri);
        Console.WriteLine($"Result of get venues is {JsonSerializer.Serialize(venues)}");
    }
}
