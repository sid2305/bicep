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
public class SourceCodePositionTests
{
    public TestContext? TestContext { get; set; }

    [TestMethod]
    public void SerializesAndDeserializes()
    {
        var position = new SourceCodePosition(123, 456);
        string serialized = JsonSerializer.Serialize(position);

        serialized.Should().Be("\"[123:456]\"");

        var deserialized = JsonSerializer.Deserialize<SourceCodePosition>(serialized);
        deserialized.Should().Be(position);
    }
}
