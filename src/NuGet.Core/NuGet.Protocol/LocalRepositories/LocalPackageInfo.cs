// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace NuGet.Protocol
{
    public class LocalPackageInfo
    {
        private readonly Lazy<NuspecReader> _nuspecHelper;

        [Obsolete("Please move to ")]
        public LocalPackageInfo(
            PackageIdentity identity,
            string path,
            DateTime lastWriteTimeUtc,
            Lazy<NuspecReader> nuspec,
            Func<PackageReaderBase> getPackageReader) : this(identity, path, lastWriteTimeUtc, nuspec)
        {
            // TODO: becompat.    
        }

            /// <summary>
            /// Local nuget package.
            /// </summary>
            /// <param name="identity">Package id and version.</param>
            /// <param name="path">Path to the nupkg.</param>
            /// <param name="lastWriteTimeUtc">Last nupkg write time for publish date.</param>
            /// <param name="nuspec">Nuspec XML.</param>
            public LocalPackageInfo(
            PackageIdentity identity,
            string path,
            DateTime lastWriteTimeUtc,
            Lazy<NuspecReader> nuspec)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (nuspec == null)
            {
                throw new ArgumentNullException(nameof(nuspec));
            }

            Identity = identity;
            Path = path;
            LastWriteTimeUtc = lastWriteTimeUtc;
            _nuspecHelper = nuspec;
        }

        protected LocalPackageInfo()
        {

        }

        /// <summary>
        /// Package id and version.
        /// </summary>
        public virtual PackageIdentity Identity { get; }

        /// <summary>
        /// Nupkg or folder path.
        /// </summary>
        public virtual string Path { get; }

        /// <summary>
        /// Last file write time. This is used for the publish date.
        /// </summary>
        public virtual DateTime LastWriteTimeUtc { get; }

        /// <summary>
        /// Package reader.
        /// </summary>
        /// <remarks>This creates a new instance each time. Callers need to dispose of it.</remarks>
        public virtual PackageReaderBase GetReader()
        {
            return new PackageArchiveReader(Path);
        }

        /// <summary>
        /// Nuspec reader.
        /// </summary>
        public virtual NuspecReader Nuspec
        {
            get
            {
                return _nuspecHelper.Value;
            }
        }

        public virtual bool IsNupkg
        {
            get
            {
                return Path.EndsWith(PackagingCoreConstants.NupkgExtension, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
