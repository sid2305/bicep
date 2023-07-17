// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Bicep.Core.Registry.Oci
{
    public static class BicepMediaTypes
    {
        public const string BicepModuleArtifactType = "application/vnd.ms.bicep.module.artifact";
        public const string BicepModuleConfigV1 = "application/vnd.ms.bicep.module.config.v1+json";
        public const string BicepModuleLayerV1Json = "application/vnd.ms.bicep.module.layer.v1+json";

        public const string BicepModuleSourcesArtifactType = "application/vnd.ms.bicep.module.sources";
        public const string BicepModuleSourcesV1Layer = "application/vnd.ms.bicep.module.sources.v1+zip";
    }
}
