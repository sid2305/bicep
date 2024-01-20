// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Bicep.Core.SourceCode;
using Bicep.Core.UnitTests.Assertions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;

namespace Bicep.Core.UnitTests.SourceCode;

[TestClass]
public class SourceCodePathHelperTests
{
    private static string RootC(string relativePath) => Environment.OSVersion.Platform == PlatformID.Win32NT ?
        $"c:/{relativePath}"
        : $"/c{(relativePath == "" ? "" : "/")}{relativePath}";

    private static string RootD(string relativePath) => Environment.OSVersion.Platform == PlatformID.Win32NT ?
        $"d:/{relativePath}"
        : $"/d{(relativePath == "" ? "" : "/")}{relativePath}";

    [DataRow(
        "",
        ""
    )]
    [DataRow(
        "c:\\",
        "c:\\"
    )]
    [DataRow(
        // 250 chars
        "c:\\123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 1\\a.txt",
        "c:\\123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 1\\a.txt"
    )]
    [DataRow(
        // 251 chars
        "c:\\123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 12\\a.txt",
        "c:\\123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456__path_too_long__.txt"
    )]
    [DataRow(
        // long extension
        "c:\\a.123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 txt",
        "c:\\a.123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 12345678__path_too_long__.123456789"
    )]
    [DataTestMethod]
    public void ShortenTest(string input, string expected)
    {
        var actual = SourceCodePathHelper.Shorten(input, 250);
        actual.Should().Be(expected);
    }

    [TestMethod]
    public void GetUniquePathRoots_ShouldVerifyNormalized()
    {
        ((Action)(() => SourceCodePathHelper.GetUniquePathRoots(new[] { RootC("a.txt"), RootC("b.txt"), RootC("folder/c.txt") }))).Should().NotThrow();
        ((Action)(() => SourceCodePathHelper.GetUniquePathRoots(new[] { RootC("a.txt"), RootD("b.txt"), RootC("folder\\c.txt") }))).Should().Throw<ArgumentException>().WithMessage("*normalized*");
    }

    [TestMethod]
    public void GetUniquePathRoots_ShouldThrowIfNotFullyQualified()
    {
        ((Action)(() => SourceCodePathHelper.GetUniquePathRoots(new[] { RootC("a.txt"), RootC("dir/b.txt"), RootC("c.txt") }))).Should().NotThrow();
        ((Action)(() => SourceCodePathHelper.GetUniquePathRoots(new[] { RootC("a.txt"), RootC("dir/b.txt"), "" }))).Should().Throw<ArgumentException>().WithMessage("*qualified*");
        ((Action)(() => SourceCodePathHelper.GetUniquePathRoots(new[] { RootC("a.txt"), RootC("dir/b.txt"), "/" }))).Should().Throw<ArgumentException>().WithMessage("*qualified*");
        ((Action)(() => SourceCodePathHelper.GetUniquePathRoots(new[] { RootC("a.txt"), RootC("dir/b.txt"), "c.txt" }))).Should().Throw<ArgumentException>().WithMessage("*qualified*");
    }

    [TestMethod]
    [DynamicData(nameof(GetGetUniquePathRootsTestcases), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetGetUniquePathRootsDisplayName))]
    public void GetUniquePathRootsTest(object[] objects)
    {
        var data = ((string[] paths, string[] expectedRoots))objects[0];
        var paths = data.paths;
        var expectedRoots = data.expectedRoots;

        var actualRoots = SourceCodePathHelper.GetUniquePathRoots(paths);

        actualRoots.Select(r => r.Value).Distinct().Should().BeEquivalentTo(expectedRoots);
    }

    public static String GetGetUniquePathRootsDisplayName(MethodInfo mi, Object[] objects)
    {
        var data = ((string[] paths, string[] expectedRoots))objects[0];
        var paths = data.paths;

        return string.Join(", ", paths);
    }

    public static IEnumerable<object[]> GetGetUniquePathRootsTestcases()
    {
        object[] data(string[] paths, string[] expectedRoots) => new object[] { (paths, expectedRoots) };

        yield return data(
            new string[]
            {
            },
            new string[]
            {
            });

        yield return data(
            new string[]
            {
                RootC("a.txt")
            },
            new string[]
            {
                    RootC("")
            });

        yield return data(
            new string[]
            {
                RootC("a.txt"),
                RootC("b.txt")
            },
            new string[]
            {
                RootC("")
            });

        yield return data(
            new string[]
            {
                RootC("folder/a.txt")
            },
            new string[]
            {
                RootC("folder")
            });

        yield return data(
            new string[]
            {
                RootC("a.txt"),
                RootC("folder/a.txt"),
            },
            new string[]
            {
                 RootC("")
            });

        yield return data(
            new string[]
            {
                RootC("folder/a.txt"),
                RootC("a.txt"),
            },
            new string[]
            {
                RootC("")
            });

        yield return data(
            new string[]
            {
                RootC("folder/a.txt"),
                RootC("a.txt"),
                RootC("folder/b.txt"),
                RootC("folder/a.bicep"),
                RootD("folder/sub1/a.txt"),
                RootD("folder/sub1/b.txt"),
                RootD("folder/sub1/sub2/a.txt"),
                RootD("folder/sub1/sub2/b.txt"),
                RootD("folder/sub1/sub2/c.txt"),
                RootD("folder2/sub1/sub2/b.txt"),
            },
            new string[]
            {
                RootC(""),
                RootD("folder/sub1"),
                RootD("folder2/sub1/sub2"),
            });

        yield return data(
            new string[]
            {
                RootC("folder/a.txt"),
                RootC("folder2/abc/def/ghi/a.txt"),
                RootC("folder/b.txt"),
                RootC("folder/a.bicep"),
                RootD("folder/sub1/a.txt"),
                RootD("folder/sub1/b.txt"),
                RootD("folder/sub1/sub2/a.txt"),
                RootC("folder2/abc/a.txt"),
                RootD("folder/sub1/sub2/b.txt"),
                RootD("folder/sub1/sub2/c.txt"),
                RootD("folder2/sub1/sub2/b.txt"),
            },
            new string[]
            {
                RootC("folder"),
                RootC("folder2/abc"),
                RootD("folder/sub1"),
                RootD("folder2/sub1/sub2"),
            });

        yield return data(
            new string[]
            {
                RootC("users/username/repos/deployment/src/main.bicep"),
                RootC("users/username/repos/deployment/src/modules/module1.bicep"),
                RootC("users/username/repos/deployment/src/modules/module2.bicep"),
                RootC("users/username/repos/deployment/shared/shared1.bicep"),
                RootD("bicepcacheroot/br/example.azurecr.io/test$provider$http/1.2.3$/main.json"),
             },
            new string[]
            {
                RootC("users/username/repos/deployment/src"),
                RootC("users/username/repos/deployment/shared"),
                RootD("bicepcacheroot/br/example.azurecr.io/test$provider$http/1.2.3$"),
            });

        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            yield return data(
                new string[]
                {
                    $"/a.txt",
                    $"/b.txt",
                    $"/c/b.txt",
                },
                new string[]
                {
                    $"/"
                });
        }
    }
}
