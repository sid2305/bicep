// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Configuration;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Oci;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Mock;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Bicep.Core.UnitTests.Registry
{
    //asdfg??
    public class AzureContainerRegistryManagerTests
    {
        public AzureContainerRegistryManagerTests()
        {
            IContainerRegistryClientFactory ClientFactory = StrictMock.Of<IContainerRegistryClientFactory>().Object;

            //asdfg?
            //var dispatcher = ServiceBuilder.Create(s => s.WithDisabledAnalyzersConfiguration()
            //    .AddSingleton(BicepTestConstants.ClientFactory)
            //    .AddSingleton(BicepTestConstants.TemplateSpecRepositoryFactory))
            //    .Construct<IModuleDispatcher>();

            var blobClient = new MockRegistryBlobClient();
                var clientFactory = StrictMock.Of<IContainerRegistryClientFactory>();
            clientFactory
                .Setup(m => m.CreateAuthenticatedBlobClient(It.IsAny<RootConfiguration>(), It.IsAny<Uri>(), It.IsAny<string>()))
                .Returns(blobClient);



            //if (!dispatcher.TryGetModuleReference(target, RandomFileUri(), out var @ref, out _) || @ref is not OciArtifactModuleReference targetReference)
            //        {
            //            throw new InvalidOperationException($"Module '{moduleName}' has an invalid target reference '{target}'. Specify a reference to an OCI artifact.");
            //        }

            //        Uri registryUri = new Uri($"https://{targetReference.Registry}");
            //        clients.Add((registryUri, targetReference.Repository));
            //    }

            //    return CreateMockRegistryClients(clients.Concat(additionalClients).ToArray()).factoryMock;
            //}



            //asdfg var templateSpecRepositoryFactory = dataSet.CreateMockTemplateSpecRepositoryFactory(TestContext);

            //await dataSet.PublishModulesToRegistryAsync(clientFactory);
            //var bicepFilePath = Path.Combine(outputDirectory, DataSet.TestFileMain);
            //var compiledFilePath = Path.Combine(outputDirectory, DataSet.TestFileMainCompiled);

            //// mock client factory caches the clients
            //var testClient = (MockRegistryBlobClient)clientFactory.CreateAuthenticatedBlobClient(BicepTestConstants.BuiltInConfiguration, registryUri, repository);




            //var outputDirectory = dataSet.SaveFilesToTestDirectory(TestContext);

            //var registryStr = "example.com";
            //var registryUri = new Uri($"https://{registryStr}");
            //var repository = $"test/{dataSet.Name}".ToLowerInvariant();

            //var bicepFilePath = Path.Combine(outputDirectory, DataSet.TestFileMain);
            //var compiledFilePath = Path.Combine(outputDirectory, DataSet.TestFileMainCompiled);

            //// mock client factory caches the clients
            //var testClient = (MockRegistryBlobClient)clientFactory.CreateAuthenticatedBlobClient(BicepTestConstants.BuiltInConfiguration, registryUri, repository);

            AzureContainerRegistryManager acrManager = new AzureContainerRegistryManager(clientFactory.Object);


            var moduleReference = new OciArtifactModuleReference("testregistry.whatever.io", "testrepo", "v1", null, new Uri("file:///main.bicep", UriKind.Absolute));
            //acrManager.PushModuleArtifactsAsync(BicepTestConstants.BuiltInConfiguration, moduleReference, BicepMediaTypes.BicepModuleArtifactType, )
        }
    }
}

