// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using TUnit.Core;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul.IntegrationTests;

public sealed class ReloadTests
{
    [ClassDataSource<ReloadApplication>]
    public required ReloadApplication Application { get; init; }

    [Test]
    public async Task ShouldReloadConfigurationWhenConsulValueChanges()
    {
        var client = Application.CreateClient();

        await Assert.That(await client.GetStringAsync("config/String")).IsEqualTo("Hello");

        await Application.UpdateAsync("""{ "String": "Updated" }""");

        var value = await WaitForValueAsync(client, "config/String", "Updated", TimeSpan.FromSeconds(30));

        await Assert.That(value).IsEqualTo("Updated");
    }

    private static async Task<string> WaitForValueAsync(
        HttpClient client,
        string path,
        string expected,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var value = await client.GetStringAsync(path);
        while (value != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200);
            value = await client.GetStringAsync(path);
        }

        return value;
    }
}

public sealed class ReloadApplication : ConsulApplication
{
    public const string Key = "reload/appsettings.json";

    private const string _value =
        """
        {
          "String": "Hello"
        }
        """;

    protected override KeyValuePair<string, string> GetKeyValue()
    {
        return new KeyValuePair<string, string>(Key, _value);
    }

    protected override void Configure(IConsulConfigurationSource options)
    {
        options.Parser = new JsonConfigurationParser();
        options.KeyToRemove = Key;
        options.ReloadOnChange = true;
    }
}
