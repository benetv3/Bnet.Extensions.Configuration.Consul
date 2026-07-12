// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bnet.Extensions.Configuration.Consul.Parsers;

namespace Bnet.Extensions.Configuration.Consul.Extensions;

internal static class KvPairExtensions
{
    extension(ConsulKvPair kvPair)
    {
        internal IEnumerable<KeyValuePair<string, string?>> ConvertToConfig(string keyToRemove,
            IConfigurationParser parser)
        {
            using Stream stream = new MemoryStream(kvPair.Value!);
            return parser
                .Parse(stream)
                .Select(
                    pair =>
                    {
                        var key = $"{kvPair.Key.RemoveStart(keyToRemove).TrimEnd('/').Replace('/', ':')}:{pair.Key}"
                            .Trim(':');
                        if (string.IsNullOrEmpty(key))
                        {
                            throw new InvalidKeyPairException(
                                "The key must not be null or empty. Ensure that there is at least one key under the root of the config or that the data there contains more than just a single value.");
                        }

                        return new KeyValuePair<string, string?>(key, pair.Value);
                    });
        }

        internal bool HasValue()
        {
            return kvPair.IsLeafNode() && kvPair.Value != null && kvPair.Value.Length != 0;
        }

        internal bool IsLeafNode()
        {
            return !kvPair.Key.EndsWith("/");
        }
    }

    private static string RemoveStart(this string s, string toRemove)
    {
        return s.StartsWith(toRemove) ? s.Remove(0, toRemove.Length) : s;
    }
}
