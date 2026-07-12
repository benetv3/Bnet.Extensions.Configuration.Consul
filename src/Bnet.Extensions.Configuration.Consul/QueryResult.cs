// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     The metadata returned from a query against the Consul KV store.
/// </summary>
internal class QueryResult
{
    /// <summary>
    ///     Gets or sets the HTTP status code returned by the Consul agent.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    ///     Gets or sets the index of the KV store at the time the query was served.
    /// </summary>
    public ulong LastIndex { get; set; }
}
