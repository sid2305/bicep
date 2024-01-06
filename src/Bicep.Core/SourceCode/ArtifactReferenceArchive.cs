// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.Navigation;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using Uri = System.Uri;
using TextSpan = Bicep.Core.Parsing.TextSpan;


//asdfg filename?

namespace Bicep.Core.SourceCode;

//public record SourceFileTextRange(
//    string filePath, //asdfg
//    Range TextRange
//);

//public record DocumentLinks(
//    ImmutableArray<SourceCodeDocumentLink> DocumentLinks
//);

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

public static class Asdfg
{
    public static IImmutableDictionary<Uri, SourceCodeDocumentLink[]> GetDocumentLinks(SourceFileGrouping sourceFileGrouping)
    {
        var dictionary = new Dictionary<Uri, SourceCodeDocumentLink[]>();

        foreach (var sourceAndDictPair in sourceFileGrouping.FileUriResultByArtifactReference)
        {
            ISourceFile referencingFile = sourceAndDictPair.Key;
            IDictionary<IArtifactReferenceSyntax, Result<Uri, UriResolutionError>> referenceSyntaxeToUri = sourceAndDictPair.Value;

            var linksForReferencingFile = new List<SourceCodeDocumentLink>();

            foreach (var syntaxAndUriPair in referenceSyntaxeToUri)
            {
                IArtifactReferenceSyntax syntax = syntaxAndUriPair.Key;
                Result<Uri, UriResolutionError> uriResult = syntaxAndUriPair.Value;
                if (syntax.Path is { } && uriResult.IsSuccess(out var uri))
                {
                    Trace.WriteLine($"{referencingFile.FileUri}: {syntax.Path.ToText()} -> {uri}");
                    linksForReferencingFile.Add(new SourceCodeDocumentLink(
                        syntax.Path.Span,
                        uri,
                        null, //asdfg target span
                        null //asdfg target selection span
                        ));
                }
            }

            dictionary.Add(referencingFile.FileUri, linksForReferencingFile.ToArray());
        }

        return dictionary.ToImmutableDictionary();
    }
}

//asdfg
//public static class TextSpanExtensions
//{
//    public static Range ToRange(this TextSpan textSpan) //asdfg difference between System.Range and System.Span?
//    {
//        return new Range(textSpan.Position, textSpan.Position + textSpan.Length);
//    }
//}