// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace Bnet.Extensions.Configuration.Consul;

/// <summary>
///     Contains information about exceptions that occur during a configuration load from Consul.
/// </summary>
public sealed class ConsulLoadExceptionContext
{
    internal ConsulLoadExceptionContext(IConsulConfigurationSource source, Exception exception)
    {
        Source = source;
        Exception = exception;
    }

    /// <summary>
    ///     Gets the <see cref="Exception" /> that occured in Load.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether the exception should be ignored.
    ///     Set to true to prevent the exception from being thrown.
    ///     I.e. if the exception has been handled.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    ///     Gets the <see cref="IConsulConfigurationSource" /> of the provider that caused the exception.
    /// </summary>
    public IConsulConfigurationSource Source { get; }
}
