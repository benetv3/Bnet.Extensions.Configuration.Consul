// Copyright (c) Bnet. All rights reserved.
// Derived from Winton.Extensions.Configuration.Consul, Copyright (c) Winton, licensed under the Apache License, Version 2.0.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;

namespace Bnet.Extensions.Configuration.Consul.Extensions;

/// <summary>
///     Thrown when a Consul key/value pair cannot be converted into a valid configuration key.
/// </summary>
public sealed class InvalidKeyPairException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InvalidKeyPairException" /> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidKeyPairException(string message)
        : base(message)
    {
    }
}
