// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Syntax;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Mock;
using Bicep.Core.Workspaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using static Bicep.Core.Diagnostics.DiagnosticBuilder;

namespace Bicep.Core.UnitTests.Registry;

[TestClass]
public class SourceArchiveTests
{
    [TestMethod]
    public void PackSources_asdfg() this fails
    {
        const string projectFolder = "/my project/my sources";
        var fs = new MockFileSystem();
        fs.AddDirectory(projectFolder);

        const string mainBicepContents = @"
            targetScope = 'subscription'
            metadata description = 'fake bicep file'";
        fs.AddFile(Path.Combine(projectFolder, "main.bicep"), mainBicepContents);

        const string mainJsonContents = @"{
          ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
          ""contentVersion"": ""1.0.0.0"",
          ""resources"": {},
          ""parameters"": {
            ""objectParameter"": {
              ""type"": ""object""
            }
          }
        }";
        fs.AddFile(Path.Combine(projectFolder, "main.json"), mainJsonContents);

        var bicepMain = SourceFileFactory.CreateBicepFile(new Uri("file:///main.bicep"), mainBicepContents);
        var jsonMain = SourceFileFactory.CreateArmTemplateFile(new Uri("file:///main.json"), mainJsonContents);
        using var stream = SourceArchive.PackSources(bicepMain.FileUri, bicepMain, jsonMain);

        SourceArchive sourceArchive = new SourceArchive(stream);

        sourceArchive.GetEntrypointUri().Should().Be(bicepMain.FileUri); //asdfg ?

        var archivedFiles = sourceArchive.GetSourceFiles().ToArray();
        archivedFiles.Should().BeEquivalentTo(
            new (SourceArchive.FileMetadata, string)[] {
                (new (bicepMain.FileUri, "main.bicep", SourceArchive.SourceKind_Bicep), mainBicepContents ),
                (new (jsonMain.FileUri, "main.json", SourceArchive.SourceKind_ArmTemplate), mainJsonContents )
            });
    }

    [TestMethod]
    public void GetSourceFiles_ForwardsCompat_ShouldIgnoreUnrecognizedPropertiesInMetadata()
    {
        var zip = CreateZipFileStream(
            (
                "__metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""I am an unrecognized property name"": {},
                  ""sourceFiles"": [
                    {
                      ""uri"": ""file:///main.bicep"",
                      ""archivedPath"": ""main.bicep"",
                      ""kind"": ""bicep"",
                      ""I am also recognition challenged"": ""Hi, Mom!""
                    }
                  ]
                }"
            ),
            (
                "main.bicep",
                @"bicep contents"
            )
        );

        var sut = new SourceArchive(zip);
        var file = sut.GetSourceFiles().Single();

        file.Metadata.Kind.Should().Be("bicep");
        file.Contents.Should().Be("bicep contents");
        file.Metadata.Uri.AbsolutePath.Should().Contain("main.bicep");
    }

    [TestMethod]
    public void GetSourceFiles_BackwardsCompat_ShouldBeAbleToReadOldFormats()
    {
        // DO NOT ADD TO THIS DATA - IT IS MEANT TO TEST READING
        // OLD FILE VERSIONS WITH MINIMAL DATA
        var zip = CreateZipFileStream(
            (
                "__metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""sourceFiles"": [
                    {
                      ""uri"": ""file:///main.bicep"",
                      ""archivedPath"": ""main.bicep"",
                      ""kind"": ""bicep""
                    }
                  ]
                }"
            ),
            (
                "main.bicep",
                @"bicep contents"
            )
        );

        var sut = new SourceArchive(zip);
        var file = sut.GetSourceFiles().Single();

        file.Metadata.Kind.Should().Be("bicep");
        file.Contents.Should().Be("bicep contents");
        file.Metadata.Uri.AbsolutePath.Should().Contain("main.bicep");
    }

    [TestMethod]
    public void GetSourceFiles_ForwardsCompat_ShouldIgnoreFileEntriesNotInMetadata()
    {
        var zip = CreateZipFileStream(
            (
                "__metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""I am an unrecognized property name"": {},
                  ""sourceFiles"": [
                    {
                      ""uri"": ""file:///main.bicep"",
                      ""archivedPath"": ""main.bicep"",
                      ""kind"": ""bicep"",
                      ""I am also recognition challenged"": ""Hi, Mom!""
                    }
                  ]
                }"
            ),
            (
                "I'm not mentioned in metadata.bicep",
                @"unmentioned contents"
            ),
            (
                "main.bicep",
                @"bicep contents"
            )
        );

        var sut = new SourceArchive(zip);
        var file = sut.GetSourceFiles().Single();

        file.Metadata.Kind.Should().Be("bicep");
        file.Contents.Should().Be("bicep contents");
        file.Metadata.Uri.AbsolutePath.Should().Contain("main.bicep");
    }

    private Stream CreateZipFileStream(params (string relativePath, string contents)[] files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (relativePath, contents) in files)
            {
                using var entryStream = archive.CreateEntry(relativePath).Open();
                using var sw = new StreamWriter(entryStream);
                sw.Write(contents);
            }
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
