// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Bicep.Core.Exceptions;
using Bicep.Core.Navigation;
using Bicep.Core.Registry.Oci;
using Bicep.Core.Semantics;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using static Bicep.Core.SourceCode.SourceArchive;

namespace Bicep.Core.SourceCode
{
    public record SourceArchiveResult
    {
        public SourceArchiveResult()
        {
            SourceArchive = null;
            Message = null;
        }

        public SourceArchiveResult(SourceArchive sourceArchive)
        {
            SourceArchive = sourceArchive;
            Message = null;
        }

        public SourceArchiveResult(string message)
        {
            SourceArchive = null;
            Message = message;
        }

        // If both are null, there is no source code available (e.g. wasn't published)
        public SourceArchive? SourceArchive;
        public string? Message;
    }

    // Contains the individual source code files for a Bicep file and all of its dependencies.
    public partial class SourceArchive // Partial required for serialization
    {
        private ArchiveMetadata DeserializedMetadata { get; init; }
        private bool isDisposed = false;//asfdg remove

        public ImmutableArray<SourceFileInfo> SourceFiles { get; init; }
        public string EntrypointRelativePath => DeserializedMetadata.EntryPoint;

        // The version stamped into this deserialized archive instance as the minimum required to view it
        public string? MinimumBicepVersionRequired => DeserializedMetadata.MinimumBicepVersionRequired;

        // The version of Bicep which created this deserialized archive instance.
        public string CreatedWithBicepVersion => DeserializedMetadata.CreatedWithBicepVersion;


        public const string SourceKind_Bicep = "bicep";
        public const string SourceKind_ArmTemplate = "armTemplate";
        public const string SourceKind_TemplateSpec = "templateSpec";

        private const string MetadataArchivedFileName = "__metadata.json";

        // Minimum required Bicep version that will be written into new source archives.  Only update this when breaking changes occur.
        private const string CurrentMinimumBicepVersionRequired = "0.24.61";

        public partial record SourceFileInfo(
            string Path,        // the location, relative to the main.bicep file's folder, for the file that will be shown to the end user (required in all Bicep versions)
            string ArchivePath, // the location (relative to root) of where the file is stored in the archive
            string Kind,         // kind of source
            string Contents
        );

        [JsonSerializable(typeof(ArchiveMetadata))]
        [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
        private partial class MetadataSerializationContext : JsonSerializerContext { }

        [JsonSerializable(typeof(ArchiveMetadata))]
        private record ArchiveMetadata(
            string MinimumBicepVersionRequired,
            string CreatedWithBicepVersion,
            string EntryPoint, // Path of the entrypoint file
            IEnumerable<SourceFileInfoEntry> SourceFiles
        );

        [JsonSerializable(typeof(SourceFileInfoEntry))]
        private partial record SourceFileInfoEntry(
            // IF ADDING TO THIS: Remember both forwards and backwards compatibility.
            // E.g., previous versions must be able to deal with unrecognized source kinds.
            // (but see MinimumBicepVersionToReadMetadata for breaking changes)
            string Path,        // the location, relative to the main.bicep file's folder, for the file that will be shown to the end user (required in all Bicep versions)
            string ArchivePath, // the location (relative to root) of where the file is stored in the archive
            string Kind         // kind of source
        );

        private string? GetRequiredBicepVersionMessage()
        {
            if (MinimumBicepVersionRequired is null)
            {
                return $"// This source code was published with an older version of Bicep. It needs to be republished with a newer version.";
            }

            if (!Versioning.IsCurrentBicepVersionAtLeast(this.MinimumBicepVersionRequired))
            {
                return $"// You need version {this.MinimumBicepVersionRequired} or higher of the Bicep language server to view this source code. You are using version {Versioning.GetCurrentBicepVersion().ToFullString}.";
            }

            return null;
        }

        public static SourceArchiveResult UnpackSourcesFromStream(Stream stream)
        {
            var archive = new SourceArchive(stream);
            if (archive.GetRequiredBicepVersionMessage() is string message)
            {
                return new SourceArchiveResult(message);
            }
            else
            {
                return new(archive);
            }
        }

        /// <summary>
        /// Bundles all the sources from a compilation group (thus source for a bicep file and all its dependencies
        /// in JSON form) into an archive (as a stream)
        /// </summary>
        /// <returns>A .tar.gz file as a binary stream</returns>
        public static Stream PackSourcesIntoStream(SourceFileGrouping sourceFileGrouping)
        {
            return PackSourcesIntoStream(sourceFileGrouping.EntryFileUri, sourceFileGrouping.SourceFiles.ToArray());
        }

        // TODO: Toughen this up to handle conflicting paths, ".." paths, etc.
        public static Stream PackSourcesIntoStream(Uri entrypointFileUri, params ISourceFile[] sourceFiles)
        {
            var baseFolderBuilder = new UriBuilder(entrypointFileUri)
            {
                Path = string.Join("", entrypointFileUri.Segments.SkipLast(1))
            };
            var baseFolderUri = baseFolderBuilder.Uri;

            var stream = new MemoryStream();
            using (var gz = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
            {
                using (var tarWriter = new TarWriter(gz, leaveOpen: true))
                {
                    var filesMetadata = new List<SourceFileInfoEntry>();
                    string? entryPointPath = null;

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

                        var (location, archivePath) = CalculateFilePathsFromUri(file.FileUri);
                        WriteNewFileEntry(tarWriter, archivePath, source);
                        filesMetadata.Add(new SourceFileInfoEntry(location, archivePath, kind));

                        if (file.FileUri == entrypointFileUri)
                        {
                            if (entryPointPath is not null)
                            {
                                throw new ArgumentException($"{nameof(SourceArchive)}.{nameof(PackSourcesIntoStream)}: Multiple source files with the entrypoint \"{entrypointFileUri.AbsoluteUri}\" were passed in.");
                            }

                            entryPointPath = location;
                        }
                    }

                    if (entryPointPath is null)
                    {
                        throw new ArgumentException($"{nameof(SourceArchive)}.{nameof(PackSourcesIntoStream)}: No source file with entrypoint \"{entrypointFileUri.AbsoluteUri}\" was passed in.");
                    }

                    // Add the metadata file
                    var metadataContents = CreateMetadataFileContents(entryPointPath, filesMetadata);
                    WriteNewFileEntry(tarWriter, MetadataArchivedFileName, metadataContents);
                }
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;

            (string location, string archivePath) CalculateFilePathsFromUri(Uri uri)
            {
                Uri relativeUri = baseFolderUri.MakeRelativeUri(uri);
                var relativeLocation = Uri.UnescapeDataString(relativeUri.OriginalString);
                return (relativeLocation, relativeLocation);
            }
        }

        private SourceArchive(Stream stream)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SourceArchive));
            }

            var filesBuilder = ImmutableDictionary.CreateBuilder<string, string>();

            stream.Position = 0;
            var gz = new GZipStream(stream, CompressionMode.Decompress);
            using var tarReader = new TarReader(gz);

            while (tarReader.GetNextEntry() is { } entry)
            {
                string contents = entry.DataStream is null ? string.Empty : new StreamReader(entry.DataStream, Encoding.UTF8).ReadToEnd();
                filesBuilder.Add(entry.Name, contents);
            }

            var dictionary = filesBuilder.ToImmutableDictionary();

            var metadataJson = dictionary[MetadataArchivedFileName]
                ?? throw new BicepException("Incorrectly formatted source file: No {MetadataArchivedFileName} entry");
            var metadata = JsonSerializer.Deserialize<ArchiveMetadata>(metadataJson, MetadataSerializationContext.Default.ArchiveMetadata)
                ?? throw new BicepException("Source archive has invalid metadata entry");

            var infos = new List<SourceFileInfo>();
            foreach (var info in metadata.SourceFiles.OrderBy(e => e.Path).ThenBy(e => e.ArchivePath))
            {
                var contents = dictionary[info.ArchivePath]
                    ?? throw new BicepException("Incorrectly formatted source file: File entry not found: \"{info.ArchivePath}\"");
                infos.Add(new SourceFileInfo(info.Path, info.ArchivePath, info.Kind, contents));
            }

            this.DeserializedMetadata = metadata;
            this.SourceFiles = infos.ToImmutableArray();
        }

        private static string CreateMetadataFileContents(string entrypointPath, IEnumerable<SourceFileInfoEntry> files)
        {
            // Add the __metadata.json file
            var metadata = new ArchiveMetadata(CurrentMinimumBicepVersionRequired, Versioning.GetCurrentBicepVersion().ToFullString(), entrypointPath, files);
            return JsonSerializer.Serialize(metadata, MetadataSerializationContext.Default.ArchiveMetadata);
        }

        private static void WriteNewFileEntry(TarWriter tarWriter, string archivePath, string contents)
        {
            var tarEntry = new PaxTarEntry(TarEntryType.RegularFile, archivePath);
            tarEntry.DataStream = new MemoryStream(Encoding.UTF8.GetBytes(contents));
            tarWriter.WriteEntry(tarEntry);
        }
    }
}
