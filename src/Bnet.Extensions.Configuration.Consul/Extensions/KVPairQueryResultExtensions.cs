// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Bnet.Extensions.Configuration.Consul.Extensions;

internal static class KvPairQueryResultExtensions
{
    internal static bool HasValue(this QueryResult<ConsulKvPair[]>? result)
    {
        return result != null
               && result.StatusCode != HttpStatusCode.NotFound
               && result.Response != null
               && result.Response.Any(kvp => kvp.HasValue());
    }

    internal static Dictionary<string, string?> ToConfigDictionary(
        this QueryResult<ConsulKvPair[]> result,
        Func<ConsulKvPair, IEnumerable<KeyValuePair<string, string?>>> convertConsulKvPairToConfig)
    {
        return (result.Response ?? [])
            .Where(kvp => kvp.HasValue())
            .SelectMany(convertConsulKvPairToConfig)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    }
}
