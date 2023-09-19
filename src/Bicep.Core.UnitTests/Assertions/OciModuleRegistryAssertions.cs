// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Oci;
using FluentAssertions;
using FluentAssertions.Primitives;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bicep.Core.UnitTests.Assertions
{
    public static class OciModuleRegistryExtensions
    {
        public static OciModuleRegistryAssertions Should(this OciModuleRegistry ociModuleRegistry) => new(ociModuleRegistry);
    }

    public class OciModuleRegistryAssertions : ReferenceTypeAssertions<OciModuleRegistry, OciModuleRegistryAssertions>
    {
        public OciModuleRegistryAssertions(OciModuleRegistry ociModuleRegistry) : base(ociModuleRegistry)
        {
        }

        protected override string Identifier => "OciModuleRegistry";

        public AndConstraint<OciModuleRegistryAssertions> HaveValidCachedModulesWithSources()
            => HaveValidCachedModules(withSources: true);
        public AndConstraint<OciModuleRegistryAssertions> HaveValidCachedModulesWithoutSources()
            => HaveValidCachedModules(withSources: false);

        public AndConstraint<OciModuleRegistryAssertions> HaveValidCachedModules(bool? withSources = null)
        {
            RegistryCacheShouldHaveValidModules(Subject.CacheRootDirectory);
            return new(this);
        }

        // Check that all cached modules have the expected files
        public static void RegistryCacheShouldHaveValidModules(string cacheRootDirectory, bool? withSources = null)
        {
            // ensure something got restored
            var cacheDir = new DirectoryInfo(cacheRootDirectory);
            cacheDir.Exists.Should().BeTrue("Cache root directory should exist");

            // we create it with same casing on all file systems
            var brDir = cacheDir.EnumerateDirectories().Single(dir => string.Equals(dir.Name, "br"));

            // the directory structure is .../br/<registry>/<repository>/<tag>
            var moduleDirectories = brDir
                .EnumerateDirectories()
                .SelectMany(registryDir => registryDir.EnumerateDirectories())
                .SelectMany(repoDir => repoDir.EnumerateDirectories());

            foreach (var moduleDirectory in moduleDirectories)
            {
                var files = moduleDirectory.EnumerateFiles().Select(file => file.Name).ToImmutableArray();
                if (withSources == true)
                {
                    files.Should().BeEquivalentTo("lock", "main.json", "manifest", "metadata", "layer.0", "layer.1");
                }
                else if (withSources == false)
                {
                    files.Should().BeEquivalentTo("lock", "main.json", "manifest", "metadata", "layer.0");
                }
                else
                {
                    files.Should().ContainEquivalentOf("lock", "main.json", "manifest", "metadata", "layer.0");
                }
            }
        }
    }
}
