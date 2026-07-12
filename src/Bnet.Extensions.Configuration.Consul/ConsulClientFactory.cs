// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Bnet.Extensions.Configuration.Consul;

internal sealed class ConsulClientFactory(IConsulConfigurationSource consulConfigSource) : IConsulClientFactory
{
    public IConsulClient Create()
    {
        return new ConsulClient(consulConfigSource);
    }
}
