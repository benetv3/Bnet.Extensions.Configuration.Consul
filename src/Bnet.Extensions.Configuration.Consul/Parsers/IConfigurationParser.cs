// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Bnet.Extensions.Configuration.Consul.Parsers;

/// <summary>
///     Defines how the configuration loaded from Consul should be parsed.
/// </summary>
public interface IConfigurationParser
{
    /// <summary>
    ///     Parse the <see cref="Stream" /> into a dictionary.
    /// </summary>
    /// <param name="stream">The stream to parse.</param>
    /// <returns>A dictionary representing the configuration in a flattened form.</returns>
    IDictionary<string, string?> Parse(Stream stream);
}
