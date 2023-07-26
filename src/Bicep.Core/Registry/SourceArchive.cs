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

    public record FileMetadata(
        string Uri,          // required in all Bicep versions
        string ArchivedPath, // required in all Bicep versions
        string Kind          // required in all Bicep versions
    );

    public record Metadata(
        string EntryPoint, // Uri (required in all Bicep versions)
        IEnumerable<FileMetadata> SourceFiles // required in all Bicep versions
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
        return new Uri(GetMetadata().EntryPoint, UriKind.Absolute);
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
                string source = file.GetOriginalSource();
                string kind = file switch
                {
                    BicepFile bicepFile => SourceKind_Bicep,
                    ArmTemplateFile armTemplateFile => SourceKind_ArmTemplate,
                    TemplateSpecFile => SourceKind_TemplateSpec,
                    _ => throw new ArgumentException($"Unexpected source file type {file.GetType().Name}"),
                };

                //asdfg map folder structure, duplicates, remove user info, relative paths, absolute paths, uris, etc.
                var sourceRelativeDestinationPath = Path.GetFileName(file.FileUri.LocalPath);
                WriteNewEntry(zipArchive, sourceRelativeDestinationPath, source);
                filesMetadata.Add(new(file.FileUri.AbsoluteUri, sourceRelativeDestinationPath, kind));
            }

            var metadata = new Metadata(entryFileUri.AbsoluteUri, filesMetadata);
            string metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            WriteNewEntry(zipArchive, MetadataArchivedFileName, metadataJson); //asdfg no collisions
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    public string GetMetadataContentsAsdfgDeleteMe() {
        return GetRequiredEntryContents(MetadataArchivedFileName);
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
            yield return (entry, GetRequiredEntryContents(entry.ArchivedPath));
        }
    }
    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private Metadata GetMetadata()
    {
        var metadataJson = GetRequiredEntryContents(MetadataArchivedFileName);
        var metadata = JsonSerializer.Deserialize<Metadata>(metadataJson, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ?? throw new ArgumentException($"Unable to deserialize metadata from archived file \"{MetadataArchivedFileName}\"");
        if (metadata is null)
        {
            throw new ArgumentException($"Unable to deserialize metadata from archived file \"{MetadataArchivedFileName}\"");
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
