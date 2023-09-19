// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bicep.Core.Registry;
using FluentAssertions;

namespace Bicep.Core.UnitTests.Registry;

record Module(Layer[] Layers);
record Layer(string MediaType);

// Represents a cached module in the on-disk registry cache
public record CachedModule(
    string cacheFolder,
    string Registry,
    string Repository,
    string Tag)
{
    public string ManifestContents => File.ReadAllText(Path.Combine(cacheFolder, "manifest"));
    public JsonObject ManifestJson => (JsonObject)JsonNode.Parse(ManifestContents)!;

    public string MetadataContents => File.ReadAllText(Path.Combine(cacheFolder, "metadata"));
    public JsonObject MetadataJson => (JsonObject)JsonNode.Parse(MetadataContents)!;

    public string[] LayerMediaTypes
    {
        get
        {
            // Deserialize the JSON into an object
            var module = JsonSerializer.Deserialize<Module>(ManifestJson)!;
            string[] layerMediaTypes = module.Layers.Select(layer => layer.MediaType).ToArray();
            return layerMediaTypes;
        }
    }

    public SourceArchive? TryGetSources()
    {
        var types = LayerMediaTypes;
        var count = types.Where(layer => layer == "application/vnd.ms.bicep.module.source.v1+zip").Count();
        count.Should().BeLessThanOrEqualTo(1);
        var sourceLayerIndex = Array.FindIndex(types, layer => layer == "application/vnd.ms.bicep.module.source.v1+zip");
        if (sourceLayerIndex >= 0)
        {
            var sourceArchivePath = Path.Combine(cacheFolder, $"layer.{sourceLayerIndex}");
            return new SourceArchive(File.OpenRead(sourceArchivePath));
        }
        else
        {
            return null;
        }
    }
}

public static class CachedModules
{
    // Get all cached modules from the registry cache
    public static ImmutableArray<CachedModule> GetCachedRegistryModules(string cacheRootDirectory) //asdfg move where?
    {
        // ensure something got restored
        var cacheDir = new DirectoryInfo(cacheRootDirectory);
        if (!cacheDir.Exists)
        {
            return ImmutableArray<CachedModule>.Empty;
        }

        // we create it with same casing on all file systems
        var brDir = cacheDir.EnumerateDirectories().Single(dir => string.Equals(dir.Name, "br"));

        // the directory structure is .../br/<registry>/<repository>/<tag>
        var moduleDirectories = brDir
            .EnumerateDirectories()
            .SelectMany(registryDir => registryDir.EnumerateDirectories())
            .SelectMany(repoDir => repoDir.EnumerateDirectories());

        return moduleDirectories
            .Select(moduleDirectory => new CachedModule(
                moduleDirectory.FullName,
                UnobfuscateFolderName(moduleDirectory.Parent!.Parent!.Name),
                UnobfuscateFolderName(moduleDirectory.Parent!.Name),
                UnobfuscateFolderName(moduleDirectory.Name)))
            .ToImmutableArray();
    }

    private static string UnobfuscateFolderName(string folderName)
    {
        return folderName.Replace("$", "/").TrimEnd('/');
    }
}