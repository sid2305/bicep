// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core.Diagnostics;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Oci;
using Bicep.Core.SourceCode;
using Bicep.Core.Workspaces;
using MediatR;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Bicep.LanguageServer.Handlers
{
    [Method(BicepExternalSourceRequestHandler.BicepExternalSourceLspMethodName, Direction.ClientToServer)]
    public record BicepExternalSourceParams(
        TextDocumentIdentifier TextDocument, // The bicep file which contains a reference to the target module
        string Target                        // The module reference to display sources for
    ) : ITextDocumentIdentifierParams, IRequest<BicepExternalSourceResponse>;

    public record BicepExternalSourceResponse(string Content);

    /// <summary>
    /// Handles textDocument/bicepExternalSource LSP requests. These are sent by clients that are resolving contents of document URIs using the bicep-extsrc: scheme.
    /// The BicepDefinitionHandler returns such URIs when definitions are inside modules that reside in the local module cache.
    /// </summary>
    public class BicepExternalSourceRequestHandler : IJsonRpcRequestHandler<BicepExternalSourceParams, BicepExternalSourceResponse>
    {
        public const string BicepExternalSourceLspMethodName = "textDocument/bicepExternalSource";

        private readonly IModuleDispatcher moduleDispatcher;
        private readonly IFileResolver fileResolver;

        public BicepExternalSourceRequestHandler(IModuleDispatcher moduleDispatcher, IFileResolver fileResolver)
        {
            this.moduleDispatcher = moduleDispatcher;
            this.fileResolver = fileResolver;
        }

        public Task<BicepExternalSourceResponse> Handle(BicepExternalSourceParams request, CancellationToken cancellationToken)
        {
            // If any of the following paths result in an exception being thrown (and surfaced client-side to the user),
            // it indicates a code defect client or server-side.
            // In normal operation, the user should never see them regardless of how malformed their code is.

            if (!moduleDispatcher.TryGetArtifactReference(ArtifactType.Module, request.Target, request.TextDocument.Uri.ToUriEncoded()).IsSuccess(out var moduleReference))
            {
                throw new InvalidOperationException(
                    $"The client specified an invalid module reference '{request.Target}'.");
            }

            if (!moduleReference.IsExternal)
            {
                throw new InvalidOperationException(
                    $"The specified module reference '{request.Target}' refers to a local module which is not supported by {BicepExternalSourceLspMethodName} requests.");
            }

            if (this.moduleDispatcher.GetArtifactRestoreStatus(moduleReference, out _) != ArtifactRestoreStatus.Succeeded)
            {
                throw new InvalidOperationException(
                    $"The module '{moduleReference.FullyQualifiedReference}' has not yet been successfully restored.");
            }

            if (!moduleDispatcher.TryGetLocalArtifactEntryPointUri(moduleReference).IsSuccess(out var uri))
            {
                throw new InvalidOperationException(
                    $"Unable to obtain the entry point URI for module '{moduleReference.FullyQualifiedReference}'.");
            }

            if (moduleDispatcher.TryGetModuleSources(moduleReference) is SourceArchive sourceArchive)
            {
                // TODO: For now, we just proffer the main source file
                var entrypointFile = sourceArchive.SourceFiles.Single(f => f.Path == sourceArchive.EntrypointPath);
                return Task.FromResult(new BicepExternalSourceResponse(entrypointFile.Contents));
            }

            // No sources available, just retrieve the JSON source
            if (!this.fileResolver.TryRead(uri).IsSuccess(out var contents, out var failureBuilder))
            {
                var message = failureBuilder(DiagnosticBuilder.ForDocumentStart()).Message;
                throw new InvalidOperationException($"Unable to read file '{uri}'. {message}");
            }

            return Task.FromResult(new BicepExternalSourceResponse(contents));
        }

        /// <summary>
        /// Creates a bicep-extsrc: URI for a given module's source file to give to the client to use as a document URI.  (Client should then
        ///   respond with a textDocument/externalSource request).
        /// </summary>
        /// <param name="localCachedJsonPath">The path to the local cached main.json file</param>
        /// <param name="reference">The module reference</param>
        /// <param name="sourceArchive">The source archive for the module, if sources are available</param>
        /// <returns>A bicep-extsrc: URI</returns>
        public static Uri GetExternalSourceLinkUri(string localCachedJsonPath, OciArtifactReference reference, SourceArchive? sourceArchive)
        {
            // NOTE: This should match the logic in src\vscode\src\language\bicepExternalSourceContentProvider.ts:decodeExternalSourceUri

            Debug.Assert(Path.GetFileName(localCachedJsonPath).Equals("main.json", StringComparison.InvariantCulture), "A compiled module entrypoint should always be main.json");

            var sourceFilePath = localCachedJsonPath;
            var entrypointFilename = Path.GetFileName(sourceFilePath);

            if (sourceArchive is { })
            {
                // We have Bicep source code available.
                // Replace the local cached JSON name (always main.json) with the actual source entrypoint filename (e.g.
                //   myentrypoint.bicep) so clients know to request the bicep instead of json, and so they know to use the
                //   bicep language server to display the code.
                //   e.g. "path/main.json" -> "path/myentrypoint.bicep"
                // The "path/myentrypoint.bicep" is virtual (doesn't actually exist on disk)
                entrypointFilename = Path.GetFileName(sourceArchive.EntrypointPath);
                sourceFilePath = Path.Join(Path.GetDirectoryName(sourceFilePath), entrypointFilename);
            }

            // The file path and fully qualified reference may contain special characters (like :) that need to be url-encoded.
            sourceFilePath = WebUtility.UrlEncode(sourceFilePath);
            var fullyQualifiedReference = WebUtility.UrlEncode(reference.FullyQualifiedReference);
            var version = reference.Tag ?? reference.Digest;

            //var display = $"{reference.Scheme}:{reference.Registry}/{reference.Repository}/{entrypointFilename} ({reference.Tag ?? reference.Digest})";
            var friendlyTitle = $"{reference.Scheme}:{reference.Registry}/{reference.Repository}/{version}/{entrypointFilename} ({Path.GetFileName(reference.Repository)}:{version})";

            // Encode the source file path as a path and the fully qualified reference as a fragment.
            // VsCode will pass it to our language client, which will respond by requesting the source to display via
            //   a textDocument/bicepExternalSource request (see BicepExternalSourceHandler)
            // Example:
            //
            //   source available (unencoded version):
            //     bicep-extsrc:br:myregistry.azurecr.io/myrepo:main.bicep (v1)#br:myregistry.azurecr.io/myrepo:v1#/Users/MyUserName/.bicep/br/registry.azurecr.io/myrepo/v1$/main.bicep
            //
            //   source not available (unencoded version)
            //   NOTE: the second # will be encoded because it's part of the fragment
            //     bicep-extsrc:br:myregistry.azurecr.io/myrepo:main.json (v1)#br:myregistry.azurecr.io/myrepo:v1#/Users/MyUserName/.bicep/br/registry.azurecr.io/myrepo/v1$/main.json
            return new Uri($"bicep-extsrc:{friendlyTitle}#{fullyQualifiedReference}#{sourceFilePath}");
        }
    }
}
