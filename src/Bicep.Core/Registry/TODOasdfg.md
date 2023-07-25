asdfg delete me

# TODOs
* Make possible to have additional layers in main manifest
* src/Bicep.Core/Registry/AzureContainerRegistryManager.cs -> rename?
* Move sources stuff out of AzureContainerRegistryManager.cs
* Why are integration tests so slow?
  * ForceModuleRestoreShouldRestoreAllModules 2x
    * 3s: await dataSet.PublishModulesToRegistryAsync(clientFactory);
  * ForceModuleRestoreWithStuckFileLockShouldFailAfterTimeout
  * **30s**: ModuleRestoreWithStuckFileLockShouldFailAfterTimeout

// asdfg anthony says create hash from modified sources

//asdfg ITestDataSource?

//asdfg source mapping?  var features = new FeatureProviderOverrides(TestContext, RegistryEnabled: dataSet.HasExternalModules, SourceMappingEnabled: true);

// asdfg cyclic dependencies?
// asdfg what about references to other external modules?
/*
 e.g.:
 module m1 'br/public:samples/hello-world:1.0.2' = {
   name: 'm1'
   params: {
     name: 'me myself'
   }
 }
=>
    {
      "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/publicRegistry/helloWorld/main.bicep",
      "localPath": "main.bicep",
      "kind": "bicep"
    },

*/


# Test scenarios

## Bicep.Core.IntegrationTests.RegistryTests
### Exists
* Cache location handling
* module restore state
* force module restore
### TODO asdfg
* Save/restore sources.zip
* Get sources archive from sources.zip
### Covers
* AzureContainerRegistryManager.PushArtifactAsync
* AzureContainerRegistryManager.PullArtifactAsync
  * DownloadMainManifestAsync
  * ProcessManifest
  * ProcessLayer
  * ValidateBlobResponse
  * GetReferrersAsync (called)
  * GetBicepSourcesAsync (called)

## namespace Bicep.Core.UnitTests.Registry.DescriptorFactoryTests
## namespace Bicep.Core.UnitTests.Registry.ModuleDispatcherTests
## namespace Bicep.Core.UnitTests.Registry.OciModuleRegistryTests
## namespace Bicep.Core.UnitTests.Registry.SourceArchiveTests

### Bicep.LanguageServer.Handlers.BicepRegistryCacheRequestHandler



## testing TODO asdfg
AzureContainerRegistryManager asdfg p1 (currently only integration tests?)

publish
BicepDefinitionHandler
