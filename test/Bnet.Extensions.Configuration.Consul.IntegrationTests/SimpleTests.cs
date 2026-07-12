// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using TUnit.Core;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul.IntegrationTests;

public sealed class SimpleTests
{
    [ClassDataSource<SimpleApplication>]
    public required SimpleApplication Application { get; init; }

    [Test]
    [Arguments("config/simple", "PlainString")]
    public async Task ShouldLoadConfigurationValueFromConsul(string path, string expected)
    {
        var client = Application.CreateClient();

        var value = await client.GetStringAsync(path);

        await Assert.That(value).IsEqualTo(expected);
    }
}

public sealed class SimpleApplication : ConsulApplication
{
    public const string Key = "simple";

    private const string _value = "PlainString";

    protected override KeyValuePair<string, string> GetKeyValue()
    {
        return new KeyValuePair<string, string>(Key, _value);
    }

    protected override void Configure(IConsulConfigurationSource options)
    {
        options.Parser = new SimpleConfigurationParser();
        options.KeyToRemove = string.Empty;
    }
}
