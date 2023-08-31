// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Bicep.Core.Syntax;
using System.Collections.Concurrent;

namespace Bicep.Core.Semantics.Metadata
{
    public abstract class SyntaxMetadataCacheBase<TMetadata>
    {
        private readonly ConcurrentDictionary<SyntaxBase, TMetadata> cache;

        protected SyntaxMetadataCacheBase()
        {
            cache = new();
        }

        protected abstract TMetadata Calculate(SyntaxBase syntax);

        public TMetadata? TryLookup(SyntaxBase syntax)
        {
            return cache.GetOrAdd(
                syntax,
                syntax => Calculate(syntax));
        }
    }
}
