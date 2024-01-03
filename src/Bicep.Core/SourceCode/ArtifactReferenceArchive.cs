// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.Navigation;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using Range = System.Range;
using Uri = System.Uri;

namespace Bicep.Core.SourceCode;

public record SourceFileTextRange(
    string filePath, //asdfg
    Range TextRange);

//public record ArtifactReferencesCollection(
//    ImmutableDictionary<string> 
//    );

//public class ArtifactReferencesCollection
//{
//    public ArtifactReferencesCollection(SourceFileGrouping sourceFileGrouping)
//    {
//        foreach (var pair in sourceFileGrouping.FileUriResultByArtifactReference)
//        {
//            ISourceFile sourceFile = pair.Key;
//            IDictionary<IArtifactReferenceSyntax, Result<Uri, UriResolutionError>> referenceSyntaxes = pair.Value;

//            var referenceSpans = referenceSyntaxes.Keys.Select(x => x.TryGetPath()).WhereNotNull().Select(x => x.Span);

//            //    .WhereNotNull()
//            //    .Select()


//            //KeyValuePair<ISourceFile, ImmutableDictionary<IArtifactReferenceSyntax, Utils.Result<System.Uri, UriResolutionError>>>
//        }
//    }

//    public Range { get; init; }

//    [Optional]
//    public DocumentUri? Target { get; init; }

//    [Optional]
//    public JToken? Data { get; init; }

//    [Optional]
//    public string? Tooltip { get; init; }

//    ImmutableDictionary<ISourceFile, ImmutableDictionary<IArtifactReferenceSyntax, Result<Uri, UriResolutionError>>> fileUriResultByArtifactReference;

//    public ArtifactReferenceArchive()
//    {
//        ImmutableDictionary<ISourceFile, ImmutableDictionary<IArtifactReferenceSyntax, Result<Uri, UriResolutionError>>> fileUriResultByArtifactReference,
//    }
//}
