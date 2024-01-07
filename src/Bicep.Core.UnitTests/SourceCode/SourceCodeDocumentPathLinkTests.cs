// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using Bicep.Core.Parsing;
using Bicep.Core.SourceCode;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Utils;
using Bicep.Core.Workspaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.Core.UnitTests.SourceCode;

[TestClass]
public class SourceCodeDocumentPathLinkTests
{
    [TestMethod]
    public void SerializesAndDeserializes()
    {
        var link = new SourceCodeDocumentPathLink(
            new SourceCodeRange(123, 456, 234, 567),
            "../modules/target.bicep",
            new SourceCodeRange(234, 567, 345, 456),
            new SourceCodeRange(123, 123, 234, 234));
        string serialized = JsonSerializer.Serialize(link);

        serialized.Should().Be("{\"Range\":\"[123:456]-[234:567]\","
            + "\"Target\":\"../modules/target.bicep\","
            + "\"TargetRange\":\"[234:567]-[345:456]\","
            + "\"TargetSelectionRange\":\"[123:123]-[234:234]\"}");

        var deserialized = JsonSerializer.Deserialize<SourceCodeDocumentPathLink>(serialized);
        deserialized.Should().Be(link);
    }

    [TestMethod]
    public void SerializesAndDeserializes_WithNullValues1()
    {
        var link = new SourceCodeDocumentPathLink(
            new SourceCodeRange(123, 456, 234, 567),
            "../modules/target.bicep",
            null,
            new SourceCodeRange(123, 123, 234, 234));
        string serialized = JsonSerializer.Serialize(link);
        var deserialized = JsonSerializer.Deserialize<SourceCodeDocumentPathLink>(serialized);
        deserialized.Should().Be(link);
    }

    [TestMethod]
    public void SerializesAndDeserializes_WithNullValues2()
    {
        var link = new SourceCodeDocumentPathLink(
            new SourceCodeRange(123, 456, 234, 567),
            "../modules/target.bicep",
            new SourceCodeRange(234, 567, 345, 456),
            null);
        string serialized = JsonSerializer.Serialize(link);
        var deserialized = JsonSerializer.Deserialize<SourceCodeDocumentPathLink>(serialized);
        deserialized.Should().Be(link);
    }

    [TestMethod]
    public void SerializesAndDeserializes_WithNullValues3()
    {
        var link = new SourceCodeDocumentPathLink(
            new SourceCodeRange(123, 456, 234, 567),
            "../modules/target.bicep",
            null,
            null);
        string serialized = JsonSerializer.Serialize(link);
        var deserialized = JsonSerializer.Deserialize<SourceCodeDocumentPathLink>(serialized);
        deserialized.Should().Be(link);
    }
}
