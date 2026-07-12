// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     Represents a single item as returned by the Consul KV HTTP API.
/// </summary>
internal sealed class ConsulKvResponseItem
{
    /// <summary>
    ///     Gets or sets the fully qualified key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the Base64 encoded value, or null when the key has no value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    ///     Gets or sets the index at which the key was last modified.
    /// </summary>
    public ulong ModifyIndex { get; set; }
}
