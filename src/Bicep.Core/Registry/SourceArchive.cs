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
using static Bicep.Core.Registry.SourceArchive;

namespace Bicep.Core.Registry;

public class SourceArchive : IDisposable
{
    private ZipArchive? zipArchive;

    //asdfg enum or something?
    public const string SourceKind_Bicep = "bicep";
    public const string SourceKind_ArmTemplate = "armTemplate";
    public const string SourceKind_TemplateSpec = "templateSpec";

    public const string MetadataArchivedFileName = "__metadata.json";

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
    //asdfg path too long for archive
    //asdfg path contains bad characters for archive

    // WARNING: Only change this value if there is a breaking change such that old versions of Bicep should fail on reading this source archive
    public const int CurrentMetadataVersion = 0; // TODO(asdfg): Change to 1 when removing experimental flag

    public record FileMetadata(
        string Path,         // the location, relative to the main.bicep file's folder, for the file that will be shown to the end user (required in all Bicep versions)
        string ArchivedPath, // the location (relative to root) of where the file is stored in the archive
        string Kind          // kind of source
    );

    public record Metadata(
        int MetadataVersion,
        string EntryPoint, // Path of the entrypoint file
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

    private string GetEntryContents(string archivedPath)
    {
        if (zipArchive is null)
        {
            throw new ObjectDisposedException(nameof(SourceArchive));
        }

        if (zipArchive.GetEntry(archivedPath) is not ZipArchiveEntry entry)
        {
            throw new Exception($"Could not find expected entry in archived module sources \"{archivedPath}\"");
        }

        using var entryStream = entry.Open();
        using var sr = new StreamReader(entryStream);
        return sr.ReadToEnd();
    }

    public string GetEntrypointPath()
    {
        return GetMetadata().EntryPoint;
    }

    public static Stream PackSources(SourceFileGrouping sourceFileGrouping)
    {
        return PackSources(sourceFileGrouping.EntryFileUri, sourceFileGrouping.SourceFiles.ToArray());
    }

    public static Stream PackSources(Uri entrypointFileUri, params ISourceFile[] sourceFiles)
    {
        //var entrypoints = sourceFiles.Where(f => f.FileUri.Equals(entrypointFileUri)).ToArray();
        //if (entrypoints .Length == 0)
        //{
        //    throw new ArgumentException($"{nameof(SourceArchive)}.{nameof(PackSources)}: No source file with entrypoint \"{entrypointFileUri.AbsoluteUri}\" was passed in.");
        //}
        //else if (entrypoints.Length > 1)
        //{
        //    throw new ArgumentException($"{nameof(SourceArchive)}.{nameof(PackSources)}: Multiple source files with the entrypoint \"{entrypointFileUri.AbsoluteUri}\" were passed in.");
        //}

        //asdfg don't let any files conflict with entrypoint path
        string? entryPointPath = null;

        var baseFolderBuilder = new UriBuilder(entrypointFileUri);
        baseFolderBuilder.Path = string.Join("", entrypointFileUri.Segments.SkipLast(1));
        var baseFolderUri = baseFolderBuilder.Uri;

        var stream = new MemoryStream();
        using (ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            zipArchive.Comment = "asdfg";

            var filesMetadata = new List<FileMetadata>();

            foreach (var file in sourceFiles)
            {
                string source = file.GetOriginalSource();
                string kind = file switch
                {
                    BicepFile bicepFile => SourceKind_Bicep,
                    ArmTemplateFile armTemplateFile => SourceKind_ArmTemplate,
                    TemplateSpecFile => SourceKind_TemplateSpec,
                    _ => throw new ArgumentException($"Unexpected source file type {file.GetType().Name}"),
                };

                //asdfg map folder structure, duplicates, remove user info, relative paths, absolute paths, uris, etc.
                var paths = GetFilePaths(file.FileUri);
                WriteNewEntry(zipArchive, paths.archivedPath, source);
                filesMetadata.Add(new FileMetadata(paths.location, paths.archivedPath, kind));

                if (file.FileUri == entrypointFileUri)
                {
                    if (entryPointPath is not null)
                    { //asdfg testpoint
                        throw new ArgumentException($"{nameof(SourceArchive)}.{nameof(PackSources)}: Multiple source files with the entrypoint \"{entrypointFileUri.AbsoluteUri}\" were passed in.");
                    }

                    entryPointPath = paths.location;
                }
            }

            if (entryPointPath is null)
            {
                throw new ArgumentException($"{nameof(SourceArchive)}.{nameof(PackSources)}: No source file with entrypoint \"{entrypointFileUri.AbsoluteUri}\" was passed in.");
            }

            // Add the __metadata.json file
            var metadataContents = CreateMetadataFileContents(entryPointPath, filesMetadata);
            WriteNewEntry(zipArchive, MetadataArchivedFileName, metadataContents); //asdfg no collisions with __metadata.json or any other file
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;

        (string location, string archivedPath) GetFilePaths(Uri uri)
        {
            Uri relativeUri = baseFolderUri.MakeRelativeUri(uri);
            var relativeLocation  = Uri.UnescapeDataString(relativeUri.OriginalString);
            return (relativeLocation, relativeLocation);
        }
    }

    public string GetMetadataFileContents()
    {
        return GetEntryContents(MetadataArchivedFileName);
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
            yield return (entry, GetEntryContents(entry.ArchivedPath));
        }
    }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static string CreateMetadataFileContents(string entrypointPath, IEnumerable<FileMetadata> files)
    {
        // Add the __metadata.json file
        var metadata = new Metadata(CurrentMetadataVersion, entrypointPath, files);
        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private Metadata GetMetadata()
    {
        var metadataJson = GetEntryContents(MetadataArchivedFileName);
        var metadata = JsonSerializer.Deserialize<Metadata>(metadataJson, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ?? throw new ArgumentException($"Unable to deserialize metadata from archived file \"{MetadataArchivedFileName}\"");
        if (metadata is null)
        {
            throw new ArgumentException($"Unable to deserialize metadata from archived file \"{MetadataArchivedFileName}\"");
        }

        if (metadata.MetadataVersion > CurrentMetadataVersion) {
            throw new Exception($"Source archive contains a metadata file with metadata version {metadata.MetadataVersion}, which this version of Bicep cannot handle. Please upgrade Bicep.");
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
