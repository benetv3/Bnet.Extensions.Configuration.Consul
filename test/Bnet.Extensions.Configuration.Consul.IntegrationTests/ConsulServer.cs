using System.Text;
using DotNet.Testcontainers.Builders;
using TUnit.Core.Interfaces;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace Bnet.Extensions.Configuration.Consul.IntegrationTests;

public sealed class ConsulServer : IAsyncInitializer, IAsyncDisposable
{
    private readonly IContainer _server = new ContainerBuilder("hashicorp/consul:1.15")
        .WithPortBinding(8500, true)
        .WithWaitStrategy(
            Wait
                .ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(8500).ForPath("/v1/status/leader")))
        .Build();

    public Uri Address { get; private set; } = null!;

    private readonly HttpClient _client = new();

    public async Task InitializeAsync()
    {
        await _server.StartAsync();

        Address = new UriBuilder("http", _server.Hostname, _server.GetMappedPublicPort(8500)).Uri;
        _client.BaseAddress = Address;
    }

    public async Task PutAsync(string key, string value)
    {
        using var content = new StringContent(value, Encoding.UTF8);
        var response = await _client.PutAsync($"v1/kv/{key}", content);
        response.EnsureSuccessStatusCode();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _server.DisposeAsync();
    }
}
