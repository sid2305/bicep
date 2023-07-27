// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Bicep.Core.Debuggable;

/// <summary>
/// A MemoryStream that will display its contents as text in the VS debugger
/// </summary>
[DebuggerDisplay("Pos={Position}, {GetTextContents()}")]
public class TextMemoryStream : MemoryStream
{
    public TextMemoryStream() { }

    public TextMemoryStream(byte[] buffer)
        : base(buffer) { }

    public TextMemoryStream(int capacity)
    : base(capacity) { }

    private string GetTextContents()
    {
        var previousPosition = this.Position;
        this.Position = 0;

        try
        {
            using (StreamReader reader = new StreamReader(this, leaveOpen: true))
            {
                return reader.ReadToEnd();
            }
        }
        finally
        {
            this.Position = previousPosition;
        }
    }
}
