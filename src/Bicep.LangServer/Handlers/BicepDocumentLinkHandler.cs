// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bicep.Core.Navigation;
using Bicep.Core.SourceCode;
using Bicep.Core.Syntax;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.Providers;
using Bicep.LanguageServer.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Uri = System.Uri;

namespace Bicep.LanguageServer.Handlers
{
    public class BicepDocumentLinkHandler : DocumentLinkHandlerBase
    {
        public override Task<DocumentLinkContainer?> Handle(DocumentLinkParams request, CancellationToken cancellationToken)
        {
            List<DocumentLink> links = new();
            links.Add(new DocumentLink() {
                Range = new Range(1, 5, 1, 15),
                Data = "asdfg",
                Tooltip = "my tooltip" });

            return Task.FromResult<DocumentLinkContainer?>(new DocumentLinkContainer(links));
        }

        public override Task<DocumentLink> Handle(DocumentLink request, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        protected override DocumentLinkRegistrationOptions CreateRegistrationOptions(DocumentLinkCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = TextDocumentSelector.ForScheme(LangServerConstants.ExternalSourceFileScheme)
        };
    }
}