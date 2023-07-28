// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Bicep.Core.Debuggable;

/// <summary>
/// A MemoryStream that can be easily converted to/from text and will display its contents as text in the VS debugger.
/// </summary>
[DebuggerDisplay("Pos={Position == Length - 1 ? \"at end\" : Position}, {GetString()}")]
public class TextMemoryStream : MemoryStream
{
    #region MemoryStream constructors

    public TextMemoryStream() { }

    public TextMemoryStream(byte[] buffer) : base(buffer) { }

    public TextMemoryStream(int capacity) : base(capacity) { }

    public TextMemoryStream(byte[] buffer, bool writable) : base(buffer, writable) { }

    public TextMemoryStream(byte[] buffer, int index, int count) : base(buffer, index, count) { }

    public TextMemoryStream(byte[] buffer, int index, int count, bool writable) : base(buffer, index, count, writable) { }

    public TextMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) : base(buffer, index, count, writable, publiclyVisible) { }

    #endregion MemoryStream constructors
    public TextMemoryStream(IEnumerable<byte> bytes)
        : base(bytes.ToArray())
    {
    }

    public TextMemoryStream(string content)
    : base(Encoding.UTF8.GetBytes(content))
    { }

    public string GetString()
    {
        return Encoding.UTF8.GetString(this.GetBuffer());
    }
}
