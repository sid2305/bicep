// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Containers.ContainerRegistry;
using Azure.Core;
using Bicep.Core.Extensions;
using Bicep.Core.Modules;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Bicep.Core.Emit.ResourceDependencyVisitor;

namespace Bicep.Core.Registry.Oci;

    public interface IOciRegistryContentClient //asdfg do we only need an interface for GetReferrers?
    {
    #region Forward ContainerRegistryContentClient functionality

    /// <summary>
    /// Sets a manifest.
    /// </summary>
    /// <param name="manifest">The <see cref="BinaryData"/> containing the serialized manifest to set.</param>
    /// <param name="tag">A optional tag to assign to the artifact this manifest represents.</param>
    /// <param name="mediaType">The media type of the manifest.  If not specified, this value will be set to
    /// a default value of "application/vnd.oci.image.manifest.v1+json".</param>
    /// <param name="cancellationToken"> The cancellation token to use. </param>
    /// <returns>The result of the set manifest operation.</returns>
    /// <exception cref="ArgumentNullException"> If <paramref name="manifest"/> is null.</exception>
    /// <exception cref="RequestFailedException">Thrown when a failure is returned by the Container Registry service.</exception>
    Task<Response<SetManifestResult>> SetManifestAsync(BinaryData manifest, string? tag = default, ManifestMediaType? mediaType = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a container registry blob.
    /// </summary>
    /// <param name="content">The stream containing the blob data.</param>
    /// <param name="cancellationToken"> The cancellation token to use. </param>
    /// <returns>The result of the blob upload.  The raw response associated with this result is the response from the final complete upload request.</returns>
    /// <exception cref="ArgumentNullException"> If <paramref name="content"/> is null.</exception>
    /// <exception cref="RequestFailedException">Thrown when a failure is returned by the Container Registry service.</exception>
    Task<Response<UploadRegistryBlobResult>> UploadBlobAsync(Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a container registry blob.
    /// This API is a preferred way to fetch blobs that can fit into memory.
    /// The content is provided as <see cref="BinaryData"/>, which provides a lightweight abstraction for a payload of bytes.
    /// It provides convenient helper methods to get out commonly used primitives, such as streams, strings, or bytes.
    /// </summary>
    /// <param name="digest">The digest of the blob to download.</param>
    /// <param name="cancellationToken"> The cancellation token to use. </param>
    /// <returns>The result of the download blob content operation.</returns>
    /// <exception cref="ArgumentNullException"> If <paramref name="digest"/> is null.</exception>
    /// <exception cref="RequestFailedException">Thrown when a failure is returned by the Container Registry service.</exception>
    Task<Response<DownloadRegistryBlobResult>> DownloadBlobContentAsync(string digest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a manifest.
    /// </summary>
    /// <param name="tagOrDigest">The tag or digest of the manifest to get.</param>
    /// <param name="cancellationToken">The cancellation token to use.</param>
    /// <returns>The manifest result.</returns>
    /// <exception cref="ArgumentNullException"> If <paramref name="tagOrDigest"/> is null.</exception>
    /// <exception cref="RequestFailedException">Thrown when a failure is returned by the Container Registry service.</exception>
    Task<Response<GetManifestResult>> GetManifestAsync(string tagOrDigest, CancellationToken cancellationToken = default);

    #endregion Forward ContainerRegistryContentClient functionality

    /// <summary>
    /// Retrieves all manifests that reference the main manifest as its attached parent ("Subject")
    /// </summary>
    /// <param name="mainManifestDigest"></param>
    /// <returns></returns>
    Task<IEnumerable<(string artifactType, string digest)>> GetReferrersAsync(string mainManifestDigest);
}
