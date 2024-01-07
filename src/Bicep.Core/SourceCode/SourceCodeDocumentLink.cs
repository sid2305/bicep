// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Bicep.Core.Parsing;

namespace Bicep.Core.SourceCode
{
    public record SourceCodeDocumentUriLink(
        //asdfg comments

        // Span of the origin of this link.
        SourceCodeRange Range,

        // The target path for this link.
        Uri TargetUri, //asdfg moniker?

        // asdfg The full target range of this link. If the target for example is a symbol
        // then target range is the range enclosing this symbol not including
        // leading/trailing whitespace but everything else like comments. This
        // information is typically used to highlight the range in the editor.
        //asdfg[JsonConverter(typeof(TextSpanConverter))]
        SourceCodeRange? TargetRange, //asdfg?

        // asdfg The range that should be selected and revealed when this link is being
        // followed, e.g the name of a function. Must be contained by the
        // `targetRange`. See also `DocumentSymbol#range`
        SourceCodeRange? TargetSelectionRange //asdfg?
    );

    public record SourceCodeDocumentPathLink(
        SourceCodeRange Range,
        string Target, //asdfg moniker?
        SourceCodeRange? TargetRange,
        SourceCodeRange? TargetSelectionRange //asdfg?
    );
}
