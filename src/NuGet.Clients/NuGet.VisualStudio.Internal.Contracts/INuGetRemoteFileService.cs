// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.VisualStudio.Internal.Contracts
{
    public interface INuGetRemoteFileService : IDisposable
    {
        ValueTask<Stream?> GetRemoteFileAsync(Uri uri, CancellationToken cancellationToken);
    }
}
