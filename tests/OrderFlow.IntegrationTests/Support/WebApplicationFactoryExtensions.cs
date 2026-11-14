using System.Net.Http.Headers;

using Microsoft.AspNetCore.Mvc.Testing;

namespace OrderFlow.IntegrationTests.Support;

/// <summary>
/// Builds a client as a given caller for any factory, including the copies made with WithWebHostBuilder.
/// </summary>
internal static class WebApplicationFactoryExtensions
{
    public static HttpClient CreateClientFor(this WebApplicationFactory<Program> factory, string? token)
    {
        var client = factory.CreateClient();

        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }
}