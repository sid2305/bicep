// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using Bicep.Core.Diagnostics;
using Bicep.Core.UnitTests;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;

namespace Bicep.Core.IntegrationTests
{
    [TestClass]
    public class TesExpTests
    {
        [TestMethod]
        public async Task Testmock()
        {
            var directoryPath = "C:\\Users\\slahoti\\OneDrive - Microsoft\\Desktop\\bicep\\src\\Bicep.Core.IntegrationTests\\test";

            Dictionary<string, string> fileContentDictionary = new();

            foreach (string filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                string fileContent = File.ReadAllText(filePath);
                string relativePath = Path.GetRelativePath(directoryPath, filePath).Replace(Path.DirectorySeparatorChar, '/');
                fileContentDictionary.Add(relativePath, fileContent);
            }

            IReadOnlyDictionary<string, string> fileSystem = new ReadOnlyDictionary<string, string>(fileContentDictionary);

            var services = new ServiceBuilder()
            .WithMockFileSystem(fileSystem);

            IReadOnlyList<(string, string)> convertedFileSystem = fileContentDictionary
            .Select(kv => (kv.Key, kv.Value))
            .ToList();

            var result = await CompilationHelper.RestoreAndCompile(services,convertedFileSystem.ToArray());

            result.Should().NotHaveAnyDiagnostics();
            result.Template.Should().NotBeNull();
        }
    }
}
