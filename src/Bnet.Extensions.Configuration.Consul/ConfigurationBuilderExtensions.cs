// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     Extensions for the <see cref="IConfigurationBuilder" /> that provide syntactic sugar for
///     using the <see cref="IConsulConfigurationSource" />.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <param name="builder">The builder to add consul to.</param>
    extension(IConfigurationBuilder builder)
    {
        /// <summary>
        ///     Adds Consul as a configuration source to the <see cref="IConfigurationBuilder" />
        ///     using the default settings in <see cref="IConsulConfigurationSource" />.
        /// </summary>
        /// <param name="key">The key in consul where the configuration is located.</param>
        /// <returns>The builder.</returns>
        public IConfigurationBuilder AddConsul(string key)
        {
            return builder.AddConsul(key, _ => { });
        }

        /// <summary>
        ///     Adds Consul as a configuration source to the <see cref="IConfigurationBuilder" />
        ///     and applies the given overrides to the <see cref="IConsulConfigurationSource" />.
        /// </summary>
        /// <param name="key">The key in consul where the configuration is located.</param>
        /// <param name="options">An action used to configure the options of the <see cref="IConsulConfigurationSource" />.</param>
        /// <returns>The builder.</returns>
        public IConfigurationBuilder AddConsul(string key,
            Action<IConsulConfigurationSource> options)
        {
            var consulConfigSource = new ConsulConfigurationSource(key);
            options(consulConfigSource);
            return builder.Add(consulConfigSource);
        }
    }
}
