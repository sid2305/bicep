// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bicep.Core.FileSystem;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;

namespace Bicep.Core.SourceCode
{
    public static class SourceCodePathHelper
    {
        public static string Shorten(string path, int maxLength)
        {
            if (path.Length <= maxLength)
            {
                return path;
            }

            var extension = Path.GetExtension(path) ?? string.Empty;
            var tail = "__path_too_long__" + extension.Substring(0, Math.Min(10, extension.Length));
            var shortPath = path.Substring(0, maxLength - tail.Length) + tail;
            Debug.Assert(shortPath.Length == maxLength);
            return shortPath;
        }

        /// <summary>
        /// Finds the list of all distinctFolders in the given pathsArray that are not subfolders of any path,
        /// i.e. the smallest set of distinctFolders so that you can express any of the pathsArray as relative
        /// to one of the roots, without having to use "..".
        ///
        /// Each given path will inside or in a descendent of one and only one root
        /// <returns>
        /// A mapping of the original paths to the root path that they should be relative to.
        /// </returns>
        /// </summary>
        /// <example>
        /// 
        ///   c:/users/username/repos/deployment/src/main.bicep
        ///   c:/users/username/repos/deployment/src/modules/module1.bicep
        ///   c:/users/username/repos/deployment/src/modules/module2.bicep
        ///   c:/users/username/repos/deployment/shared/shared1.bicep
        ///   d:/bicepcacheroot/br/example.azurecr.io/test$provider$http/1.2.3$/main.json
        ///
        /// the calculed roots are:
        /// 
        ///   c:/users/username/repos/deployment/src
        ///   c:/users/username/repos/deployment/shared
        ///   d:/bicepcacheroot/br/example.azurecr.io/test$provider$http/1.2.3$
        ///
        /// so the returned map is:
        ///
        ///   c:/users/username/repos/deployment/src/main.bicep            => c:/users/username/repos/deployment/src
        ///   c:/users/username/repos/deployment/src/modules/module1.bicep => c:/users/username/repos/deployment/src
        ///   c:/users/username/repos/deployment/src/modules/module2.bicep => c:/users/username/repos/deployment/src
        ///   c:/users/username/repos/deployment/shared/shared1.bicep      => c:/users/username/repos/deployment/shared
        ///   d:/bicepcacheroot/br/example.azurecr.io/test$provider$http/1.2.3$/main.json
        ///                                                                => d:/bicepcacheroot/br/example.azurecr.io/test$provider$http/1.2.3$
        /// </example>
        public static IDictionary<string, string> GetUniquePathRoots(string[] filePaths)
        {
            if (filePaths.Distinct().Count() != filePaths.Length)
            {
                throw new ArgumentException($"Paths should be distinct before calling {nameof(GetUniquePathRoots)}");
            }

            if (filePaths.Any(p => p.Contains('\\')))
            {
                throw new ArgumentException($"Paths should be normalized before calling {nameof(GetUniquePathRoots)}");
            }

            string[] distinctFolders = filePaths.Select(path =>
            {
                if (!Path.IsPathFullyQualified(path) || Path.GetDirectoryName(path) is not string folder)
                {
                    throw new ArgumentException($"Path '{path}' should be a valid fully qualified path");
                }

                return folder;
            }).
            Select(Normalize)
            .Distinct()
            .ToArray();

            var distinctRoots = distinctFolders.Where(path =>
                !distinctFolders.Any(path2 => path != path2 && IsDescendentOf(path, path2))
            ).ToArray();

            // Map path -> root to return
            var rootMapping = filePaths.Select(
                filePath =>
                {
                    var fileFolder = Path.GetDirectoryName(filePath)!; // (GetDirectoryName should always return non-null for a file path)
                    var matchingRoot = distinctRoots.Where(r => IsSameOrIsDescendentOf(fileFolder, r)).Single();
                    return (filePath, matchingRoot);
                }
            );
            return rootMapping.ToDictionary(pair => pair.filePath, pair => pair.matchingRoot);
        }

        public static string Normalize(string path)//asdfg rename
        {
            return path.Replace('\\', '/');
        }

        private static bool IsDescendentOf(string folder, string possibleParentFolder) //asdfg rename
        {
            return folder != possibleParentFolder // asdfg insensitive?
                && IsSameOrIsDescendentOf(folder, possibleParentFolder);
        }

        private static bool IsSameOrIsDescendentOf(string folder, string possibleParentFolder) //asdfg rename
        {
            return !Path.GetRelativePath(possibleParentFolder, folder).StartsWith("..")
                && Path.GetPathRoot(folder) == Path.GetPathRoot(possibleParentFolder);
        }
    }
}
