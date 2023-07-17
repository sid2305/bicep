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
public class SourceBundleTests
{
    [TestMethod]
    public void asdfg()
    {
        const string projectFolder = "/my project/my sources";
        //const string bundleFolder = "/my module cache/my sources";
        var fs = new MockFileSystem();
        fs.AddDirectory(projectFolder);
        const string mainBicepContents = @"targetScope = 'subscription'
metadata description = 'fake bicep file'";
        fs.AddFile(Path.Combine(projectFolder, "main.bicep"), mainBicepContents);

        var bicepMain = SourceFileFactory.CreateBicepFile(new Uri("file:///main.bicep"), mainBicepContents);
        using var stream = SourceBundle.PackSources(bicepMain.FileUri, bicepMain);

        using var test = File.OpenWrite("/Users/stephenweatherford/test.zip"); //asdfg
        stream.CopyTo(test);
        test.Close();

        stream.Seek(0, SeekOrigin.Begin);


        //SourceBundle.UnpackSources()
        SourceBundle sourceBundle = new SourceBundle(stream);
        //SourceBundle sourceBundle = new SourceBundle(fs, bundleFolder);

        //
    }

    [TestMethod]
    public void ForwardsCompat_ShouldIgnoreUnrecognizedPropertiesInMetadata()
    {
        var zip = CreateZipFile(
            (
                "__metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""I am an unrecognized property name"": {},
                  ""sourceFiles"": [
                    {
                      ""uri"": ""file:///main.bicep"",
                      ""localPath"": ""main.bicep"",
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

        var sut = new SourceBundle(zip);
        var file = sut.GetSourceFiles().Single();

        file.metadata.Kind.Should().Be("bicep");
        file.contents.Should().Be("bicep contents");
        file.metadata.Uri.AbsolutePath.Should().Contain("main.bicep");
    }

    [TestMethod]
    public void BackwardsCompat_ShouldBeAbleToReadOldFormats()
    {
        // DO NOT ADD TO DATA - IT IS MEANT TO TEST READING
        // OLD FILE VERSIONS WITH MINIMAL DATA
        var zip = CreateZipFile(
            (
                "__metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""sourceFiles"": [
                    {
                      ""uri"": ""file:///main.bicep"",
                      ""localPath"": ""main.bicep"",
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

        var sut = new SourceBundle(zip);
        var file = sut.GetSourceFiles().Single();

        file.metadata.Kind.Should().Be("bicep");
        file.contents.Should().Be("bicep contents");
        file.metadata.Uri.AbsolutePath.Should().Contain("main.bicep");
    }

    [TestMethod]
    public void ForwardsCompat_ShouldIgnoreFileEntriesNotInMetadata()
    {
        var zip = CreateZipFile(
            (
                "__metadata.json",
                @"
                {
                  ""entryPoint"": ""file:///main.bicep"",
                  ""I am an unrecognized property name"": {},
                  ""sourceFiles"": [
                    {
                      ""uri"": ""file:///main.bicep"",
                      ""localPath"": ""main.bicep"",
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

        var sut = new SourceBundle(zip);
        var file = sut.GetSourceFiles().Single();

        file.metadata.Kind.Should().Be("bicep");
        file.contents.Should().Be("bicep contents");
        file.metadata.Uri.AbsolutePath.Should().Contain("main.bicep");
    }

    private Stream CreateZipFile(params (string relativePath, string contents)[] files) {
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
