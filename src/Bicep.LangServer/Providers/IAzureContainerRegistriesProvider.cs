// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Bicep.LanguageServer.Providers
{
    public interface IAzureContainerRegistriesProvider
    {
        // Returns login server URIs, e.g. "contoso.azurecr.io"
        IAsyncEnumerable<string> GetRegistryUris(Uri templateUri, CancellationToken cancellation);
    }
}
