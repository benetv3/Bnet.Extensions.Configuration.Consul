using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using TUnit.Core;
using TUnit.Core.Interfaces;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Bnet.Extensions.Configuration.Consul.IntegrationTests;

/// <summary>
///     Base test host that seeds Consul and exposes an ASP.NET Core application whose configuration
///     is loaded from Consul. Each concrete scenario overrides the seed data and the source options.
/// </summary>
public abstract class ConsulApplication : IAsyncInitializer, IAsyncDisposable
{
    private WebApplication _app = null!;
    private HttpClient? _client;
    private string _key = null!;

    [ClassDataSource<ConsulServer>(Shared = SharedType.PerTestSession)]
    public required ConsulServer ConsulServer { get; init; }

    /// <summary>
    ///     The initial key/value pair written to Consul before the host starts.
    /// </summary>
    protected abstract KeyValuePair<string, string> GetKeyValue();

    /// <summary>
    ///     Applies the scenario-specific options (parser, key to remove, reload...) to the source.
    ///     Infrastructure options such as the Consul address are set by the base host beforehand.
    /// </summary>
    protected abstract void Configure(IConsulConfigurationSource options);

    public async Task InitializeAsync()
    {
        var (key, value) = GetKeyValue();
        _key = key;

        await ConsulServer.PutAsync(key, value);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddConsul(
            key,
            options =>
            {
                // Infrastructure concerns owned by the test host.
                options.PollWaitTime = TimeSpan.FromSeconds(1);
                options.ConsulConfigurationOptions = o => o.Address = ConsulServer.Address;

                // Scenario-specific configuration (parser, keys, reload...).
                Configure(options);
            });

        _app = builder.Build();
        _app.MapGet(
            "/config/{*configKey}",
            (string configKey, IConfiguration configuration) => configuration[configKey] ?? string.Empty);

        await _app.StartAsync();

        _client = _app.GetTestClient();
    }

    public HttpClient CreateClient()
    {
        return _client ??= _app.GetTestClient();
    }

    public Task UpdateAsync(string value)
    {
        return ConsulServer.PutAsync(_key, value);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        await _app.DisposeAsync();
    }
}
