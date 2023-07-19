// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Bicep.Core.Semantics;
using Bicep.Core.Workspaces;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using System.Text;
using Bicep.Core.Navigation;
using System.IO.Abstractions;
using System.Linq;

namespace Bicep.Core.Registry;

// asdfg fix regression: opening template spec or arm templates doesn't show in JSON editor
// asdfg analyzer failures will show in editor that would have been ignored with bicepconfig.json
// asdfg okay to show location of template spec refs?  I assume they're in the compiled ARM json anyway?
// asdfg loadContent files?  they don't currently show up in sources
// asdfg option to not publish sources?
// asdfg hide under capabilities
// asdfg anthony (marcin?) said we should built off of the modified bicep (stripped comments)...  correct?
//     asdfg what about formatting JSON?

// asdfg remove user info from paths in __metadata.json
// asdfg built json
// asdfg paths: absolute or relative or both?
//   e.g.
/*
 {
  "entryPoint": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/complicated/my%20entrypoint.bicep",
  "sourceFiles": [
    {
      "uri": "/Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$app$app-configuration/1.0.1$/main.json",
      "localPath": "main.json",
      "kind": "armTemplate"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$samples$hello-world/1.0.2$/main.json",
      "localPath": "main.json",
      "kind": "armTemplate"
    },
    {
      "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/simpleModule/storageAccount.bicep",
      "localPath": "storageAccount.bicep",
      "kind": "bicep"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$app$dapr-containerapps-environment/1.2.2$/main.json",
      "localPath": "main.json",
      "kind": "armTemplate"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/2.0a/main.json",
      "localPath": "main.json",
      "kind": "templateSpec"
    },
    {
      "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/complicated/my%20entrypoint.bicep",
      "localPath": "my%20entrypoint.bicep",
      "kind": "bicep"
    },
    {
      "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/complicated/modules/main.bicep",
      "localPath": "main.bicep",
      "kind": "bicep"
    },
    {
      "uri": "/Users/stephenweatherford/.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/1.0a/main.json",
      "localPath": "main.json",
      "kind": "templateSpec"
    }
  ]
}
 */
// asdfg example: relative to ancestor of entrypoint:
/*
 module relativePath '../simpleModule/storageAccount.bicep' = {
    =>
     {
  "uri": "file:///Users/stephenweatherford/repos/template-examples/bicep/modules/simpleModule/storageAccount.bicep",
  "localPath": "storageAccount.bicep",
  "kind": "bicep"
},

 */
//asdfg same module could be referenced in different ways in different places

// asdfg template spec e.g.:
// module tsModule 'ts:e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storageSpec:1.0a' = {
// =>
// /Users/stephenweatherford/.bicep/ts/e5ef2b13-6478-4887-ad57-1aa6b9475040/sawbicep/storagespec/1.0a/main.json:

// asdfg should I decompress sources.zip?
// asdfg pretty-print JSON?
// asdfg remove comments from bicep
// asdfg show bicep sources for nested modules?
//     /Users/stephenweatherford/.bicep/br/mcr.microsoft.com/bicep$app$dapr-containerapps-environment/1.2.2$/main.json:

public class SourceArchive : IDisposable
{
    private ZipArchive? zipArchive;

    const string SourceKind_Bicep = "bicep";
    const string SourceKind_ArmTemplate = "armTemplate";
    const string SourceKind_TemplateSpec = "templateSpec";
    // IF ADDING TO THIS: Remember both forwards and backwards compatibility.
    // Previous versions must be able to deal with unrecognized source kinds.   asdfg test

    ////asdfg how test forwards compat?
    //public static class SourceInfoKeys
    //{
    //    const string Uri = "uri"; // Required for all versions
    //    const string LocalPath = "localPath"; // Required for all versions
    //    const string Kind = "kind"; // Required for all versions
    //    // IF ADDING TO THIS: Remember both forwards and backwards compatibility.
    //    //   Previous versions of Bicep must be able to ignore what is added.
    //}

    public record FileMetadata(
        Uri Uri,          // required
        string LocalPath, // required
        string Kind       // required
    );

    // asdfg test that deserializing this with unknown properties works
    public record Metadata( //asdfg
        Uri EntryPoint, //asdfg?
                        //asdfg List<Dictionary<string, string>> SourceFiles
        IEnumerable<FileMetadata> SourceFiles
    );

    //asdfg    private IFileSystem fileSystem;
    //private string localSourcesFolder; //asdfg?

    //asdfg private const string ZipFileName = "bicepSources.zip";

    // public class PackResult : IDisposable asdfg
    // {
    //     public string ZipFilePath { get; }

    //     private string? tempFolder;

    //     public PackResult(string tempFolder, string zipFilePath)
    //     {
    //         this.ZipFilePath = zipFilePath;
    //         this.tempFolder = tempFolder;
    //     }

    //     protected virtual void Dispose(bool disposing)
    //     {
    //         if (tempFolder is not null && disposing)
    //         {
    //             Directory.Delete(this.tempFolder, recursive: true);
    //             this.tempFolder = null;
    //         }
    //     }

    //     public void Dispose()
    //     {
    //         Dispose(disposing: true);
    //         GC.SuppressFinalize(this);
    //     }
    // }

    public SourceArchive(Stream stream)  //asdfg takes ownership
    {
        this.zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
    }

    private string GetRequiredEntryContents(string relativePath)
    {
        if (zipArchive is null)
        {
            throw new ObjectDisposedException(nameof(SourceArchive));
        }

        if (zipArchive.GetEntry(relativePath) is not ZipArchiveEntry entry)
        {
            throw new Exception($"Could not find expected entry in archived module sources \"{relativePath}\"");
        }

        using var entryStream = entry.Open();
        using var sr = new StreamReader(entryStream);
        return sr.ReadToEnd();
    }

    public Uri GetEntrypointUri()
    {
        return GetMetadata().EntryPoint;
    }

    public static Stream PackSources(SourceFileGrouping sourceFileGrouping)
    {
        return PackSources(sourceFileGrouping.EntryFileUri, sourceFileGrouping.SourceFiles.ToArray());
    }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static Stream PackSources(Uri entryFileUri, params ISourceFile[] sourceFiles)
    {
        //asdfg how structure hierarchy of files?
        //asdfg map of locations to filenames

        var stream = new MemoryStream();
        using (ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            zipArchive.Comment = "asdfg";

            var filesMetadata = new List<FileMetadata>();

            foreach (var file in sourceFiles)
            {
                string source;
                string kind;
                switch (file)
                {
                    case BicepFile bicepFile:
                        source = bicepFile.ProgramSyntax.ToTextPreserveFormatting(); //asdfg?
                        kind = SourceKind_Bicep;
                        break;
                    case ArmTemplateFile armTemplateFile:
                        source = armTemplateFile.Template?.ToJson() ?? "(ARM template is null)"; //asdfg testpoint
                        kind = SourceKind_ArmTemplate;
                        break;
                    case TemplateSpecFile templateSpecFile:
                        source = templateSpecFile.MainTemplateFile.Template?.ToJson() ?? "(ARM template is null)"; //asdfg testpoint
                        kind = SourceKind_TemplateSpec;
                        break;
                    default:
                        throw new ArgumentException($"Unexpected source file type {file.GetType().Name}"); //asdfg?
                }

                //asdfg map folder structure, duplicates, remove user info, relative paths, absolute paths, uris, etc.
                var sourceRelativeDestinationPath = Path.GetFileName(file.FileUri.AbsolutePath);
                WriteNewEntry(zipArchive, sourceRelativeDestinationPath, source);
                filesMetadata.Add(new(file.FileUri, sourceRelativeDestinationPath, kind));
            }

            var metadata = new Metadata(entryFileUri, filesMetadata);
            string metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            WriteNewEntry(zipArchive, "__metadata.json", metadataJson); //asdfg no collisions
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    public string GetMetadataContentsAsdfgDeleteMe() {
        return GetRequiredEntryContents("__metadata.json"); //asdfg magic
    }

    public IEnumerable<(FileMetadata Metadata, string Contents)> GetSourceFiles()
    {
        if (zipArchive is null)
        {
            throw new ObjectDisposedException(nameof(SourceArchive));
        }

        var metadata = GetMetadata();
        foreach (var entry in metadata.SourceFiles) //asdfg entrypoint first
        {
            yield return (entry, GetRequiredEntryContents(entry.LocalPath));
        }
    }
    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private Metadata GetMetadata()
    {
        var metadataJson = GetRequiredEntryContents("__metadata.json"); //asdfg magic
        var metadata = JsonSerializer.Deserialize<Metadata>(metadataJson, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ?? throw new ArgumentException($"Unable to deserialize metadata from {"__metadata.json"}");
        if (metadata is null)
        {
            throw new ArgumentException($"Unable to deserialize metadata from {"__metadata.json"}");
        }

        return metadata;
    }

    private static void WriteNewEntry(ZipArchive archive, string path, string contents)
    {
        var metadataEntry = archive.CreateEntry(path);
        using var entryStream = metadataEntry.Open();
        using var writer = new StreamWriter(entryStream);
        writer.Write(contents);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && zipArchive is not null)
        {
            zipArchive.Dispose();
            zipArchive = null;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
