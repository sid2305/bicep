// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Web;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Oci;
using Bicep.Core.SourceCode;
using Bicep.Core.UnitTests;
using Bicep.Core.UnitTests.Mock;
using Bicep.Core.UnitTests.Utils;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.Handlers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bicep.LangServer.UnitTests.Handlers
{
    [TestClass]
    public class BicepExternalSourceRequestHandlerTests
    {
        private static readonly IFileSystem MockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            ["/foo/bar/bicepconfig.json"] = BicepTestConstants.BuiltInConfiguration.ToUtf8Json(),
        });

        private static readonly IConfigurationManager ConfigurationManager = new ConfigurationManager(MockFileSystem);

        [TestMethod]
        public async Task InvalidModuleReferenceShouldThrow()
        {
            const string ModuleRefStr = "hello";

            var dispatcher = StrictMock.Of<IModuleDispatcher>();
            dispatcher.Setup(m => m.TryGetArtifactReference(ArtifactType.Module, ModuleRefStr, It.IsAny<Uri>())).Returns(ResultHelper.Create(null as ArtifactReference, x => x.ModuleRestoreFailed("blah")));

            var resolver = StrictMock.Of<IFileResolver>();

            var handler = new BicepExternalSourceRequestHandler(dispatcher.Object, resolver.Object);

            var @params = new BicepExternalSourceParams("/main.bicep", ModuleRefStr);
            (await FluentActions
                .Awaiting(() => handler.Handle(@params, default))
                .Should()
                .ThrowAsync<InvalidOperationException>())
                .WithMessage($"The client specified an invalid module reference '{ModuleRefStr}'.");
        }

        [TestMethod]
        public async Task LocalModuleReferenceShouldThrow()
        {
            var dispatcher = StrictMock.Of<IModuleDispatcher>();
            DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder = null;

            const string ModuleRefStr = "./hello.bicep";
            LocalModuleReference.TryParse(ModuleRefStr, new Uri("fake:///not/real.bicep")).IsSuccess(out var localRef).Should().BeTrue();
            localRef.Should().NotBeNull();

            ArtifactReference? outRef = localRef;
            dispatcher.Setup(m => m.TryGetArtifactReference(ArtifactType.Module, ModuleRefStr, It.IsAny<Uri>())).Returns(ResultHelper.Create(outRef, failureBuilder));

            var resolver = StrictMock.Of<IFileResolver>();

            var handler = new BicepExternalSourceRequestHandler(dispatcher.Object, resolver.Object);

            var @params = new BicepExternalSourceParams("/foo/bar/main.bicep", ModuleRefStr);
            (await FluentActions
                .Awaiting(() => handler.Handle(@params, default))
                .Should()
                .ThrowAsync<InvalidOperationException>())
                .WithMessage($"The specified module reference '{ModuleRefStr}' refers to a local module which is not supported by {BicepExternalSourceRequestHandler.BicepExternalSourceLspMethodName} requests.");
        }

        [TestMethod]
        public async Task ExternalModuleNotInCacheShouldThrow()
        {
            var dispatcher = StrictMock.Of<IModuleDispatcher>();
            DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder = null;

            const string UnqualifiedModuleRefStr = "example.azurecr.invalid/foo/bar:v3";
            const string ModuleRefStr = "br:" + UnqualifiedModuleRefStr;

            var configuration = IConfigurationManager.GetBuiltInConfiguration();
            var parentModuleLocalPath = "/foo/main.bicep";
            var parentModuleUri = new Uri($"file://{parentModuleLocalPath}");
            OciArtifactReference.TryParseModule(null, UnqualifiedModuleRefStr, configuration, parentModuleUri).IsSuccess(out var moduleReference).Should().BeTrue();
            moduleReference.Should().NotBeNull();

            ArtifactReference? outRef = moduleReference;
            dispatcher.Setup(m => m.TryGetArtifactReference(ArtifactType.Module, ModuleRefStr, parentModuleUri)).Returns(ResultHelper.Create(outRef, failureBuilder));
            dispatcher.Setup(m => m.GetArtifactRestoreStatus(moduleReference!, out failureBuilder)).Returns(ArtifactRestoreStatus.Unknown);

            var resolver = StrictMock.Of<IFileResolver>();

            var handler = new BicepExternalSourceRequestHandler(dispatcher.Object, resolver.Object);

            var @params = new BicepExternalSourceParams(parentModuleLocalPath, ModuleRefStr);
            (await FluentActions
                .Awaiting(() => handler.Handle(@params, default))
                .Should()
                .ThrowAsync<InvalidOperationException>())
                .WithMessage($"The module '{ModuleRefStr}' has not yet been successfully restored.");
        }

        [TestMethod]
        public async Task ExternalModuleFailedEntryPointShouldThrow()
        {
            var dispatcher = StrictMock.Of<IModuleDispatcher>();
            DiagnosticBuilder.ErrorBuilderDelegate? failureBuilder = null;
            const string UnqualifiedModuleRefStr = "example.azurecr.invalid/foo/bar:v3";
            const string ModuleRefStr = "br:" + UnqualifiedModuleRefStr;

            var configuration = IConfigurationManager.GetBuiltInConfiguration();
            var parentModuleLocalPath = "/main.bicep";
            var parentModuleUri = new Uri($"file://{parentModuleLocalPath}");
            OciArtifactReference.TryParseModule(null, UnqualifiedModuleRefStr, configuration, parentModuleUri).IsSuccess(out var moduleReference).Should().BeTrue();
            moduleReference.Should().NotBeNull();

            ArtifactReference? outRef = moduleReference;
            dispatcher.Setup(m => m.TryGetArtifactReference(ArtifactType.Module, ModuleRefStr, parentModuleUri)).Returns(ResultHelper.Create(outRef, failureBuilder));
            dispatcher.Setup(m => m.GetArtifactRestoreStatus(moduleReference!, out failureBuilder)).Returns(ArtifactRestoreStatus.Succeeded);
            dispatcher.Setup(m => m.TryGetLocalArtifactEntryPointUri(moduleReference!)).Returns(ResultHelper.Create(null as Uri, x => x.ModuleRestoreFailed("blah")));

            var resolver = StrictMock.Of<IFileResolver>();

            var handler = new BicepExternalSourceRequestHandler(dispatcher.Object, resolver.Object);

            var @params = new BicepExternalSourceParams(parentModuleLocalPath, ModuleRefStr);
            (await FluentActions
                .Awaiting(() => handler.Handle(@params, default))
                .Should()
                .ThrowAsync<InvalidOperationException>())
                .WithMessage($"Unable to obtain the entry point URI for module '{ModuleRefStr}'.");
        }

        [TestMethod]
        public async Task FailureToReadEntryPointShouldThrow()
        {
            var dispatcher = StrictMock.Of<IModuleDispatcher>();

            // needed for mocking out parameters
            DiagnosticBuilder.ErrorBuilderDelegate? nullBuilder = null;
            DiagnosticBuilder.ErrorBuilderDelegate? readFailureBuilder = x => x.ErrorOccurredReadingFile("Mock file read failure.");
            string? fileContents = null;

            const string UnqualifiedModuleRefStr = "example.azurecr.invalid/foo/bar:v3";
            const string ModuleRefStr = "br:" + UnqualifiedModuleRefStr;

            var fileUri = new Uri("file:///main.bicep");
            var configuration = IConfigurationManager.GetBuiltInConfiguration();
            OciArtifactReference.TryParseModule(null, UnqualifiedModuleRefStr, configuration, fileUri).IsSuccess(out var moduleReference).Should().BeTrue();
            moduleReference.Should().NotBeNull();

            ArtifactReference? outRef = moduleReference;
            dispatcher.Setup(m => m.TryGetArtifactReference(ArtifactType.Module, ModuleRefStr, It.IsAny<Uri>())).Returns(ResultHelper.Create(outRef, null));
            dispatcher.Setup(m => m.GetArtifactRestoreStatus(moduleReference!, out nullBuilder)).Returns(ArtifactRestoreStatus.Succeeded);
            dispatcher.Setup(m => m.TryGetLocalArtifactEntryPointUri(moduleReference!)).Returns(ResultHelper.Create(fileUri, null));

            SourceArchive? sourceArchive = null;
            dispatcher.Setup(m => m.TryGetModuleSources(moduleReference!)).Returns(sourceArchive);

            var resolver = StrictMock.Of<IFileResolver>();
            resolver.Setup(m => m.TryRead(fileUri)).Returns(ResultHelper.Create(fileContents, readFailureBuilder));

            var handler = new BicepExternalSourceRequestHandler(dispatcher.Object, resolver.Object);

            var @params = new BicepExternalSourceParams(fileUri.AbsolutePath, ModuleRefStr);
            (await FluentActions
                .Awaiting(() => handler.Handle(@params, default))
                .Should()
                .ThrowAsync<InvalidOperationException>())
                .WithMessage($"Unable to read file 'file:///main.bicep'. An error occurred reading file. Mock file read failure.");
        }

        [TestMethod]
        public async Task RestoredValidModule_WithNoSources_ShouldReturnJsonContents()
        {
            var dispatcher = StrictMock.Of<IModuleDispatcher>();

            // needed for mocking out parameters
            DiagnosticBuilder.ErrorBuilderDelegate? nullBuilder = null;
            DiagnosticBuilder.ErrorBuilderDelegate? readFailureBuilder = x => x.ErrorOccurredReadingFile("Mock file read failure.");
            string? fileContents = "mock file contents";

            const string UnqualifiedModuleRefStr = "example.azurecr.invalid/foo/bar:v3";
            const string ModuleRefStr = "br:" + UnqualifiedModuleRefStr;

            var fileUri = new Uri("file:///foo/bar/main.bicep");
            var configuration = ConfigurationManager.GetConfiguration(fileUri);

            OciArtifactReference.TryParseModule(null, UnqualifiedModuleRefStr, configuration, fileUri).IsSuccess(out var moduleReference).Should().BeTrue();
            moduleReference.Should().NotBeNull();

            ArtifactReference? outRef = moduleReference;
            dispatcher.Setup(m => m.TryGetArtifactReference(ArtifactType.Module, ModuleRefStr, It.IsAny<Uri>())).Returns(ResultHelper.Create(outRef, null));
            dispatcher.Setup(m => m.GetArtifactRestoreStatus(moduleReference!, out nullBuilder)).Returns(ArtifactRestoreStatus.Succeeded);
            dispatcher.Setup(m => m.TryGetLocalArtifactEntryPointUri(moduleReference!)).Returns(ResultHelper.Create(fileUri, null));

            SourceArchive? sourceArchive = null;
            dispatcher.Setup(m => m.TryGetModuleSources(moduleReference!)).Returns(sourceArchive);

            var resolver = StrictMock.Of<IFileResolver>();
            resolver.Setup(m => m.TryRead(fileUri)).Returns(ResultHelper.Create(fileContents, nullBuilder));

            var handler = new BicepExternalSourceRequestHandler(dispatcher.Object, resolver.Object);

            var @params = new BicepExternalSourceParams(fileUri.AbsolutePath, ModuleRefStr);
            var response = await handler.Handle(@params, default);

            response.Should().NotBeNull();
            response.Content.Should().Be(fileContents);
        }

        [TestMethod]
        public async Task RestoredValidModule_WithSource_ShouldReturnBicepContents()
        {
            var dispatcher = StrictMock.Of<IModuleDispatcher>();

            // needed for mocking out parameters
            DiagnosticBuilder.ErrorBuilderDelegate? nullBuilder = null;
            DiagnosticBuilder.ErrorBuilderDelegate? readFailureBuilder = x => x.ErrorOccurredReadingFile("Mock file read failure.");
            string? fileContents = "mock file contents";

            const string UnqualifiedModuleRefStr = "example.azurecr.invalid/foo/bar:v3";
            const string ModuleRefStr = "br:" + UnqualifiedModuleRefStr;

            var fileUri = new Uri("file:///foo/bar/main.bicep");
            var configuration = ConfigurationManager.GetConfiguration(fileUri);

            OciArtifactReference.TryParseModule(null, UnqualifiedModuleRefStr, configuration, fileUri).IsSuccess(out var moduleReference).Should().BeTrue();
            moduleReference.Should().NotBeNull();

            ArtifactReference? outRef = moduleReference;
            dispatcher.Setup(m => m.TryGetArtifactReference(ArtifactType.Module, ModuleRefStr, It.IsAny<Uri>())).Returns(ResultHelper.Create(outRef, null));
            dispatcher.Setup(m => m.GetArtifactRestoreStatus(moduleReference!, out nullBuilder)).Returns(ArtifactRestoreStatus.Succeeded);
            dispatcher.Setup(m => m.TryGetLocalArtifactEntryPointUri(moduleReference!)).Returns(ResultHelper.Create(fileUri, null));

            var bicepSource = "metadata hi 'mom'";
            var sourceArchive = SourceArchive.FromStream(SourceArchive.PackSourcesIntoStream(fileUri, new Core.Workspaces.ISourceFile[] {
                SourceFileFactory.CreateBicepFile(fileUri, bicepSource)}));
            dispatcher.Setup(m => m.TryGetModuleSources(moduleReference!)).Returns(sourceArchive);

            var resolver = StrictMock.Of<IFileResolver>();
            resolver.Setup(m => m.TryRead(fileUri)).Returns(ResultHelper.Create(fileContents, nullBuilder));

            var handler = new BicepExternalSourceRequestHandler(dispatcher.Object, resolver.Object);

            var @params = new BicepExternalSourceParams(fileUri.AbsolutePath, ModuleRefStr);
            var response = await handler.Handle(@params, default);

            response.Should().NotBeNull();
            response.Content.Should().Be(bicepSource);
        }

        //asdfg: repo with path (/ -> $)
        //[TestMethod]
        //public void GetExternalSourceLinkUri_asdfg()
        //{
        //    string localCachedJsonPath = "/.bicep/br/myregistry.azurecr.io/bicep$myrepo/v1$/main.json";
        //    Uri entrypointUri = new("file:///my entrypoint.bicep", UriKind.Absolute);
        //    OciArtifactReference reference = new(ArtifactType.Module, "myregistry", "repo", "tag", null, new Uri("file:///parent.bicep", UriKind.Absolute));

        //    var sourceArchive = SourceArchive.FromStream(SourceArchive.PackSourcesIntoStream(
        //        entrypointUri,
        //        new Core.Workspaces.ISourceFile[] {
        //            SourceFileFactory.CreateBicepFile(entrypointUri, "metadata description = 'bicep module'")
        //        }));

        //    var result = BicepExternalSourceRequestHandler.GetExternalSourceLinkUri(localCachedJsonPath, reference, sourceArchive);

        //    DecodeExternalSourceUri(result).
        //    //asdfg result.Should().Be("bicep-extsrc:br:myregistry/repo/tag/my entrypoint.bicep (repo:tag)#br%3Amyregistry%2Frepo%3Atag%23%5C.bicep%5Cbr%5Cmyregistry.azurecr.io%5Cbicep%24myrepo%5Cv1%24%5Cmy+entrypoint.bicep");
        //}

        //asdfg test w/o source

        [DataRow("/.bicep/br/myregistry.azurecr.io/bicep$myrepo/v1$/main.json", "main.bicep")]
        [DataRow("c:\\.bicep\\br\\myregistry.azurecr.io\\bicep$myrepo\\v1$\\main.json", "main.bicep")]
        [DataRow("c:\\.bicep\\br\\myregistry.azurecr.io\\bicep$myrepo\\v1$\\main.json", "my entrypoint.bicep")]
        [DataRow("c:\\.bicep\\br\\myregistry.azurecr.io\\bicep$myrepo\\v1$\\main.json", "my # entrypoint.bicep")]
        [DataTestMethod]
        public void GetExternalSourceLinkUri_CachedPathWithSource(string localCachedJsonPath, string bicepEntrypointFilename)
        {
            Uri result = GetExternalSourceLinkUri(localCachedJsonPath: localCachedJsonPath, entrypointUriString: $"file:///{bicepEntrypointFilename}");

            DecodeExternalSourceUri(result).LocalCachedPath.Should().Be(localCachedJsonPath);
        }

        [DataRow("/.bicep/br/myregistry.azurecr.io/bicep$myrepo/v1$/main.json", "main.json")]
        [DataRow("c:\\.bicep\\br\\myregistry.azurecr.io\\bicep$myrepo\\v1$\\main.json", "main.json")]
        [DataTestMethod]
        public void GetExternalSourceLinkUri_CachedPathWithoutSource(string localCachedJsonPath, string expectedEntrypoint)
        {
            Uri result = GetExternalSourceLinkUri(localCachedJsonPath: localCachedJsonPath, entrypointUriString: null);

            DecodeExternalSourceUri(result).LocalCachedPath.Should().Be(localCachedJsonPath);
        }

        [DataRow("file:///entrypoint.bicep")]
        //asdfg non-file::
        [DataTestMethod]
        public void GetExternalSourceLinkUri_EntrypointPath(string entrypointUriString)
        {
            Uri result = GetExternalSourceLinkUri(entrypointUriString: entrypointUriString);

            string entryPointFilename = Path.GetFileName(DecodeExternalSourceUri(result).LocalCachedPath);
            entryPointFilename.Should().Be(Path.GetFileName(entryPointFilename));
        }

        [DataRow("myregistry.azurecr.io", "bicep/myrepo/module1", "v1", null)]
        //asdfg
        [DataTestMethod]
        public void GetExternalSourceLinkUri_(string registry, string repository, string? tag, string? digest)
        {
            Uri result = GetExternalSourceLinkUri(registry: registry, repository: repository, tag: tag, digest: digest);

            //asdfg result.Should().Be("bicep-extsrc:br:myregistry/repo/tag/my entrypoint.bicep (repo:tag)#br%3Amyregistry%2Frepo%3Atag%23%5C.bicep%5Cbr%5Cmyregistry.azurecr.io%5Cbicep%24myrepo%5Cv1%24%5Cmy+entrypoint.bicep");
        }

        private Uri GetExternalSourceLinkUri(
            string localCachedJsonPath = "/.bicep/br/myregistry.azurecr.io/bicep$myrepo/v1$/main.json",
            string? entrypointUriString = "file:///my entrypoint.bicep", // Use null to indicate no source code is available
            string registry = "myregistry.azurecr.io",
            string repository = "file:///bicep/myrepo/module1",
            string? tag = "v1",
            string? digest = null
            )
        {
            Uri? entrypointUri = entrypointUriString is { } ? new(entrypointUriString, UriKind.Absolute) : null;
            OciArtifactReference reference = new(ArtifactType.Module, registry, repository, tag, digest, new Uri("file:///parent.bicep", UriKind.Absolute));

            var sourceArchive = entrypointUri is { } ?
                SourceArchive.FromStream(SourceArchive.PackSourcesIntoStream(
                    entrypointUri,
                    new Core.Workspaces.ISourceFile[] {
                        SourceFileFactory.CreateBicepFile(entrypointUri, "metadata description = 'bicep module'")
                    }))
                : null;

            var result = BicepExternalSourceRequestHandler.GetExternalSourceLinkUri(localCachedJsonPath, reference, sourceArchive);

            ValidateExternalSourceLinkUri(result);

            return result;
        }

        private void ValidateExternalSourceLinkUri(Uri uri)
        {
            string link = uri.ToString();

            link.Should().StartWith("bicep-extsrc:");
            link.Should().MatchRegex("^bicep-extsrc:(br|ts):");
            //asdfg link.Should().MatchRegex("^([^#]+)#([^#]+)#([^#]+)$", "it should contain exactly two #'s");

            //asdfg DecodeExternalSourceUri(uri).LocalCachedPath.Should().EndWith("main.json");
        }

        private record ExternalSource(
            string Title,
            string ModuleReference,
            string LocalCachedPath
        );

        private ExternalSource DecodeExternalSourceUri(Uri uri) {
            // NOTE: This mimics the code in src\vscode-bicep\src\language\bicepExternalSourceContentProvider.ts
            string title = HttpUtility.UrlDecode(uri.AbsolutePath);
            string fragmentWithoutLeadingHash = uri.Fragment.Substring(1);

            int hashIndex = fragmentWithoutLeadingHash.IndexOf("#");
            hashIndex.Should().BeGreaterThan(0, "Should find a # in the fragment");

            string moduleReference = HttpUtility.UrlDecode(fragmentWithoutLeadingHash.Substring(0, hashIndex));
            string localPath = HttpUtility.UrlDecode((fragmentWithoutLeadingHash.Substring(hashIndex + 1)));

            return new ExternalSource(title, moduleReference, localPath);

            //asdfg getting: br:myregistry.azurecr.io/file:///bicep/myrepo/module1:v1
        }
    }
}
