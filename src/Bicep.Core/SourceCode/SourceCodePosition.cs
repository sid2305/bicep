// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bicep.Core.Parsing;

namespace Bicep.Core.SourceCode
{
    [JsonConverter(typeof(SourceCodePositionConverter))]
    public record SourceCodePosition(int Line, int Column)
    {
        public SourceCodePosition((int line, int column) input)
            : this(input.line, input.column)
        { }

        public static bool TryParse(string s, [NotNullWhen(true)] out SourceCodePosition? sourceCodePosition)
        {
            var parts = s?.TrimStart('"').TrimStart('[').TrimEnd(']').TrimEnd('"').Split(":");
            if (parts?.Length == 2 && int.TryParse(parts[0], out int line) && int.TryParse(parts[1], out int column))
            {
                sourceCodePosition = new SourceCodePosition(line, column);
                return true;
            }
            else
            {
                sourceCodePosition = null;
                return false;
            }
        }

        public override string ToString()
        {
            return $"[{Line}:{Column}]";
        }
    }

    public class SourceCodePositionConverter : JsonConverter<SourceCodePosition>
    {
        public override SourceCodePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            if (s is { } && SourceCodePosition.TryParse(s, out SourceCodePosition? sourceCodePosition))
            {
                return sourceCodePosition;
            }
            else
            {
                throw new ArgumentException($"Invalid input format for deserialization of {nameof(SourceCodePosition)}");
            }
        }

        public override void Write(Utf8JsonWriter writer, SourceCodePosition value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
