// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using NuGet.Common;
using NuGet.PackageManagement.UI;
using NuGet.Packaging;
using NuGet.VisualStudio.Internal.Contracts;

namespace NuGet.PackageManagement.VisualStudio
{
    public sealed class NuGetRemoteFileService : INuGetRemoteFileService
    {
        private ServiceActivationOptions? _options;
        private IServiceBroker _serviceBroker;
        private AuthorizationServiceClient? _authorizationServiceClient;

        public NuGetRemoteFileService(
            ServiceActivationOptions options,
            IServiceBroker serviceBroker,
            AuthorizationServiceClient authorizationServiceClient)
        {
            _options = options;
            _serviceBroker = serviceBroker;
            _authorizationServiceClient = authorizationServiceClient;

            Assumes.NotNull(_serviceBroker);
            Assumes.NotNull(_authorizationServiceClient);
        }

        public NuGetRemoteFileService(IServiceBroker serviceBroker)
        {
            _serviceBroker = serviceBroker;
            Assumes.NotNull(_serviceBroker);
        }
        public async ValueTask<(Stream?, IconBitmapStatus)> GetRemoteFileAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (IsEmbeddedUri(uri))
            {
                string packagePath = uri.LocalPath;
                if (File.Exists(packagePath))
                {
                    string fileRelativePath = PathUtility.StripLeadingDirectorySeparators(
                    Uri.UnescapeDataString(uri.Fragment)
                        .Substring(1)); // Substring skips the '#' in the URI fragment

                    //TODO: protect against zip slip attack
                    string extractedIconPath = Path.Combine(Path.GetDirectoryName(packagePath), fileRelativePath);

                    if (File.Exists(extractedIconPath))
                    {
                        Stream fileStream = new FileStream(extractedIconPath, FileMode.Open);
                        return (fileStream, IconBitmapStatus.EmbeddedIcon);
                    }
                    else
                    {
                        try
                        {
                            using (PackageArchiveReader reader = new PackageArchiveReader(packagePath))
                            using (Stream parStream = await reader.GetStreamAsync(fileRelativePath, cancellationToken))
                            {
                                var memoryStream = new MemoryStream();
                                await parStream.CopyToAsync(memoryStream);
                                return (memoryStream, IconBitmapStatus.EmbeddedIcon);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            return (null, IconBitmapStatus.DefaultIconDueToNullStream);
                        }
                    }
                }
                else
                {
                    return (null, IconBitmapStatus.DefaultIconDueToNullStream);
                }
            }
            else
            {
                Stream? stream = await GetStream(uri);
                return (stream, stream == null ? IconBitmapStatus.DefaultIconDueToNullStream : IconBitmapStatus.DownloadedIcon);
            }
        }

        private async Task<Stream?> GetStream(Uri uri)
        {
            // BitmapImage can download on its own from URIs, but in order
            // to support downloading on a worker thread, we need to download the image
            // data and put into a memorystream. Then have the BitmapImage decode the
            // image from the memorystream.

            byte[]? imageData = null;
            MemoryStream? memoryStream = null;

            if (uri.IsFile)
            {
                if (File.Exists(uri.LocalPath))
                {
                    memoryStream = new MemoryStream(File.ReadAllBytes(uri.LocalPath));
                    return memoryStream;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        imageData = await httpClient.GetByteArrayAsync(uri);

#pragma warning disable CA2000 // Dispose objects before losing scope - stream needs to be disposed by caller.
                        memoryStream = new MemoryStream(imageData, writable: false);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    }
                    catch (HttpRequestException)
                    {
                        return null;
                    }
                    catch (TaskCanceledException)
                    {
                        return null;
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                }

                return memoryStream;
            }
        }

        /// <summary>
        /// NuGet Embedded Uri verification
        /// </summary>
        /// <param name="uri">An URI to test</param>
        /// <returns><c>true</c> if <c>uri</c> is an URI to an embedded file in a NuGet package</returns>
        public static bool IsEmbeddedUri(Uri uri)
        {
            return uri != null
                && uri.IsAbsoluteUri
                && uri.IsFile
                && !string.IsNullOrEmpty(uri.Fragment)
                && uri.Fragment.Length > 1;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~NuGetRemoteFileService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
