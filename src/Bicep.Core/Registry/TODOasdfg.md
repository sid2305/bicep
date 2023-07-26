asdfg delete me

# Questions for Bicep Issues
* public interface ISourceFile
    {
        Uri FileUri { get; }
        **string GetOriginalSource();**
    }


# TODOs asdfg
* Version the sources.zip file?
* Should entrypoint be json or bicep?
* Make possible to have additional layers in main manifest
* src/Bicep.Core/Registry/AzureContainerRegistryManager.cs -> rename?
* Move sources stuff out of AzureContainerRegistryManager.cs
* Why are integration tests so slow?
  * ForceModuleRestoreShouldRestoreAllModules 2x
    * 3s: await dataSet.PublishModulesToRegistryAsync(clientFactory);
  * ForceModuleRestoreWithStuckFileLockShouldFailAfterTimeout
  * **30s**: ModuleRestoreWithStuckFileLockShouldFailAfterTimeout
* Make only relative, e.g.:
   SOURCE FILE: file:///Users/stephenweatherford/repos/template-examples/bicep/modules/privateRegistry/helloWorld/main.bicep:
* Minimum version that can read a particular sources files
* Anthony says create hash from modified sources
* ITestDataSource?
* source mapping?
  * var features = new FeatureProviderOverrides(TestContext, RegistryEnabled: dataSet.HasExternalModules, SourceMappingEnabled: true);
  * /// Creates a JsonTextWriter that is capable of generating a source map for the compiled JSON
* zip or gzt or whatever?
* need way to match .bicep with .json?
* don't include main.json?
*     public interface ISourceFile
    {
        Uri FileUri { get; }
        string GetOriginalSource();
    }


* compilationWriter.ToStream(compilation, compiledArmTemplateStream); //asdfgasdfg this is what is used to write main arm template (from bicep)
* PublishCommandTests
* nested modules
* local modules


# LATER
* [ ] loadContent files?  they don't currently show up in sources


// asdfg fix regression: opening template spec or arm templates doesn't show in JSON editor
// asdfg analyzer failures will show in editor that would have been ignored with bicepconfig.json
// asdfg okay to show location of template spec refs?  I assume they're in the compiled ARM json anyway?
// asdfg option to not publish sources?
// asdfg hide under capabilities
// asdfg anthony (marcin?) said we should built off of the modified bicep (stripped comments)...  correct?
//     asdfg what about formatting JSON?

// asdfg remove user info from paths in __metadata.json
// asdfg built json
// asdfg paths: absolute or relative or both?
//   e.g.
/*
 {
  "entryPoint": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/complicated/my%20entrypoint.bicep",
  "sourceFiles": [
    {
      "uri": "/Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$app$app-configuration/1.0.1$/main.json",
      "localPath": "main.json",
      "kind": "armTemplate"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$samples$hello-world/1.0.2$/main.json",
      "localPath": "main.json",
      "kind": "armTemplate"
    },
    {
      "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/simpleModule/storageAccount.bicep",
      "localPath": "storageAccount.bicep",
      "kind": "bicep"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$app$dapr-containerapps-environment/1.2.2$/main.json",
      "localPath": "main.json",
      "kind": "armTemplate"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/2.0a/main.json",
      "localPath": "main.json",
      "kind": "templateSpec"
    },
    {
      "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/complicated/my%20entrypoint.bicep",
      "localPath": "my%20entrypoint.bicep",
      "kind": "bicep"
    },
    {
      "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/complicated/modules/main.bicep",
      "localPath": "main.bicep",
      "kind": "bicep"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/1.0a/main.json",
      "localPath": "main.json",
      "kind": "templateSpec"
    }
  ]
}
 */
// asdfg example: relative to ancestor of entrypoint:
/*
 module relativePath '../simpleModule/storageAccount.bicep' = {
    =>
     {
  "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/simpleModule/storageAccount.bicep",
  "localPath": "storageAccount.bicep",
  "kind": "bicep"
},

 */
//asdfg same module could be referenced in different ways in different places

// asdfg template spec e.g.:
// module tsModule 'ts:e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storageSpec:1.0a' = {
// =>
// /Users/stephenweatherford/.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/1.0a/main.json:

// asdfg should I decompress sources.zip?
// asdfg pretty-print JSON?
// asdfg remove comments from bicep
// asdfg show bicep sources for nested modules?
//     /Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$app$dapr-containerapps-environment/1.2.2$/main.json:


* asdfg cyclic dependencies?
* asdfg what about references to other external modules?
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

## namespace Bicep.Core.UnitTests.Registry.OciModuleRegistryTests
### Covers
* ociModuleRegistry.TryGetDocumentationUri/TryGetDescription
### TODO asdfg
* [ ] everything else

## namespace Bicep.Core.UnitTests.Registry.SourceArchiveTests

### Bicep.LanguageServer.Handlers.BicepRegistryCacheRequestHandler



## testing TODO asdfg
AzureContainerRegistryManager asdfg p1 (currently only integration tests?)

publish
BicepDefinitionHandler

## Sources to test
### Bicep.Core.Registry.OciModuleRegistry  asdfg
* [ ] TryGetSources
### AzureContainerRegistryManager asdfg
* [ ] GetReferrersAsync
* [ ] GetBicepSourcesAsync
### Bicep.LanguageServer.Handlers.BicepDefinitionHandler asdfg
* [ ] HandleModuleReference?
* [ ] GetModuleSourceLinkUri?
* [ ] HandleUnboundSymbolLocation?
  * [ ] && context.Compilation.SourceFileGrouping.TryGetSourceFile(moduleDeclarationSyntax) is ISourceFile sourceFile
* [ ] HandleModuleReference?
* [ ] GetModuleSourceLinkUri?
### Bicep.LanguageServer.Handlers.BicepRegistryCacheRequestHandler asdfg
* [ ] Handle