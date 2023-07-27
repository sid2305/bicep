// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Containers.ContainerRegistry;
using Azure.Core;
using Azure.Identity;
using Bicep.Core.Configuration;
using Bicep.Core.Extensions;
using Bicep.Core.Modules;
using Bicep.Core.Registry.Oci;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using OciDescriptor = Bicep.Core.Registry.Oci.OciDescriptor;
using OciManifest = Bicep.Core.Registry.Oci.OciManifest;

namespace Bicep.Core.Registry
{
    public class AzureContainerRegistryManager
    {
        // media types are case-insensitive (they are lowercase by convention only)
        private const StringComparison MediaTypeComparison = StringComparison.OrdinalIgnoreCase;
        private const StringComparison DigestComparison = StringComparison.Ordinal;

        private readonly IContainerRegistryClientFactory clientFactory;

        public AzureContainerRegistryManager(IContainerRegistryClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
        }

        [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Relying on references to required properties of the generic type elsewhere in the codebase.")]
        public async Task<OciArtifactResult> PullModuleArtifactsAsync(RootConfiguration configuration, OciArtifactModuleReference moduleReference, bool includeSources = true)
        {
            ContainerRegistryContentClient client;
            OciManifest manifest;
            Stream manifestStream;
            string mainManifestDigest;

            async Task<(ContainerRegistryContentClient, OciManifest, Stream, string)> DownloadMainManifestInternalAsync(bool anonymousAccess)
            {
                var client = this.CreateBlobClient(configuration, moduleReference, anonymousAccess);
                var (manifest, manifestStream, manifestDigest) = await DownloadMainManifestAsync(moduleReference, client);
                return (client, manifest, manifestStream, manifestDigest);
            }

            try
            {
                // Try authenticated client first.
                Trace.WriteLine($"Authenticated attempt to pull artifact for module {moduleReference.FullyQualifiedReference}.");
                (client, manifest, manifestStream, mainManifestDigest) = await DownloadMainManifestInternalAsync(anonymousAccess: false);
            }
            catch (RequestFailedException exception) when (exception.Status == 401 || exception.Status == 403)
            {
                // Fall back to anonymous client.
                Trace.WriteLine($"Authenticated attempt to pull artifact for module {moduleReference.FullyQualifiedReference} failed, received code {exception.Status}. Fallback to anonymous pull.");
                (client, manifest, manifestStream, mainManifestDigest) = await DownloadMainManifestInternalAsync(anonymousAccess: true);
            }
            catch (CredentialUnavailableException)
            {
                // Fall back to anonymous client.
                Trace.WriteLine($"Authenticated attempt to pull artifact for module {moduleReference.FullyQualifiedReference} failed due to missing login step. Fallback to anonymous pull.");
                (client, manifest, manifestStream, mainManifestDigest) = await DownloadMainManifestInternalAsync(anonymousAccess: true);
            }

            // Continue using the client that worked for the rest of our calls

            var moduleStream = await GetModuleArmTemplateFromManifest(client, manifest);
            Stream? sourcesStream = null;

            if (includeSources)
            {
                sourcesStream = await GetBicepSourcesAsync(client, moduleReference, mainManifestDigest);
            }

            return new OciArtifactResult(mainManifestDigest, manifest, manifestStream, moduleStream, sourcesStream);
        }

        // Retrieves a list of manifests that refer to the specified manifest (and thus are attached to it)
        private static async Task<IEnumerable<(string artifactType, string digest)>> GetReferrersAsync(ContainerRegistryContentClient client, OciArtifactModuleReference moduleReference, string mainManifestDigest)
        {
            IEnumerable<(string artifactType, string digest)>? referrers = null;

            if (client.Pipeline is { } pipeline) // asdfg this guards against Bicep.Core.UnitTests.Registry.MockRegistryBlobClient which doesn't implement Pipeline.  Should it be implemented in tests?
            {
                var request = client.Pipeline.CreateRequest();
                request.Method = RequestMethod.Get;
                request.Uri.Reset(GetRegistryUri(moduleReference));
                request.Uri.AppendPath("/v2/", false);
                request.Uri.AppendPath(moduleReference.Repository, true);
                request.Uri.AppendPath("/referrers/", false);
                request.Uri.AppendPath(mainManifestDigest);

                var response = await client.Pipeline.SendRequestAsync(request, CancellationToken.None);
                if (response.IsError)
                {
                    throw new Exception($"Unable to retrieve source manifests. Referrers API failed with status code {response.Status}");
                }

                //asdfg test with responses that contain additional data

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
                var referrersResponse = JsonSerializer.Deserialize<JsonElement>(response.Content.ToString());
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

                /* Example JSON result:
                    {
                      "schemaVersion": 2,
                      "mediaType": "application/vnd.oci.image.index.v1+json",
                      "manifests": [
                        {
                          "mediaType": "application/vnd.oci.image.manifest.v1+json",
                          "digest": "sha256:210a9f9e8134fc77940ea17f971adcf8752e36b513eb7982223caa1120774284",
                          "size": 811,
                          "artifactType": "application/vnd.ms.bicep.module.sources"
                        },
                        ...
                */

                referrers = referrersResponse.TryGetPropertyByPath("manifests")
                    ?.EnumerateArray()
                    .Select<JsonElement, (string? artifactType, string? digest)>(
                        m => (m.GetProperty("artifactType").GetString(), m.GetProperty("digest").GetString()))
                    .Where(m => m.artifactType is not null && m.digest is not null)
                    .Select(m => (m.artifactType!, m.digest!));
            }

            return referrers ?? Enumerable.Empty<(string, string)>();
        }

        private async Task<Stream?> GetBicepSourcesAsync(ContainerRegistryContentClient client, OciArtifactModuleReference moduleReference, string mainManifestDigest)
        {
            var referrers = await GetReferrersAsync(client, moduleReference, mainManifestDigest);
            var sourceDigests = referrers.Where(r => r.artifactType == BicepMediaTypes.BicepModuleSourcesArtifactType).Select(r => r.digest);
            if (sourceDigests?.Count() > 1)
            {//asdfg testpoint?
                Trace.WriteLine($"Multiple source manifests found for module {moduleReference.FullyQualifiedReference}, ignoring all."
                + $"Module manifest: ${mainManifestDigest}. "
                + $"Source referrers: {string.Join(", ", sourceDigests)}");
            }
            else if (sourceDigests?.SingleOrDefault() is string sourcesManifestDigest)
            {
                var sourcesManifest = await client.GetManifestAsync(sourcesManifestDigest); //asdfgasdfg testpoint
                var sourcesManifestStream = sourcesManifest.Value.Manifest.ToStream();
                var dm = DeserializeManifest(sourcesManifestStream);
                var sourceLayer = dm.Layers.FirstOrDefault(l => l.MediaType == BicepMediaTypes.BicepModuleSourcesV1Layer);
                if (sourceLayer?.Digest is string sourcesBlobDigest)
                {
                    var sourcesBlobResult = await client.DownloadBlobContentAsync(sourcesBlobDigest);

                    // Caller is responsible for disposing the stream
                    return sourcesBlobResult.Value.Content.ToStream();
                }
            }

            return null;
        }

        //asdfg https://learn.microsoft.com/en-us/dotnet/api/overview/azure/containers.containerregistry-readme?view=azure-dotnet#upload-images
        //asdfg https://learn.microsoft.com/en-us/azure/container-registry/container-registry-image-formats#oci-artifacts
        // asdfg Azure Container Registry supports the OCI Distribution Specification, a vendor-neutral, cloud-agnostic spec to store, share, secure, and deploy container images and other content types (artifacts). The specification allows a registry to store a wide range of artifacts in addition to container images. You use tooling appropriate to the artifact to push and pull artifacts. For examples, see:
        //asdfg https://github.com/oras-project/artifacts-spec/blob/main/scenarios.md


        public async Task PushModuleArtifactsAsync(RootConfiguration configuration, OciArtifactModuleReference moduleReference, string? artifactType, StreamDescriptor config, Stream? bicepSources/*asdfg dono't pass this in*/, string? documentationUri = null, string? description = null, params StreamDescriptor[] layers)
        {
            // TODO: How do we choose this? Does it ever change?
            var algorithmIdentifier = DescriptorFactory.AlgorithmIdentifierSha256;

            // push is not supported anonymously
            var blobClient = this.CreateBlobClient(configuration, moduleReference, anonymousAccess: false);

            config.ResetStream();
            var configDescriptor = DescriptorFactory.CreateDescriptor(algorithmIdentifier, config);

            config.ResetStream();
            _ = await blobClient.UploadBlobAsync(config.Stream);

            var layerDescriptors = new List<OciDescriptor>(layers.Length);
            foreach (var layer in layers)
            {
                var layerDescriptor = DescriptorFactory.CreateDescriptor(algorithmIdentifier, layer);
                layerDescriptors.Add(layerDescriptor);

                layer.ResetStream();
                _ = await blobClient.UploadBlobAsync(layer.Stream);
            }

            var annotations = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(documentationUri))
            {
                annotations[LanguageConstants.OciOpenContainerImageDocumentationAnnotation] = documentationUri;
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                annotations[LanguageConstants.OciOpenContainerImageDescriptionAnnotation] = description;
            }

            // Adding a timestamp is important to ensure any sources manifests will always point to a unique module
            //   manifest, even if something in the sources changes that doesn't affect the compiled output.
            // And it can be useful as well.
            annotations[LanguageConstants.OciOpenContainerImageCreatedAnnotation] = DateTime.UtcNow.ToRFC3339();

            var manifest = new OciManifest(2, null, artifactType, configDescriptor, layerDescriptors, null, annotations);

            using var manifestStream = new MemoryStream();
            OciSerialization.Serialize(manifestStream, manifest);

            manifestStream.Position = 0;
            var manifestBinaryData = await BinaryData.FromStreamAsync(manifestStream);
            var manifestUploadResult = await blobClient.SetManifestAsync(manifestBinaryData, moduleReference.Tag, mediaType: ManifestMediaType.OciImageManifest);

            manifestStream.Position = 0;
            var manifestStreamDescriptor = new StreamDescriptor(manifestStream, ManifestMediaType.OciImageManifest.ToString()/*asdfg*/); //asdfg
            var manifestDescriptor = DescriptorFactory.CreateDescriptor(algorithmIdentifier, manifestStreamDescriptor);

            if (bicepSources is not null)
            {
                // asdfg remove current attachments (only if force??)

                // Azure Container Registries won't recognize this as a valid attachment unless this is valid JSON, so write out an empty object
                using var innerConfigStream = new MemoryStream(new byte[] { (byte)'{', (byte)'}' });
                var configasdfg = new StreamDescriptor(innerConfigStream, BicepMediaTypes.BicepModuleSourcesArtifactType);//, new Dictionary<string, string> { { "asdfg1", "asdfg value" } });
                var configasdfgDescriptor = DescriptorFactory.CreateDescriptor(algorithmIdentifier, configasdfg);

                // Upload config blob
                configasdfg.ResetStream();
                var asdfgresponse1 = await blobClient.UploadBlobAsync(configasdfg.Stream); // asdfg should get digest from result
                var layerasdfg = new StreamDescriptor(bicepSources, BicepMediaTypes.BicepModuleSourcesV1Layer, new Dictionary<string, string> { { "org.opencontainers.image.title", $"Sources for {moduleReference.FullyQualifiedReference}"/*asdfg*/ } });
                layerasdfg.ResetStream();
                var layerasdfgDescriptor = DescriptorFactory.CreateDescriptor(algorithmIdentifier, layerasdfg);

                layerasdfg.ResetStream();
                var asdfgresponse2 = await blobClient.UploadBlobAsync(layerasdfg.Stream);

                var manifestasdfg = new OciManifest(
                    2,
                    null,
                    BicepMediaTypes.BicepModuleSourcesArtifactType,
                    configasdfgDescriptor,
                    new List<OciDescriptor> { layerasdfgDescriptor },
                    subject: manifestDescriptor, // This is the reference back to the main manifest that links the source manifest to it
                    new Dictionary<string, string> { { LanguageConstants.OciOpenContainerImageCreatedAnnotation, DateTime.UtcNow.ToRFC3339() } }
                    );

                using var manifestasdfgStream = new MemoryStream();
                OciSerialization.Serialize(manifestasdfgStream, manifestasdfg);

                manifestasdfgStream.Position = 0;
                var manifestasdfgBinaryData = await BinaryData.FromStreamAsync(manifestasdfgStream);
                var manifestasdfgUploadResult = await blobClient.SetManifestAsync(manifestasdfgBinaryData, null);//, mediaType: ManifestMediaType.OciImageManifest/*asdfg?*/);
            }
        }

        private static Uri GetRegistryUri(OciArtifactModuleReference moduleReference) => new($"https://{moduleReference.Registry}");

        private ContainerRegistryContentClient CreateBlobClient(RootConfiguration configuration, OciArtifactModuleReference moduleReference, bool anonymousAccess) => anonymousAccess
            ? this.clientFactory.CreateAnonymousBlobClient(configuration, GetRegistryUri(moduleReference), moduleReference.Repository)
            : this.clientFactory.CreateAuthenticatedBlobClient(configuration, GetRegistryUri(moduleReference), moduleReference.Repository);

        private static async Task<(OciManifest, Stream, string)> DownloadMainManifestAsync(OciArtifactModuleReference moduleReference, ContainerRegistryContentClient client)
        {
            Response<GetManifestResult> manifestResponse;
            try
            {
                // either Tag or Digest is null (enforced by reference parser)
                var tagOrDigest = moduleReference.Tag
                    ?? moduleReference.Digest
                    ?? throw new ArgumentNullException(nameof(moduleReference), $"The specified module reference has both {nameof(moduleReference.Tag)} and {nameof(moduleReference.Digest)} set to null.");

                manifestResponse = await client.GetManifestAsync(tagOrDigest);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                // manifest does not exist
                throw new OciModuleRegistryException("The module does not exist in the registry.", exception);
            }

            // the Value is disposable, but we are not calling it because we need to pass the stream outside of this scope
            var stream = manifestResponse.Value.Manifest.ToStream();

            // BUG: The SDK internally consumed the stream for validation purposes and left position at the end
            stream.Position = 0;
            ValidateManifestResponse(manifestResponse);

            // the SDK doesn't expose all the manifest properties we need
            // so we need to deserialize the manifest ourselves to get everything
            stream.Position = 0;
            var deserialized = DeserializeManifest(stream);
            stream.Position = 0;

            return (deserialized, stream, manifestResponse.Value.Digest);
        }

        private static void ValidateManifestResponse(Response<GetManifestResult> manifestResponse)
        {
            var digestFromRegistry = manifestResponse.Value.Digest;
            var stream = manifestResponse.Value.Manifest.ToStream();

            // TODO: The registry may use a different digest algorithm - we need to handle that
            string digestFromContent = DescriptorFactory.ComputeDigest(DescriptorFactory.AlgorithmIdentifierSha256, stream);

            if (!string.Equals(digestFromRegistry, digestFromContent, DigestComparison))
            {
                throw new OciModuleRegistryException($"There is a mismatch in the manifest digests. Received content digest = {digestFromContent}, Digest in registry response = {digestFromRegistry}");
            }
        }

        private static async Task<Stream> GetModuleArmTemplateFromManifest(ContainerRegistryContentClient client, OciManifest manifest)
        {
            // Bicep versions before 0.14 used to publish modules without the artifactType field set in the OCI manifest,
            // so we must allow null here
            if (manifest.ArtifactType is not null && !string.Equals(manifest.ArtifactType, BicepMediaTypes.BicepModuleArtifactType, MediaTypeComparison))
            {
                throw new InvalidModuleException($"Expected OCI artifact to have the artifactType field set to either null or '{BicepMediaTypes.BicepModuleArtifactType}' but found '{manifest.ArtifactType}'.", InvalidModuleExceptionKind.WrongArtifactType);
            }

            ValidateConfig(manifest.Config);
            if (manifest.Layers.Length != 1)
            {
                throw new InvalidModuleException("Expected a single layer in the OCI artifact.");
            }

            var layer = manifest.Layers.Single();

            return await ProcessMainManifestLayer(client, layer);
        }

        private static async Task<Stream> ProcessMainManifestLayer(ContainerRegistryContentClient client, OciDescriptor layer)
        {
            if (!string.Equals(layer.MediaType, BicepMediaTypes.BicepModuleLayerV1Json, MediaTypeComparison))
            {
                throw new InvalidModuleException($"Expected main module manifest layer to have media type \"{layer.MediaType}\", but found \"{ layer.MediaType }\"", InvalidModuleExceptionKind.WrongModuleLayerMediaType);
            }

            return await DownloadBlobAsync(client, layer.Digest, layer.Size);
        }

        private static async Task<Stream> DownloadBlobAsync(ContainerRegistryContentClient client, string digest, long expectedSize)
        {
            Response<DownloadRegistryBlobResult> blobResponse;
            try
            {
                blobResponse = await client.DownloadBlobContentAsync(digest);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                throw new InvalidModuleException($"Could not find container registry blob with digest \"{digest}\".", exception);
            }

            var stream = blobResponse.Value.Content.ToStream();

            if (expectedSize != stream.Length)
            {
                throw new InvalidModuleException($"Expected container registry blob with digest {digest} to have a size of {expectedSize} bytes but it contains {stream.Length} bytes.");
            }

            stream.Position = 0;
            string digestFromContents = DescriptorFactory.ComputeDigest(DescriptorFactory.AlgorithmIdentifierSha256, stream);
            stream.Position = 0;

            if (!string.Equals(digest, digestFromContents, DigestComparison))
            {
                throw new InvalidModuleException($"There is a mismatch in the module's container registry digests. Received content digest = {digestFromContents}, requested digest = {digest}");
            }

            return blobResponse.Value.Content.ToStream();
        }

        private static void ValidateConfig(OciDescriptor config) //asdfg make nested function?
        {
            // media types are case insensitive
            if (!string.Equals(config.MediaType, BicepMediaTypes.BicepModuleConfigV1, MediaTypeComparison))
            {
                throw new InvalidModuleException($"Did not expect config media type \"{config.MediaType}\".");
            }

            if (config.Size != 0)
            {
                throw new InvalidModuleException("Expected an empty config blob.");
            }
        }

        private static OciManifest DeserializeManifest(Stream stream)
        {
            try
            {
                return OciSerialization.Deserialize<OciManifest>(stream);
            }
            catch (Exception exception)
            {
                throw new InvalidModuleException("Unable to deserialize the module manifest.", exception);
            }
        }
    }
}
