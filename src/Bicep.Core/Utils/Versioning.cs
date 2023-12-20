// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using NuGet.Versioning;

namespace Bicep.Core.Utils;

public static class Versioning
{
    public static SemanticVersion GetCurrentBicepVersion()
    {
        return SemanticVersion.Parse(ThisAssembly.AssemblyInformationalVersion);
    }

    public static bool IsCurrentBicepVersionAtLeast(string minimumVersion)
    {
        var current = GetCurrentBicepVersion();
        if (SemanticVersion.TryParse(ThisAssembly.AssemblyInformationalVersion, out var minimum))
        {
            return current >= minimum;
        }

        return false;
    }
}
