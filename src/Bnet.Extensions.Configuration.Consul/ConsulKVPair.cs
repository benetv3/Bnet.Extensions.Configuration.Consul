// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     Represents a single key/value pair stored in Consul.
/// </summary>
public sealed class ConsulKvPair
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsulKvPair" /> class.
    /// </summary>
    /// <param name="key">The fully qualified key in Consul.</param>
    public ConsulKvPair(string key)
    {
        Key = key;
    }

    /// <summary>
    ///     Gets the fully qualified key in Consul.
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     Gets or sets the raw value stored under the key.
    /// </summary>
#pragma warning disable SA1011 // False positive for nullable arrays in StyleCop 1.1.118
    public byte[]? Value { get; set; }
#pragma warning restore SA1011
}
