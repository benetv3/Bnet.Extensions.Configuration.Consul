// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>A factory responsible for creating <see cref="IConsulClient" /> objects.</summary>
internal interface IConsulClientFactory
{
    /// <summary>Creates a new instance of an <see cref="IConsulClient" />.</summary>
    /// <returns>A new <see cref="IConsulClient" />.</returns>
    IConsulClient Create();
}
