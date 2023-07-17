// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bicep.Core.Registry.Oci
{
    public class OciDescriptor
    {
        public OciDescriptor(string mediaType, string? artifactType, string digest/*asdfg*/, long size, IDictionary<string, string>? annotations)
        {
            this.MediaType = mediaType;
            this.ArtifactType = artifactType;
            this.Digest = digest;
            this.Size = size;
            this.Annotations = (annotations?.Count > 0) ? annotations.ToImmutableDictionary() : null;
        }

        public string MediaType { get; }

        public string? ArtifactType { get; }

        public string Digest { get; }

        public long Size { get; }

        public ImmutableDictionary<string, string>? Annotations { get; }
    }
}
