// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     Options that control a single query against the Consul KV store.
/// </summary>
internal sealed class QueryOptions
{
    /// <summary>
    ///     Gets or sets the index used for blocking queries.
    ///     When greater than zero the request blocks until the KV store changes or <see cref="WaitTime" /> elapses.
    /// </summary>
    public ulong WaitIndex { get; init; }

    /// <summary>
    ///     Gets or sets the maximum amount of time a blocking query should wait for a change.
    /// </summary>
    public TimeSpan WaitTime { get; init; }
}
