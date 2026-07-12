// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     Options used to configure how the library connects to the Consul agent.
/// </summary>
public sealed class ConsulClientOptions
{
    /// <summary>
    ///     Gets or sets the address of the Consul agent.
    ///     Defaults to <c>http://localhost:8500</c>.
    /// </summary>
    public Uri Address { get; set; } = new Uri("http://localhost:8500");

    /// <summary>
    ///     Gets or sets the ACL token to send with each request.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    ///     Gets or sets the datacenter to query. When null the agent's default datacenter is used.
    /// </summary>
    public string? Datacenter { get; set; }
}
