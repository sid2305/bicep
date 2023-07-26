// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Parsing;
using Bicep.Core.Registry;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bicep.LanguageServer.Handlers
{
    [Method(BicepRegistryCacheRequestHandler.BicepCacheLspMethod, Direction.ClientToServer)]
    public record BicepRegistryCacheParams(TextDocumentIdentifier TextDocument, string Target) : ITextDocumentIdentifierParams, IRequest<BicepRegistryCacheResponse>;

    public record BicepRegistryCacheResponse(string Content); //asdfg

    /// <summary>
    /// Handles textDocument/bicepCache LSP requests. These are sent by clients that are resolving contents of document URIs using the bicep-cache:// scheme.
    /// The BicepDefinitionHandler returns such URIs when definitions are inside modules that reside in the local module cache.
    /// </summary>
    public class BicepRegistryCacheRequestHandler : IJsonRpcRequestHandler<BicepRegistryCacheParams, BicepRegistryCacheResponse> //asdfgasdfg tests
    {
        public const string BicepCacheLspMethod = "textDocument/bicepCache";

        private readonly IModuleDispatcher moduleDispatcher;

        private readonly IFileResolver fileResolver;

        public BicepRegistryCacheRequestHandler(IModuleDispatcher moduleDispatcher, IFileResolver fileResolver)
        {
            this.moduleDispatcher = moduleDispatcher;
            this.fileResolver = fileResolver;
        }

        public Task<BicepRegistryCacheResponse> Handle(BicepRegistryCacheParams request, CancellationToken cancellationToken) //asdfgasdfg
        {
            // If any of the following paths result in an exception being thrown (and surfaced client-side to the user),
            // it indicates a code defect client or server-side.
            // In normal operation, the user should never see them regardless of how malformed their code is.

            if (!moduleDispatcher.TryGetModuleReference(request.Target, request.TextDocument.Uri.ToUri(), out var moduleReference, out _))
            {
                throw new InvalidOperationException($"The client specified an invalid module reference '{request.Target}'.");
            }

            if (!moduleReference.IsExternal)
            {
                throw new InvalidOperationException($"The specified module reference '{request.Target}' refers to a local module which is not supported by {BicepCacheLspMethod} requests."); //asdfg?
            }

            if (this.moduleDispatcher.GetModuleRestoreStatus(moduleReference, out _) != ModuleRestoreStatus.Succeeded)
            {
                throw new InvalidOperationException($"The module '{moduleReference.FullyQualifiedReference}' has not yet been successfully restored.");
            }

            if (!moduleDispatcher.TryGetLocalModuleEntryPointUri(moduleReference, out var uri, out _)) //asdfg eg file:///Users/stephenweatherford/.bicep/br/sawbicep.azurecr.io/storage/test$/main.json
            {
                throw new InvalidOperationException($"Unable to obtain the entry point URI for module '{moduleReference.FullyQualifiedReference}'.");
            }

            // asdfg tracing 
            if (moduleDispatcher.TryGetModuleSources(moduleReference, out var sourceArchive)) { //asdfg eg file:///Users/stephenweatherford/.bicep/br/sawbicep.azurecr.io/storage/test$/main.json
                //asdfg testpoint
                using (var sources = sourceArchive)
                {
                    var sortedSources = sources.GetSourceFiles()
                        .OrderBy(item => item.Metadata.Uri == sources.GetEntrypointUri())
                        .ThenBy(item => item.Metadata.Uri);

                    var sourcesCombined = "EXPERIMENTAL FEATURE, UNDER DEVELOPMENT!\n\nSource files that were published:\n\n"
                        + sources.GetMetadataContentsAsdfgDeleteMe();
                    sourcesCombined += "\n\n==================================================================\n";
                    sourcesCombined +=
                        string.Join(
                            "\n==================================================================\n",
                            sortedSources
                            .Select(m => $"SOURCE FILE: {m.Metadata.Uri}:\n\n{m.Contents}\n")
                            .ToArray());

                    return Task.FromResult(new BicepRegistryCacheResponse(sourcesCombined));
                }
            }

            // No sources available asdfg
            if (!this.fileResolver.TryRead(uri, out var contents, out var failureBuilder)) //asdfg
            { //asdfg testpoint
                var message = failureBuilder(DiagnosticBuilder.ForDocumentStart()).Message;
                throw new InvalidOperationException($"Unable to read file '{uri}'. {message}");
            }

            return Task.FromResult(new BicepRegistryCacheResponse(contents));
        }
    }
}
