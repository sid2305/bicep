
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using Bicep.Core.Configuration;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Oci;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Mock;
using Bicep.Core.UnitTests.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.ResourceStack.Common.Memory;
using Moq;

namespace Bicep.Core.UnitTests.Registry
{
    [TestClass]
    public class AzureContainerRegistryManagerTests
    {
        [NotNull]
        public TestContext? TestContext { get; set; }

        private OciModuleReference CreateModuleReference(string registry, string repository, string? tag, string? digest)
        {
            OciModuleReference.TryParse(null, $"{registry}/{repository}:{tag}", BicepTestConstants.BuiltInConfiguration, new Uri("file:///main.bicep")).IsSuccess(out var moduleReference).Should().BeTrue();
            return moduleReference!;
        }

        private async Task<(MockRegistryBlobClient, Mock<IContainerRegistryClientFactory>)> PublishArtifactLayersToMockClient(string tempDirectory, string registry, Uri registryUri, string repository, string? mediaType, string? artifactType, string? configContents, IEnumerable<(string mediaType, string contents)> layers)
        {
            var client = new MockRegistryBlobClient();

            var clientFactory = StrictMock.Of<IContainerRegistryClientFactory>();
            clientFactory.Setup(m => m.CreateAuthenticatedBlobClient(It.IsAny<RootConfiguration>(), registryUri, repository)).Returns(client);

            var templateSpecRepositoryFactory = BicepTestConstants.TemplateSpecRepositoryFactory;

            Directory.CreateDirectory(tempDirectory);

            var containerRegistryManager = new AzureContainerRegistryManager(clientFactory.Object);

            var fs = new MockFileSystem();
            var configurationManager = new ConfigurationManager(fs);
            var parentUri = new Uri("http://test.bicep", UriKind.Absolute);
            var configuration = configurationManager.GetConfiguration(parentUri);

            using var compiledStream = new BufferedMemoryStream();

            var moduleReference = CreateModuleReference(registry, repository, "v1", null);
            await containerRegistryManager.PushArtifactAsync(
                configuration: configuration,
                artifactReference: moduleReference,
                mediaType: mediaType,
                artifactType: artifactType,
                config: new StreamDescriptor(new TextByteArray(configContents ?? string.Empty).ToStream(), BicepMediaTypes.BicepModuleConfigV1),
                layers: (layers.Select(layer => new StreamDescriptor(TextByteArray.TextToStream(layer.contents), layer.mediaType))),
                new OciManifestAnnotationsBuilder()
            );

            return (client, clientFactory);
        }

        [DataTestMethod]
        //
        // No errors expected...
        //
        [DataRow(null, null, null)]
        [DataRow(null, "application/vnd.ms.bicep.module.artifact", null)]
        [DataRow("application/vnd.oci.image.manifest.v1+json", null, null)]
        [DataRow("application/vnd.oci.image.manifest.v1+json", "application/vnd.ms.bicep.module.artifact", null)]
        //
        // We should ignore any unrecognized layers and any data written into a module's config, for future compatibility
        // Expecting no errors
        [DataRow(null, null, "{}", null)]
        [DataRow("application/vnd.oci.image.manifest.v1+json", "application/vnd.ms.bicep.module.artifact", "{\"whatever\": \"your heart desires as long as it's JSON\"}")]
        //
        // Testcases expecting errors...
        //
        // These are just invalid. It's possible they might change in the future, but they would have to be breaking changes,
        //   current clients can't be expected to ignore these.
        [DataRow("application/vnd.oci.image.manifest.v1+json", "application/vnd.ms.bicep.module.unexpected", null,
            // expected error:
            "Error BCP192: Unable to restore.*but found 'application/vnd.ms.bicep.module.unexpected'.*newer version of Bicep might be required")]
        [DataRow("application/vnd.oci.image.manifest.v1+json", "application/vnd.ms.bicep.module.une2xpected", null,
            // expected error:
            "Error BCP192: Unable to restore.*but found 'application/vnd.ms.bicep.module.unexpected'.*newer version of Bicep might be required")]
        public async Task Restore_Artifacts_BackwardsAndForwardsCompatibility(string? mediaType, string? artifactType, string? configContents, string? expectedErrorRegex/*asdfg*/ = null)
        {
            var registry = "example.com";
            var registryUri = new Uri("https://" + registry);
            var repository = "hello/there";
            var tempDirectory = FileHelper.GetUniqueTestOutputPath(TestContext);

            var (client, clientFactory) = await PublishArtifactLayersToMockClient(
                tempDirectory,
                registry,
                registryUri,
                repository,
                mediaType,
                artifactType,
                configContents,
                new (string mediaType, string contents)[] { (BicepMediaTypes.BicepModuleLayerV1Json, "layer contents") });

            client.BlobUploads.Should().Be(2);
            client.Manifests.Should().HaveCount(1);
            client.ManifestTags.Should().HaveCount(1);
            client.ManifestObjects.Single().Value.Layers.Should().HaveCount(1);

            string digest = client.Manifests.Single(m => m.Value.Text.Contains(BicepMediaTypes.BicepModuleConfigV1)).Key;

            var bicep = $@"
module empty 'br:{registry}/{repository}@{digest}' = {{
  name: 'empty'
}}
";
        }

        //[DataTestMethod]asdfg
        //// Valid
        //[DataRow(new string[] { BicepMediaTypes.BicepModuleLayerV1Json }, null)]
        //// TODO: doesn't work because provider doesn't write out main.json file:
        ////[DataRow(new string[] { BicepMediaTypes.BicepProviderArtifactLayerV1TarGzip }, null)]
        //[DataRow(new string[] { "unknown1", "unknown2", BicepMediaTypes.BicepModuleLayerV1Json }, null)]
        //[DataRow(new string[] { "unknown1", BicepMediaTypes.BicepModuleLayerV1Json, "unknown2" }, null)]
        //[DataRow(new string[] { BicepMediaTypes.BicepModuleLayerV1Json, "unknown1", "unknown2" }, null)]
        //[DataRow(new string[] { BicepMediaTypes.BicepModuleLayerV1Json, "unknown1", "unknown1", "unknown2", "unknown2" }, null)]
        //// TODO: doesn't work because provider doesn't write out main.json file:
        //// [DataRow(new string[] { "unknown", BicepMediaTypes.BicepProviderArtifactLayerV1TarGzip }, null)]
        ////
        //// Invalid
        //[DataRow(new string[] { }, "Expected at least one layer")]
        //[DataRow(new string[] { "unknown1", "unknown2" }, "Did not expect only layer media types unknown1, unknown2")]
        //[DataRow(new string[] { BicepMediaTypes.BicepModuleLayerV1Json, BicepMediaTypes.BicepModuleLayerV1Json },
        //    $"Did not expect to find multiple layer media types of application/vnd.ms.bicep.module.layer.v1\\+json, application/vnd.ms.bicep.module.layer.v1\\+json")]
        //[DataRow(new string[] { BicepMediaTypes.BicepProviderArtifactLayerV1TarGzip, BicepMediaTypes.BicepProviderArtifactLayerV1TarGzip },
        //    $"Did not expect to find multiple layer media types of application/vnd.ms.bicep.provider.layer.v1.tar\\+gzip, application/vnd.ms.bicep.provider.layer.v1.tar\\+gzip")]
        //[DataRow(new string[] { BicepMediaTypes.BicepModuleLayerV1Json, BicepMediaTypes.BicepProviderArtifactLayerV1TarGzip },
        //    $"Did not expect to find multiple layer media types of application/vnd.ms.bicep.module.layer.v1\\+json, application/vnd.ms.bicep.provider.layer.v1.tar\\+gzip")]
        //public async Task Restore_Artifacts_LayerMediaTypes(string[] layerMediaTypes, string expectedErrorRegex)
        //{
        //    var registry = "example.com";
        //    var registryUri = new Uri("https://" + registry);
        //    var repository = "hello/there";
        //    var dataSet = DataSets.Empty;
        //    var tempDirectory = FileHelper.GetUniqueTestOutputPath(TestContext);

        //    var (client, clientFactory) = await PublishArtifactLayersToMockClient(
        //        tempDirectory,
        //        registry,
        //        registryUri,
        //        repository,
        //        dataSet,
        //        "application/vnd.oci.image.manifest.v1+json",
        //        "application/vnd.ms.bicep.module.artifact",
        //        null,
        //        layerMediaTypes);

        //    client.Manifests.Should().HaveCount(1);
        //    client.ManifestTags.Should().HaveCount(1);

        //    string digest = client.Manifests.Single().Key;

        //    var bicep = $@"
        //    module empty 'br:{registry}/{repository}@{digest}' = {{
        //      name: 'empty'
        //    }}
        //    ";

        //    var restoreBicepFilePath = Path.Combine(tempDirectory, "restored.bicep");
        //    File.WriteAllText(restoreBicepFilePath, bicep);

        //    var settings = new InvocationSettings(new(TestContext, RegistryEnabled: true), clientFactory.Object, BicepTestConstants.TemplateSpecRepositoryFactory);

        //    var (output, error, result) = await Bicep(settings, "restore", restoreBicepFilePath);
        //    using (new AssertionScope())
        //    {
        //        output.Should().BeEmpty();

        //        if (expectedErrorRegex == null)
        //        {
        //            result.Should().Be(0);
        //            error.Should().BeEmpty();
        //        }
        //        else
        //        {
        //            result.Should().Be(1);
        //            error.Should().MatchRegex(expectedErrorRegex);
        //        }
        //    }
        //}
    }
}
