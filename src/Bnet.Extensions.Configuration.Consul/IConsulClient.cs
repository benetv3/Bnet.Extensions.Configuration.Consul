// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>An HTTP client for querying the Consul KV store.</summary>
internal interface IConsulClient : IDisposable
{
    /// <summary>
    ///     Lists all key/value pairs under the given key, optionally blocking until a change occurs.
    /// </summary>
    /// <param name="key">The root key to list.</param>
    /// <param name="options">The options controlling the query.</param>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>The result of the query.</returns>
    Task<QueryResult<ConsulKvPair[]>> List(string key, QueryOptions options, CancellationToken cancellationToken);
}
