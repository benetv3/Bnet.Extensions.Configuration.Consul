// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Bnet.Extensions.Configuration.Consul.Extensions;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul;

internal sealed class ConsulConfigurationSource : IConsulConfigurationSource
{
    private string? _keyToRemove;

    public ConsulConfigurationSource(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        Key = key;
        Parser = new JsonConfigurationParser();
        ConvertConsulKvPairToConfig = DefaultConvertConsulKvPairToConfigStrategy;
    }

    public Action<ConsulClientOptions>? ConsulConfigurationOptions { get; set; }

    public Action<SocketsHttpHandler>? ConsulHttpClientHandlerOptions { get; set; }

    public Action<HttpClient>? ConsulHttpClientOptions { get; set; }

    public string Key { get; }

    public string KeyToRemove
    {
        get => _keyToRemove ?? Key;
        set => _keyToRemove = value;
    }

    public Func<ConsulKvPair, IEnumerable<KeyValuePair<string, string?>>> ConvertConsulKvPairToConfig { get; set; }

    public Action<ConsulLoadExceptionContext>? OnLoadException { get; set; }

    public Func<ConsulWatchExceptionContext, TimeSpan>? OnWatchException { get; set; }

    public CancellationTokenSource? WatchCancellationTokenSource { get; set; }

    public bool Optional { get; set; } = false;

    public IConfigurationParser Parser { get; set; }

    public TimeSpan PollWaitTime { get; set; } = TimeSpan.FromMinutes(5);

    public bool ReloadOnChange { get; set; } = false;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var consulClientFactory = new ConsulClientFactory(this);
        return new ConsulConfigurationProvider(this, consulClientFactory);
    }

    private IEnumerable<KeyValuePair<string, string?>> DefaultConvertConsulKvPairToConfigStrategy(ConsulKvPair consulKvPair)
    {
        return consulKvPair.ConvertToConfig(KeyToRemove, Parser);
    }
}
