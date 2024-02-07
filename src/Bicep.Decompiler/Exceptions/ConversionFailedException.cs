// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Bicep.Decompiler.Exceptions
{
    public class ConversionFailedException(string message, IJsonLineInfo jsonLineInfo, Exception? innerException = null) : Exception(FormatMessage(message, jsonLineInfo), innerException)
    {
        private static string FormatMessage(string message, IJsonLineInfo jsonLineInfo)
            => $"[{jsonLineInfo.LineNumber}:{jsonLineInfo.LinePosition}]: {message}";
    }
}
