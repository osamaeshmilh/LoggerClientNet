using LoggerClient.Configurations;
using Newtonsoft.Json.Linq;

namespace LoggerClient.Authentication;

public class ApplicationAuthenticator
{
    private readonly string _authEndpoint;

    public ApplicationAuthenticator(LoggerClientConfiguration configuration)
    {
        _authEndpoint = configuration.AuthEndpoint;
    }

    public async Task<long?> GetApplicationIdByTokenAsync(string token)
    {
        Console.WriteLine($"{_authEndpoint}/api/application/get-application-by-token?token={token}");
        using var httpClient = new HttpClient();
        var response =
            await httpClient.GetAsync($"{_authEndpoint}/api/application/get-application-by-token?token={token}");

        if (!response.IsSuccessStatusCode) return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var application = JObject.Parse(jsonResponse);
        return application["id"].Value<long>();
    }
}