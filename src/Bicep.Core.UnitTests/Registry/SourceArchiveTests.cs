// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Bicep.Core.FileSystem;
using Bicep.Core.SourceCode;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Utils;
using Bicep.Core.Workspaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using static Microsoft.SRM.DecisionTree;

namespace Bicep.Core.UnitTests.SourceCode;

[TestClass]
public class SourceArchiveTests
{
    public TestContext? TestContext { get; set; }

    private const string MainDotBicepSource = @"
        targetScope = 'subscription'
        // Module description
        metadata description = 'fake main bicep file'";

    private const string MainDotJsonSource = @"{
        ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
        ""contentVersion"": ""1.0.0.0"",
        ""resources"": {
        // Some people like this formatting
        },
        ""parameters"": {
        ""objectParameter"": {
            ""type"": ""object""
        }
        }
    }";

    private const string SecondaryDotBicepSource = @"
        // Module description
        metadata description = 'fake secondary bicep file'
    ";

    private const string StandaloneJsonSource = @"{
        // This file is a module that was referenced directly via JSON
        ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
        ""contentVersion"": ""1.0.0.0"",
        ""resources"": [],
        ""parameters"": {
            ""secureStringParam"": {
                ""type"": ""securestring""
            }
        }
    }";

    private const string TemplateSpecJsonSource = @"{
            // Template spec
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""metadata"": {
                ""_generator"": {
                    ""name"": ""bicep"",
                    ""version"": ""0.17.1.54307"",
                    ""templateHash"": ""3268788020119860428""
                },
                ""description"": ""my template spec description storagespec v2""
            },
            ""parameters"": {
                ""storageAccountType"": {
                    ""type"": ""string"",
                    ""defaultValue"": ""Standard_LRS"",
                    ""allowedValues"": [
                        ""Standard_LRS"",
                        ""Standard_GRS"",
                        ""Standard_ZRS"",
                        ""Premium_LRS""
                    ]
                },
                ""loc"": {
                    ""type"": ""string"",
                    ""defaultValue"": ""[resourceGroup().location]""
                }
            },
            ""variables"": {
                ""prefix"": ""mytest""
            },
            ""resources"": [
                {
                    ""type"": ""Microsoft.Storage/storageAccounts"",
                    ""apiVersion"": ""2021-04-01"",
                    ""name"": ""[format('{0}{1}', variables('prefix'), uniqueString(resourceGroup().id))]"",
                    ""location"": ""[parameters('loc')]"",
                    ""sku"": {
                        ""name"": ""[parameters('storageAccountType')]""
                    },
                    ""kind"": ""StorageV2""
                }
            ]
        }";

    private const string LocalModuleDotJsonSource = @"{
        // localModule.json
        ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
        ""contentVersion"": ""1.0.0.0"",
        ""resources"": [],
        ""parameters"": {
            ""stringParam"": {
                ""type"": ""string""
            }
        }
    }";

    private ISourceFile CreateSourceFile(MockFileSystem fs, Uri projectFolderUri, string relativePath, string sourceKind, string content)
    {
        projectFolderUri.AbsolutePath.Should().EndWith("/");
        Uri uri = new(projectFolderUri, relativePath);
        fs.AddFile(uri.LocalPath, content);
        string actualContents = fs.File.ReadAllText(uri.LocalPath);
        return sourceKind switch
        {
            SourceArchive.SourceKind_ArmTemplate => SourceFileFactory.CreateArmTemplateFile(uri, actualContents),
            SourceArchive.SourceKind_Bicep => SourceFileFactory.CreateSourceFile(uri, actualContents),
            SourceArchive.SourceKind_TemplateSpec => SourceFileFactory.CreateTemplateSpecFile(uri, actualContents),
            _ => throw new Exception($"Unrecognized source kind: {sourceKind}")
        };
    }

    [TestMethod]
    public void CanPackAndUnpackSourceFiles()
    {
        Uri projectFolder = new("file:///my project/my sources/", UriKind.Absolute);
        var fs = new MockFileSystem();
        fs.AddDirectory(projectFolder.LocalPath);

        var mainBicep = CreateSourceFile(fs, projectFolder, "main.bicep", SourceArchive.SourceKind_Bicep, MainDotBicepSource);
        var mainJson = CreateSourceFile(fs, projectFolder, "main.json", SourceArchive.SourceKind_ArmTemplate, MainDotJsonSource);
        var standaloneJson = CreateSourceFile(fs, projectFolder, "standalone.json", SourceArchive.SourceKind_ArmTemplate, StandaloneJsonSource);
        var templateSpecMainJson = CreateSourceFile(fs, projectFolder, "Main template.json", SourceArchive.SourceKind_TemplateSpec, TemplateSpecJsonSource);
        var localModuleJson = CreateSourceFile(fs, projectFolder, "localModule.json", SourceArchive.SourceKind_ArmTemplate, LocalModuleDotJsonSource);

        using var stream = SourceArchive.PackSourcesIntoStream(mainBicep.FileUri, mainBicep, mainJson, standaloneJson, templateSpecMainJson, localModuleJson);
        stream.Length.Should().BeGreaterThan(0);

        SourceArchive? sourceArchive = SourceArchive.UnpackFromStream(stream).UnwrapOrThrow();
        sourceArchive!.EntrypointRelativePath.Should().Be("main.bicep");


        var archivedFiles = sourceArchive.SourceFiles.ToArray();
        archivedFiles.Should().BeEquivalentTo(
            new SourceArchive.SourceFileInfo[] {
                new ("main.bicep", "files/main.bicep", SourceArchive.SourceKind_Bicep, MainDotBicepSource),
                new ("main.json", "files/main.json", SourceArchive.SourceKind_ArmTemplate, MainDotJsonSource ),
                new ("standalone.json", "files/standalone.json", SourceArchive.SourceKind_ArmTemplate, StandaloneJsonSource),
                new ("Main template.json", "files/Main template.json", SourceArchive.SourceKind_TemplateSpec,  TemplateSpecJsonSource),
                new ("localModule.json", "files/localModule.json", SourceArchive.SourceKind_ArmTemplate,  LocalModuleDotJsonSource),
            });
    }

    [TestMethod]
    public void CanPackAndUnpackDocumentLinks()
    {
        Uri projectFolder = new("file:///my project/my sources/", UriKind.Absolute);
        var fs = new MockFileSystem();
        fs.AddDirectory(projectFolder.LocalPath);

        var mainBicep = CreateSourceFile(fs, projectFolder, "main.bicep", SourceArchive.SourceKind_Bicep, MainDotBicepSource);
        var mainJson = CreateSourceFile(fs, projectFolder, "main.json", SourceArchive.SourceKind_ArmTemplate, MainDotJsonSource);
        var standaloneJson = CreateSourceFile(fs, projectFolder, "standalone.json", SourceArchive.SourceKind_ArmTemplate, StandaloneJsonSource);
        var templateSpecMainJson = CreateSourceFile(fs, projectFolder, "Main template.json", SourceArchive.SourceKind_TemplateSpec, TemplateSpecJsonSource);
        var localModuleJson = CreateSourceFile(fs, projectFolder, "localModule.json", SourceArchive.SourceKind_ArmTemplate, LocalModuleDotJsonSource);

        var dict = new Dictionary<Uri, SourceCodeDocumentUriLink[]>()
        {
            {
                new Uri("file:///my project/my sources/main.bicep", UriKind.Absolute),
                new SourceCodeDocumentUriLink[]
                {
                    new SourceCodeDocumentUriLink(new SourceCodeRange(1, 2, 1, 3), new Uri("file:///my project/my sources/modules/module1.bicep", UriKind.Absolute)),
                }
            },
            {
                new Uri("file:///my project/my sources/modules/module1.bicep", UriKind.Absolute),
                new SourceCodeDocumentUriLink[]
                {
                    new SourceCodeDocumentUriLink(new SourceCodeRange(123, 124, 234, 235), new Uri("file:///my project/my sources/main.bicep", UriKind.Absolute)),
                    new SourceCodeDocumentUriLink(new SourceCodeRange(234, 235, 345, 346), new Uri("file:///my project/my sources/remote/main.json", UriKind.Absolute)),
                    new SourceCodeDocumentUriLink(new SourceCodeRange(123, 456, 234, 567), new Uri("file:///my project/my sources/main.bicep", UriKind.Absolute)),
                }
            },
        };

        using var stream = SourceArchive.PackSourcesIntoStream(mainBicep.FileUri, dict, mainBicep, mainJson, standaloneJson, templateSpecMainJson, localModuleJson);
        stream.Length.Should().BeGreaterThan(0);

        SourceArchive? sourceArchive = SourceArchive.UnpackFromStream(stream).TryUnwrap();
        sourceArchive.Should().NotBeNull();

        var links = sourceArchive!.DocumentLinks;

        var expected = new Dictionary<string, SourceCodeDocumentPathLink[]>()
        {
            {
                "main.bicep",
                new SourceCodeDocumentPathLink[]
                {
                    new SourceCodeDocumentPathLink(new SourceCodeRange(1, 2, 1, 3), "modules/module1.bicep"),
                }
            },
            {
                "modules/module1.bicep",
                new SourceCodeDocumentPathLink[]
                {
                    new SourceCodeDocumentPathLink(new SourceCodeRange(123, 124, 234, 235), "main.bicep"),
                    new SourceCodeDocumentPathLink(new SourceCodeRange(234, 235, 345, 346), "remote/main.json"),
                    new SourceCodeDocumentPathLink(new SourceCodeRange(123, 456, 234, 567), "main.bicep"),
                }
            },
        };

        links.Should().BeEquivalentTo(expected);
    }

    [DataRow(
        new string[] { "c:/my root/my project/my main.bicep", "c:/my other.bicep" },
        new string[] { "my root/my project/my main.bicep", "my other.bicep" },
        new string[] { "files/my root/my project/my main.bicep", "files/my other.bicep" },
        DisplayName = "HandlesPathsCorrectly: spaces")]
    [DataRow(
        new string[] { "c:\\my root\\my project\\my main.bicep", "c:/my other.bicep" },
        new string[] { "my root/my project/my main.bicep", "my other.bicep" },
        new string[] { "files/my root/my project/my main.bicep", "files/my other.bicep" },
        DisplayName = "HandlesPathsCorrectly: backslashes")]
    /*[DataRow(
        "c:\\my root\\my project\\my main.bicep",
        "subfolder\\my other.bicep",
        "my main.bicep",
        "subfolder/my other.bicep",
        "files/subfolder/my other.bicep",
        DisplayName = "HandlesPathsCorrectly: backslash")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "/my root/my project/sub folder/my other bicep.bicep",
        "my main.bicep",
        "sub folder/my other bicep.bicep",
        "files/sub folder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: subfolder")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my root/my project/sub folder/my other bicep.bicep",
        "my main.bicep",
        "sub folder/my other bicep.bicep",
        "files/sub folder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: subfolder")]
    [DataRow(
        "c:\\my root\\my project\\my main.bicep",
        "c:\\my root/my project\\sub folder\\my other bicep.bicep",
        "my main.bicep",
        "sub folder/my other bicep.bicep",
        "files/sub folder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: windows root format")]
    [DataRow(
        "/my root/my project/my main.bicep",
        "/my root/my project/sub folder/my other bicep.bicep",
        "my main.bicep",
        "sub folder/my other bicep.bicep",
        "files/sub folder/my other bicep.bicep",
        "linux",
        DisplayName = "HandlesPathsCorrectly: Linux root format")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "\\my root\\my project\\sub folder\\my other bicep.bicep",
        "my main.bicep",
        "sub folder/my other bicep.bicep",
        "files/sub folder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: subfolder")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "/my root/my project/sub folder/sub folder 2/my other bicep.bicep",
        "my main.bicep",
        "sub folder/sub folder 2/my other bicep.bicep",
        "files/sub folder/sub folder 2/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: sub-subfolder")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "/my root/my other bicep.bicep",
        "my main.bicep",
        "../my other bicep.bicep",
        "files/parent/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: ../")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "/my other bicep.bicep",
        "my main.bicep",
        "../../my other bicep.bicep",
        "files/parent/parent/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: ../../")]
    [DataRow(
        "c:/my root/my project/my project2/my main.bicep",
        "c:/my other bicep.bicep",
        "my main.bicep",
        "../../../my other bicep.bicep",
        "files/parent/parent/parent/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: ../../../")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "/my root/..folder/my other bicep.bicep",
        "my main.bicep",
        "../..folder/my other bicep.bicep",
        "files/parent/..folder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: ..folder")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "/folder/my other bicep.bicep",
        "my main.bicep",
        "../../folder/my other bicep.bicep",
        "files/parent/parent/folder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: ../../folder")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my other root/my project/my other bicep.bicep",
        "my main.bicep",
        "../../my other root/my project/my other bicep.bicep",
        "files/parent/parent/my other root/my project/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: no folders in common")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my root/my..project/..my other bicep.bicep",
        "my main.bicep",
        "../my..project/..my other bicep.bicep",
        "files/parent/my..project/..my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: .. at beginning of filename")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my root/my..project/my other bicep.bicep..",
        "my main.bicep",
        "../my..project/my other bicep.bicep..",
        "files/parent/my..project/my other bicep.bicep..",
        DisplayName = "HandlesPathsCorrectly: .. at end of filename")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my root/my..project/my other..bicep.bicep",
        "my main.bicep",
        "../my..project/my other..bicep.bicep",
        "files/parent/my..project/my other..bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: .. in middle of filename")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my root/my project/subfolder/my other bicep.bicep",
        "my main.bicep",
        "subfolder/my other bicep.bicep",
        "files/subfolder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: rooted to drive: slashes")]
    [DataRow(
        "c:\\my root\\my project\\my main.bicep",
        "c:/my root/my project/subfolder/my other bicep.bicep",
        "my main.bicep",
        "subfolder/my other bicep.bicep",
        "files/subfolder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: rooted to drive: backslashes 1")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:\\my root\\my project\\subfolder\\my other bicep.bicep",
        "my main.bicep",
        "subfolder/my other bicep.bicep",
        "files/subfolder/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: rooted to drive: backslashes 2")]
    [DataRow(
        // This shouldn't ever happen, with the exception of when the cache root path is on another drive, because local module files must be relative to the referencing file.
        "c:/my root/my project/my main.bicep",
        "d:/my root/my project/my other bicep.bicep",
        "my main.bicep",
        "d:/my root/my project/my other bicep.bicep",
        "files/d_/my root/my project/my other bicep.bicep",
        DisplayName = "HandlesPathsCorrectly: separate drives")]
    [DataRow(
        //asdfg
        "c:/my root/my project/my main.bicep",
        "c:/Users/username/.bicep/br/mcr.microsoft.com/bicep$storage$storage-account/1.0.1$/main.json",
        "my main.bicep",
        "<cache>/mcr.microsoft.com/bicep$storage$storage-account/1.0.1$/main.json",
        "_cache_/mcr.microsoft.com/bicep$storage$storage-account/1.0.1$/main.json",
        DisplayName = "HandlesPathsCorrectly: external module (in cache)")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my root/my [&] mainProject/my &[] main.bicep",
        "my main.bicep",
        "../my [&] mainProject/my &[] main.bicep",
        "files/parent/my ___ mainProject/my ___ main.bicep",
        DisplayName = "HandlesPathsCorrectly:  Characters to avoid")]
    [DataRow(
        "c:/my root/my project/my main.bicep",
        "c:/my other root/אמא שלי/אבא שלי.bicep",
        "my main.bicep",
        "../../my other root/אמא שלי/אבא שלי.bicep",
        "files/parent/parent/my other root/אמא שלי/אבא שלי.bicep",
        DisplayName = "HandlesPathsCorrectly:  Global characters")]*/
    [DataTestMethod]
    public void HandlesPathsCorrectly(
        string[] inputPaths,
        string[] expectedPaths,
        string[] expectedArchivePaths,
        string? platform = null)
    {
        if (platform == "linux" && Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            // skip
            return;
        }

        var fs = new MockFileSystem();

        var entrypointPath = inputPaths[0];

        var rootBicepFolder = new Uri(Path.GetDirectoryName(entrypointPath)! + "/", UriKind.Absolute);
        fs.AddDirectory(rootBicepFolder.LocalPath);

        var files = inputPaths.Select(path => CreateSourceFile(fs, rootBicepFolder, path, SourceArchive.SourceKind_Bicep, $"// {path}")).ToArray();

        using var stream = SourceArchive.PackSourcesIntoStream(files[0].FileUri, files);
        SourceArchive sourceArchive = SourceArchive.UnpackFromStream(stream).UnwrapOrThrow();

        sourceArchive.EntrypointRelativePath.Should().Be(expectedPaths[0], "entrypoint path should be correct");

        sourceArchive.EntrypointRelativePath.Should().NotContain("username", "shouldn't have username in source paths");
        foreach (var file in sourceArchive.SourceFiles)
        {
            file.Path.Should().NotContain("username", "shouldn't have username in source paths");
            file.ArchivePath.Should().NotContain("username", "shouldn't have username in source paths");
        }

        for (int i = 0; i < inputPaths.Length; ++i)
        {
            var archivedTestFile = sourceArchive.SourceFiles.Single(f => f.Contents.Equals(files[i].GetOriginalSource()));
            archivedTestFile.Path.Should().Be(expectedPaths[i]);
            archivedTestFile.ArchivePath.Should().Be(expectedArchivePaths[i]);
        }
    }

    [DataRow(
        "c:/my root/my project/my_.bicep", "my_.bicep", "files/my_.bicep",
        "c:/my root/my project/my&.bicep", "my&.bicep", "files/my_.bicep(2)",
        "c:/my root/my project/my[.bicep", "my[.bicep", "files/my_.bicep(3)",
        DisplayName = "DuplicateNamesAfterMunging_ShouldHaveSeparateEntries: &")]
    [DataRow(
        "c:\\my root\\my project\\123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789\\a.txt",
            "123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789/a.txt",
            "files/123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123__path_too_long__.txt",
        "c:\\my root\\my project\\123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789\\b.txt",
            "123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789/b.txt",
            "files/123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123__path_too_long__.txt(2)",
        "c:\\my root\\my project\\123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 extra characters here that get truncated\\a.txt",
            "123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 extra characters here that get truncated/a.txt",
            "files/123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123__path_too_long__.txt(3)",
        DisplayName = "DuplicateNamesAfterMunging_ShouldHaveSeparateEntries: truncated")]
    [DataRow(
        "c:/my root/my.bicep", "my.bicep", "files/my.bicep",
        "d:/my root/my project/parent/m&.bicep", "<root2>/m&.bicep", "files/_root2_/m_.bicep",
        "d:/my root/my project/parent/m[.bicep", "<root2>/m[.bicep", "files/_root2_/m_.bicep(2)",
        DisplayName = "DuplicateNamesAfterMunging_ShouldHaveSeparateEntries: different drives")]
    [DataTestMethod]
    public void DuplicateNamesAfterMunging_ShouldHaveSeparateEntries(
        string inputBicepPath1, string expectedPath1, string expectedArchivePath1, // 1st bicep path, plus its expected path and archive path
        string inputBicepPath2, string expectedPath2, string expectedArchivePath2, // 2st bicep path, plus its expected path and archive path
        string? inputBicepPath3 = null, string? expectedPath3 = null, string? expectedArchivePath3 = null  // 3rd bicep path, plus its expected path and archive path
    )
    {
        string entrypointPath = "c:/my root/my project/my entrypoint.bicep";
        var fs = new MockFileSystem();

        var rootBicepFolder = new Uri(Path.GetDirectoryName(entrypointPath)! + "/", UriKind.Absolute);
        fs.AddDirectory(rootBicepFolder.LocalPath);

        var entrypointFile = CreateSourceFile(fs, rootBicepFolder, Path.GetFileName(entrypointPath), SourceArchive.SourceKind_Bicep, MainDotBicepSource);
        var sutFile1 = CreateSourceFile(fs, rootBicepFolder, inputBicepPath1, SourceArchive.SourceKind_Bicep, SecondaryDotBicepSource);
        var sutFile2 = CreateSourceFile(fs, rootBicepFolder, inputBicepPath2, SourceArchive.SourceKind_Bicep, SecondaryDotBicepSource);
        var sutFile3 = inputBicepPath3 is null ? null : CreateSourceFile(fs, rootBicepFolder, inputBicepPath3, SourceArchive.SourceKind_Bicep, SecondaryDotBicepSource);

        using var stream = sutFile3 is null ?
            SourceArchive.PackSourcesIntoStream(entrypointFile.FileUri, entrypointFile, sutFile1, sutFile2) :
            SourceArchive.PackSourcesIntoStream(entrypointFile.FileUri, entrypointFile, sutFile1, sutFile2, sutFile3);

        SourceArchive sourceArchive = SourceArchive.UnpackFromStream(stream).UnwrapOrThrow();

        var archivedFile1 = sourceArchive.SourceFiles.SingleOrDefault(f => f.Path == expectedPath1);
        var archivedFile2 = sourceArchive.SourceFiles.SingleOrDefault(f => f.Path == expectedPath2);
        var archivedFile3 = sourceArchive.SourceFiles.SingleOrDefault(f => f.Path == expectedPath3);

        archivedFile1.Should().NotBeNull($"Couldn't find source file \"{inputBicepPath1}\" in archive");
        archivedFile2.Should().NotBeNull($"Couldn't find source file \"{inputBicepPath2}\" in archive");
        if (inputBicepPath3 is not null)
        {
            archivedFile3.Should().NotBeNull($"Couldn't find source file \"{inputBicepPath3}\" in archive");
        }

        archivedFile1!.Path.Should().Be(expectedPath1);
        archivedFile1.ArchivePath.Should().Be(expectedArchivePath1);

        archivedFile2!.Path.Should().Be(expectedPath2);
        archivedFile2.ArchivePath.Should().Be(expectedArchivePath2);

        if (inputBicepPath3 is not null)
        {
            archivedFile3!.Path.Should().Be(expectedPath3);
            archivedFile3.ArchivePath.Should().Be(expectedArchivePath3);
        }
    }

    //asdfg test duplicates after munge
    //asdfg including real folder starts with name "parent"

    [TestMethod]
    public void GetSourceFiles_ForwardsCompat_ShouldIgnoreUnrecognizedPropertiesInMetadata()
    {
        var zip = CreateGzippedTarredFileStream(
            (
                "metadata.json",
                @"
                {
                  ""metadataVersion"": 1,
                  ""entryPoint"": ""file:///main.bicep"",
                  ""I am an unrecognized property name"": {},
                  ""bicepVersion"": ""0.18.19"",
                  ""sourceFiles"": [
                    {
                      ""path"": ""file:///main.bicep"",
                      ""archivePath"": ""files/main.bicep"",
                      ""kind"": ""bicep"",
                      ""I am also recognition challenged"": ""Hi, Mom!""
                    }
                  ]
                }"
            ),
            (
                "files/main.bicep",
                @"bicep contents"
            )
        );

        var sut = SourceArchive.UnpackFromStream(zip).UnwrapOrThrow();
        var file = sut.SourceFiles.Single();

        file.Kind.Should().Be("bicep");
        file.Contents.Should().Be("bicep contents");
        file.Path.Should().Contain("main.bicep");
    }

    [TestMethod]
    public void GetSourceFiles_BackwardsCompat_ShouldBeAbleToReadOldFormats()
    {
        // DO NOT ADD TO THIS DATA - IT IS MEANT TO TEST READING
        // OLD FILE VERSIONS WITH MINIMAL DATA
        var zip = CreateGzippedTarredFileStream(
            (
                "metadata.json",
                @"
                {
                  ""entryPoint"": ""main.bicep"",
                  ""bicepVersion"": ""0.1.2"",
                  ""metadataVersion"": 1,
                  ""sourceFiles"": [
                    {
                      ""path"": ""main.bicep"",
                      ""archivePath"": ""files/main.bicep"",
                      ""kind"": ""bicep""
                    }
                  ]
                }"
            ),
            (
                "files/main.bicep",
                "bicep contents"
            )
        );

        var sut = SourceArchive.UnpackFromStream(zip).UnwrapOrThrow();
        var file = sut.SourceFiles.Single();

        file.Kind.Should().Be("bicep");
        file.Contents.Should().Be("bicep contents");
        file.Path.Should().Be("main.bicep");
    }

    [TestMethod]
    public void GetSourceFiles_ForwardsCompat_ShouldIgnoreFileEntriesNotInMetadata()
    {
        var zip = CreateGzippedTarredFileStream(
            (
                "metadata.json",
                @"
                {
                  ""entryPoint"": ""main.bicep"",
                  ""I am an unrecognized property name"": {},
                  ""sourceFiles"": [
                    {
                      ""path"": ""main.bicep"",
                      ""archivePath"": ""files/main.bicep"",
                      ""kind"": ""bicep"",
                      ""I am also recognition challenged"": ""Hi, Mom!""
                    }
                  ],
                  ""bicepVersion"": ""0.1.2"",
                  ""metadataVersion"": 1
                }"
            ),
            (
                "I'm not mentioned in metadata.bicep",
                @"unmentioned contents"
            ),
            (
                "files/Nor am I.bicep",
                @"unmentioned contents 2"
            ),
            (
                "files/main.bicep",
                @"bicep contents"
            )
        );

        var sut = SourceArchive.UnpackFromStream(zip).UnwrapOrThrow();
        var file = sut.SourceFiles.Single();

        file.Kind.Should().Be("bicep");
        file.Contents.Should().Be("bicep contents");
        file.Path.Should().Contain("main.bicep");
    }

    [TestMethod]
    public void GetSourceFiles_ShouldGiveError_ForIncompatibleOlderVersion()
    {
        var zip = CreateGzippedTarredFileStream(
            (
                "metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""metadataVersion"": <version>,
                  ""bicepVersion"": ""0.whatever.0"",
                  ""sourceFiles"": [
                    {
                      ""path"": ""file:///main.bicep"",
                      ""archivePath"": ""main.bicep"",
                      ""kind"": ""bicep""
                    }
                  ]
                }".Replace("<version>", (SourceArchive.CurrentMetadataVersion - 1).ToString())
            ),
            (
                "main.bicep",
                @"bicep contents"
            )
        );

        SourceArchive.UnpackFromStream(zip).IsSuccess(out var sourceArchive, out var ex);
        sourceArchive.Should().BeNull();
        ex.Should().NotBeNull();
        ex!.Message.Should().StartWith("This source code was published with an older, incompatible version of Bicep (0.whatever.0). You are using version ");
    }

    [TestMethod]
    public void GetSourceFiles_ShouldGiveError_ForIncompatibleNewerVersion()
    {
        var zip = CreateGzippedTarredFileStream(
            (
                "metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""metadataVersion"": <version>,
                  ""bicepVersion"": ""0.whatever.0"",
                  ""sourceFiles"": [
                    {
                      ""path"": ""file:///main.bicep"",
                      ""archivePath"": ""main.bicep"",
                      ""kind"": ""bicep""
                    }
                  ]
                }".Replace("<version>", (SourceArchive.CurrentMetadataVersion + 1).ToString())
            ),
            (
                "main.bicep",
                @"bicep contents"
            )
        );

        var success = SourceArchive.UnpackFromStream(zip).IsSuccess(out _, out var ex);
        success.Should().BeFalse();
        ex.Should().NotBeNull();
        ex!.Message.Should().StartWith("This source code was published with a newer, incompatible version of Bicep (0.whatever.0). You are using version ");
    }

    private Stream CreateGzippedTarredFileStream(params (string relativePath, string contents)[] files)
    {
        var outFolder = FileHelper.GetUniqueTestOutputPath(TestContext!);
        var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
        {
            using (var tarWriter = new TarWriter(gz, leaveOpen: true))
            {
                foreach (var (relativePath, contents) in files)
                {
                    // Intentionally creating the archive differently than SourceArchive does it.
                    Directory.CreateDirectory(outFolder);
                    var fileName = Path.Join(outFolder, new Guid().ToString());
                    File.WriteAllText(fileName, contents, Encoding.UTF8);
                    tarWriter.WriteEntry(fileName, relativePath);
                }
            }
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}
