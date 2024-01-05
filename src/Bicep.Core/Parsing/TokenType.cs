// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Bicep.Core.Parsing
{
    public enum TokenType
    {
        At,
        Unrecognized,
        LeftBrace,
        RightBrace,
        LeftParen,
        RightParen,
        LeftSquare,
        RightSquare,
        Comma,
        Dot,
        Question,
        Colon,
        Semicolon,
        Assignment,
        Plus,
        Minus,
        Asterisk,
        Slash,
        Modulo,
        Exclamation,
        LeftChevron,
        RightChevron,
        LessThanOrEqual,
        GreaterThanOrEqual,
        Equals,
        NotEquals,
        EqualsInsensitive,
        NotEqualsInsensitive,
        LogicalAnd,
        LogicalOr,
        Identifier,
        StringLeftPiece,
        StringMiddlePiece,
        StringRightPiece,
        StringComplete,
        MultilineString,
        Integer,
        TrueKeyword,
        FalseKeyword,
        NullKeyword,
        NewLine,
        EndOfFile,
        DoubleQuestion,
        DoubleColon,
        Arrow,
        Pipe,
        WithKeyword,
        AsKeyword,
    }
}
