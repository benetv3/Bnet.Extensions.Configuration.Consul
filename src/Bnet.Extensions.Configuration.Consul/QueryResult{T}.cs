// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     The result of a query against the Consul KV store.
/// </summary>
/// <typeparam name="T">The type of the response payload.</typeparam>
internal sealed class QueryResult<T> : QueryResult
{
    /// <summary>
    ///     Gets or sets the response payload.
    /// </summary>
    public T? Response { get; set; }
}
