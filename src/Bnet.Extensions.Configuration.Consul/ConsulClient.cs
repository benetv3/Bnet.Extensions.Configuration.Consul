// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bnet.Extensions.Configuration.Consul;

/// <inheritdoc />
/// <summary>
///     An <see cref="IConsulClient" /> implemented directly on top of <see cref="HttpClient" />.
///     It only depends on <c>System.Text.Json</c> source generation so that it remains
///     trimming and AOT compatible.
/// </summary>
internal sealed class ConsulClient : IConsulClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _datacenter;

    public ConsulClient(IConsulConfigurationSource source)
    {
        var options = new ConsulClientOptions();
        source.ConsulConfigurationOptions?.Invoke(options);

        var handler = new SocketsHttpHandler
        {
            // The client is reused for the lifetime of the provider, so allow connections to be
            // recycled periodically to pick up DNS changes.
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        source.ConsulHttpClientHandlerOptions?.Invoke(handler);

        _httpClient = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = EnsureTrailingSlash(options.Address)
        };

        if (!string.IsNullOrEmpty(options.Token))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Consul-Token", options.Token);
        }

        _datacenter = options.Datacenter;
        source.ConsulHttpClientOptions?.Invoke(_httpClient);
    }

    public async Task<QueryResult<ConsulKvPair[]>> List(
        string key,
        QueryOptions options,
        CancellationToken cancellationToken)
    {
        var requestUri = new StringBuilder($"v1/kv/{key}?recurse=true");
        if (!string.IsNullOrEmpty(_datacenter))
        {
            requestUri.Append($"&dc={_datacenter}");
        }

        if (options.WaitIndex > 0)
        {
            requestUri.Append($"&index={options.WaitIndex}");
            requestUri.Append($"&wait={(long)options.WaitTime.TotalMilliseconds}ms");
        }

        using var response =
            await _httpClient.GetAsync(requestUri.ToString(), cancellationToken).ConfigureAwait(false);

        var result = new QueryResult<ConsulKvPair[]> { StatusCode = response.StatusCode };
        SetLastIndex(response, result);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var items =
                await JsonSerializer
                    .DeserializeAsync(
                        stream,
                        ConsulJsonSerializerContext.Default.ConsulKvResponseItemArray,
                        cancellationToken)
                    .ConfigureAwait(false);

            result.Response = items?
                .Select(
                    item => new ConsulKvPair(item.Key)
                    {
                        Value = item.Value == null ? null : Convert.FromBase64String(item.Value)
                    })
                .ToArray() ?? [];
        }

        return result;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.ToString();
        return value.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri(value + "/");
    }

    private static void SetLastIndex(HttpResponseMessage response, QueryResult result)
    {
        if (!response.Headers.TryGetValues("X-Consul-Index", out var indexValues))
        {
            return;
        }

        foreach (var value in indexValues)
        {
            if (ulong.TryParse(value, out var index))
            {
                result.LastIndex = index;
                return;
            }
        }
    }
}
