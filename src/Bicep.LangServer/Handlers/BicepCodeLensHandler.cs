// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core;
using Bicep.Core.Analyzers;
using Bicep.Core.CodeAction;
using Bicep.Core.CodeAction.Fixes;
using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.Parsing;
using Bicep.Core.Semantics;
using Bicep.Core.Text;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.CompilationManager;
using Bicep.LanguageServer.Completions;
using Bicep.LanguageServer.Extensions;
using Bicep.LanguageServer.Providers;
using Bicep.LanguageServer.Telemetry;
using Bicep.LanguageServer.Utils;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Bicep.LanguageServer.Handlers
{
    // Provides code actions/fixes for a range in a Bicep document
    public class BicepCodeLensHandler : CodeLensHandlerBase
    {
        private readonly IClientCapabilitiesProvider clientCapabilitiesProvider;
        private readonly ICompilationManager compilationManager;

        //asdfg
        //private static readonly ImmutableArray<ICodeFixProvider> codeFixProviders = new ICodeFixProvider[]
        //{
        //    new Mul   tilineObjectsAndArraysCodeFixProvider(),
        //}.ToImmutableArray<ICodeFixProvider>();

        public BicepCodeLensHandler(ICompilationManager compilationManager, IClientCapabilitiesProvider clientCapabilitiesProvider)
        {
            this.clientCapabilitiesProvider = clientCapabilitiesProvider;
            this.compilationManager = compilationManager;
        }

        public override Task<CodeLensContainer?> Handle(CodeLensParams request, CancellationToken cancellationToken)
        {
            List<CodeLens> codeLenses = new();

            if (request.TextDocument.Uri.Scheme == ExternalSourceReference.Scheme)
            {
                codeLenses.Add(new CodeLens()
                {
                    Range = new Range(new Position(0, 0), new Position(0, 0)),
                    Command = new Command
                    {
                        Title = "Show the compiled JSON for this module",
                        Name = "bicep.internal.showMainJson",
                    }
                    .WithArguments(
                        new ExternalSourceReference(request.TextDocument.Uri).WithMainJson().ToUri().ToString()
                    ) //asdfg unencoded??
                });
            }

            return Task.FromResult<CodeLensContainer?>(new CodeLensContainer(codeLenses));
        }

        public override Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override CodeLensRegistrationOptions CreateRegistrationOptions(CodeLensCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = DocumentSelectorFactory.CreateForBicepAndParams(),
            ResolveProvider = false
        };
    }
}
