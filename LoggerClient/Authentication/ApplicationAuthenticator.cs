using System.Net.Http.Headers;
using LoggerClient.Configurations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LoggerClient.Authentication;

public class ApplicationAuthenticator
{
    // One shared client for the process avoids socket exhaustion.
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    private readonly string _authEndpoint;
    private readonly string _token;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long? _cachedApplicationId;
    private DateTimeOffset _nextAttemptUtc = DateTimeOffset.MinValue;

    public ApplicationAuthenticator(LoggerClientConfiguration configuration)
    {
        _authEndpoint = configuration.AuthEndpoint.TrimEnd('/');
        _token = configuration.AppToken;
    }

    /// Returns the application ID, or null if authentication is unavailable.
    /// This method never throws.
    public async Task<long?> GetApplicationIdAsync()
    {
        if (_cachedApplicationId.HasValue) return _cachedApplicationId;
        if (DateTimeOffset.UtcNow < _nextAttemptUtc) return null;

        await _gate.WaitAsync();
        try
        {
            if (_cachedApplicationId.HasValue) return _cachedApplicationId;
            if (DateTimeOffset.UtcNow < _nextAttemptUtc) return null;

            _cachedApplicationId = await FetchApplicationIdAsync();
            if (_cachedApplicationId == null)
                _nextAttemptUtc = DateTimeOffset.UtcNow + RetryDelay;

            return _cachedApplicationId;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<long?> FetchApplicationIdAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_authEndpoint}/api/application/get-application-by-token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            using var response = await SharedClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"LoggerClient: authentication failed with status {(int)response.StatusCode}.");
                return null;
            }

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            return json["id"]?.Value<long>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonReaderException)
        {
            Console.WriteLine($"LoggerClient: authentication unavailable: {ex.Message}");
            return null;
        }
    }
}