// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Bnet.Extensions.Configuration.Consul.Parsers;

/// <inheritdoc />
/// <summary>
///     Implementation of <see cref="IConfigurationParser" /> for parsing JSON Configuration that
///     may either be a JSON object or a single primitive JSON value.
/// </summary>
public sealed class JsonValueConfigurationParser : IConfigurationParser
{
    /// <summary>
    /// Parses the JSON content from the stream into a dictionary of configuration key-value pairs.
    /// </summary>
    /// <param name="stream">The stream containing JSON configuration data.</param>
    /// <returns>A dictionary containing the parsed configuration key-value pairs.</returns>
    /// <example>
    /// For a JSON object like <c>{"database": {"host": "localhost"}}</c>, this returns a dictionary with
    /// the key "database:host" and value "localhost". For a primitive value like <c>"someValue"</c>,
    /// this returns a dictionary with an empty string key and value "someValue".
    /// </example>
    public IDictionary<string, string?> Parse(Stream stream)
    {
        MemoryStream ms;
        if (stream is MemoryStream existing)
        {
            ms = existing;
        }
        else
        {
            ms = new MemoryStream();
            stream.CopyTo(ms);
        }

        ms.Position = 0;
        var jsonElement = JsonDocument.Parse(ms).RootElement;

        ms.Position = 0;

        return jsonElement.ValueKind == JsonValueKind.Object
            ? new ConfigurationBuilder()
                .AddJsonStream(ms)
                .Build()
                .AsEnumerable()
                .ToDictionary(pair => pair.Key, pair => pair.Value)
            : new Dictionary<string, string?> { [""] = jsonElement.ToString() };
    }
}
