// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     Source generated <see cref="JsonSerializerContext" /> so that the Consul KV response can be
///     deserialized without reflection, making the library trimming and AOT compatible.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ConsulKvResponseItem[]))]
internal sealed partial class ConsulJsonSerializerContext : JsonSerializerContext;
