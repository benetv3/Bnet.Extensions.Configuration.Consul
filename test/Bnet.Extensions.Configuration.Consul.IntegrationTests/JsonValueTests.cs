// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using TUnit.Core;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul.IntegrationTests;

public sealed class JsonValueTests
{
    [ClassDataSource<JsonValueApplication>]
    public required JsonValueApplication Application { get; init; }

    [Test]
    [Arguments("config/String", "Hello")]
    [Arguments("config/Nested:Child", "Value")]
    [Arguments("config/List:0", "First")]
    [Arguments("config/List:1", "Second")]
    [Arguments("config/Dictionary:KeyA", "1")]
    [Arguments("config/Dictionary:KeyB", "2")]
    public async Task ShouldLoadConfigurationValueFromConsul(string path, string expected)
    {
        var client = Application.CreateClient();

        var value = await client.GetStringAsync(path);

        await Assert.That(value).IsEqualTo(expected);
    }
}

public sealed class JsonValueApplication : ConsulApplication
{
    public const string Key = "jsonvalue";

    private const string _value =
        """
        {
          "String": "Hello",
          "Nested": { "Child": "Value" },
          "List": [ "First", "Second" ],
          "Dictionary": { "KeyA": "1", "KeyB": "2" }
        }
        """;

    protected override KeyValuePair<string, string> GetKeyValue()
    {
        return new KeyValuePair<string, string>(Key, _value);
    }

    protected override void Configure(IConsulConfigurationSource options)
    {
        options.Parser = new JsonValueConfigurationParser();
        options.KeyToRemove = Key;
    }
}
