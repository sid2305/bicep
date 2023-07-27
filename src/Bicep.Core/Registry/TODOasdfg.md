asdfg delete me

# Questions for Bicep Issues

# TODOs Triage asdfg


# TODOs July asdfg
* [ ] hide under experimental features flag
* [ ] fix regression: opening template spec or arm templates doesn't show in JSON editor
* Version the sources.zip file?
* Make possible to have additional layers in main manifest
  * throw error if layer version is too large
* Move sources stuff out of AzureContainerRegistryManager.cs
* Minimum version that can read a particular sources files
* source mapping?
  * var features = new FeatureProviderOverrides(TestContext, RegistryEnabled: dataSet.HasExternalModules, SourceMappingEnabled: true);
  * /// Creates a JsonTextWriter that is capable of generating a source map for the compiled JSON
* PublishCommandTests
* nested modules
* local modules

# MVP BEFORE REMOVING EXPERIMENTAL FLAG asdfg
* need way to match .bicep with .json?
* don't include main.json?
* zip or gtz or whatever?
* Anthony says create hash from modified sources
* [ ] Refactor AzureContainerRegistryManager?  Separate generic from Bicep-specific code
  * Maybe some of it moves into OciModuleRegistry?
* [ ] option to not publish sources?
* [ ] okay to show location of template spec refs?  I assume they're in the compiled ARM json anyway?
* [ ] option to not publish sources?
* [ ] P1: Can we use attached manifests?
* P1: Set metadataVersion to 1
* Fail in a friendly way on metadataVersion too large
* [ ] .. in paths don't show when extracting zip on Mac
  eg "sourceFiles": [
    {
      "path": "../../../../../.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/2.0a/main.json",
      "archivedPath": "../../../../../.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/2.0a/main.json",
      "kind": "templateSpec"
    },
    {
      "path": "../simpleModule/storageAccount.bicep",
      "archivedPath": "../simpleModule/storageAccount.bicep",
      "kind": "bicep"
    },
    {
      "path": "../../../../../.bicep/br/mcr.microsoft.com/bicep$app$dapr-containerapps-environment/1.2.2$/main.json",
      "archivedPath": "../../../../../.bicep/br/mcr.microsoft.com/bicep$app$dapr-containerapps-environment/1.2.2$/main.json",
      "kind": "armTemplate"
    },
    {
      "path": "../../../../../.bicep/br/mcr.microsoft.com/bicep$samples$hello-world/1.0.2$/main.json",
      "archivedPath": "../../../../../.bicep/br/mcr.microsoft.com/bicep$samples$hello-world/1.0.2$/main.json",
      "kind": "armTemplate"
    }
* [ ] P2: src/Bicep.Core/Registry/AzureContainerRegistryManager.cs -> rename?
* [ ] Can/should we show entrypoint.bicep in title bar?
* [ ] anthony (marcin?) said we should built off of the modified bicep (stripped comments)...  correct?
* [ ] ensure with tests?... remove user info from paths in __metadata.json
* [ ] same module could be referenced in different ways in different places
* [ ] template specs eg
  * module tsModule 'ts:e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storageSpec:1.0a' = {
     =>
  /Users/username/.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/1.0a/main.json:
* [ ] asdfg cyclic dependencies?
* [ ] show sources for nested external modules
* [ ] show sources for nested local modules
* [ ] add tracing?

# LONG TERM
* [ ] linter won't have bicepconfig.json
* [ ] compiler won't have module aliases in bicepconfig.json
* [ ] might be showing files from a newer version of Bicep
* [ ] What if they want to see the JSON?
* [ ] show multiple files
* [ ] loadContent files?  they don't currently show up in sources



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



# ENGINEERING
* [ ] Rename BicepTestContents.ClientFactory
* Why are integration tests so slow?
  * ForceModuleRestoreShouldRestoreAllModules 2x
    * 3s: await dataSet.PublishModulesToRegistryAsync(clientFactory);
  * ForceModuleRestoreWithStuckFileLockShouldFailAfterTimeout
  * **30s**: ModuleRestoreWithStuckFileLockShouldFailAfterTimeout
* ITestDataSource?
